import assert from "node:assert/strict";
import { mkdtemp, readdir, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, beforeEach, test } from "node:test";
import { createLyricFileStore } from "../../../src/lyricFileStore.ts";
import { physicalFileSystem } from "../../../src/physicalFileSystem.ts";
import { bytes } from "../../doubles.ts";

let root = "";
beforeEach(async () => {
  root = await mkdtemp(join(tmpdir(), "fetch-lrclib-"));
});
afterEach(async () => {
  await rm(root, { recursive: true, force: true });
});

test("the store creates the directory, saves, then keeps without leftovers", async () => {
  const output = join(root, "nested", "out");
  const store = createLyricFileStore(physicalFileSystem, output);

  assert.equal(await store.saveIfAbsent("song.txt", bytes("first")), true);
  assert.equal(await store.saveIfAbsent("song.txt", bytes("second")), false);

  assert.deepEqual(await readdir(output), ["song.txt"]);
  assert.equal(await readFile(join(output, "song.txt"), "utf8"), "first");
});

test("a move refuses to overwrite, like the stand-in", async () => {
  const source = join(root, "source");
  const destination = join(root, "destination");
  await writeFile(source, "new");
  await writeFile(destination, "old");

  await assert.rejects(physicalFileSystem.moveWithoutReplacing(source, destination), { code: "EEXIST" });
  assert.equal(await readFile(destination, "utf8"), "old");
});
