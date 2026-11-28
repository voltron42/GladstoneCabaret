import { readFile } from "node:fs/promises";
import { run } from "./app.ts";
import { createLrclibClient } from "./lrclibClient.ts";
import { createLyricFileStore } from "./lyricFileStore.ts";
import { physicalFileSystem } from "./physicalFileSystem.ts";

process.exitCode = await run(
  process.argv.slice(2),
  {
    readText: async (path) => (await readFile(path, "utf8")).replace(/^\uFEFF/, ""),
    client: createLrclibClient(),
    createStore: (outputDirectory) => createLyricFileStore(physicalFileSystem, outputDirectory),
  },
  (text) => process.stdout.write(text),
  (text) => process.stderr.write(text),
);
