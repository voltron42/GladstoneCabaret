// Whole runs through the real components, with the network and file system replaced.
import assert from "node:assert/strict";
import { join } from "node:path";
import { test } from "node:test";
import { run } from "../../../src/app.ts";
import { createLyricFileStore } from "../../../src/lyricFileStore.ts";
import { bytes, FakeFileSystem, reply, usableResponse } from "../../doubles.ts";

test("a second run keeps files, including corrections", async () => {
  const fileSystem = new FakeFileSystem();
  const songs =
    '[{"artist":"A","songTitle":"One","lrclibRecordId":1},{"artist":"B","songTitle":"Two","lrclibRecordId":2}]';
  const dependencies = {
    readText: () => Promise.resolve(songs),
    client: { getRecord: (id: number) => Promise.resolve(id === 1 ? usableResponse() : reply(404)) },
    createStore: (directory: string) => createLyricFileStore(fileSystem, directory),
  };
  const runOnce = async () => {
    let output = "";
    const code = await run(
      ["--songs", "list", "--output", "out"],
      dependencies,
      (text) => (output += text),
      () => undefined,
    );
    return { code, output };
  };
  const corrected = join("out", "Artist - Track.txt");

  const firstRun = await runOnce();
  fileSystem.files.set(corrected, bytes("corrected"));
  const secondRun = await runOnce();

  assert.equal(firstRun.code, 2);
  assert.equal(
    firstRun.output,
    "GO     A - One (line times: no; saved: 2, kept: 0)\n" +
      "ERROR  B - Two (record not found)\n" +
      "Summary: go=1, no-go=0, error=1\n",
  );
  assert.match(secondRun.output, /^GO {5}A - One \(line times: no; saved: 0, kept: 2\)\n/);
  assert.deepEqual(fileSystem.files.get(corrected), bytes("corrected"));
  assert.equal(fileSystem.files.size, 2);
});
