import assert from "node:assert/strict";
import { join } from "node:path";
import { test } from "node:test";
import { createLyricFileStore } from "../../src/lyricFileStore.ts";
import { bytes, FakeFileSystem } from "../doubles.ts";

const destination = join("out", "song.txt");

async function save(fileSystem: FakeFileSystem): Promise<boolean> {
  return createLyricFileStore(fileSystem, "out").saveIfAbsent("song.txt", bytes("ours"));
}

test("saves and leaves no temporary file", async () => {
  const fileSystem = new FakeFileSystem();

  assert.equal(await save(fileSystem), true);
  assert.deepEqual([...fileSystem.files], [[destination, bytes("ours")]]);
});

test("keeps an existing file", async () => {
  const fileSystem = new FakeFileSystem({ files: [[destination, bytes("corrected")]] });

  assert.equal(await save(fileSystem), false);
  assert.deepEqual([...fileSystem.files], [[destination, bytes("corrected")]]);
});

test("keeps a file created during the save", async () => {
  const fileSystem = new FakeFileSystem({ loseRace: true });

  assert.equal(await save(fileSystem), false);
  assert.deepEqual([...fileSystem.files], [[destination, bytes("theirs")]]);
});

test("removes a partial file when writing fails", async () => {
  const fileSystem = new FakeFileSystem({ failWrite: true });

  await assert.rejects(save(fileSystem), /disk full/);
  assert.equal(fileSystem.files.size, 0);
});
