import { fetchAll } from "./fetchService.ts";
import type { Client } from "./lrclibClient.ts";
import type { Store } from "./lyricFileStore.ts";
import { exitCode, formatReport } from "./report.ts";
import { parseSongList, SongListError, type Song } from "./songList.ts";
import { isSystemError } from "./systemError.ts";

export const USAGE_EXIT_CODE = 64;
const USAGE = "usage: fetch-lrclib --songs <song list file> --output <directory>";

export interface Dependencies {
  readonly readText: (path: string) => Promise<string>;
  readonly client: Client;
  readonly createStore: (outputDirectory: string) => Store;
}

export type Write = (text: string) => void;

export async function run(
  args: readonly string[],
  dependencies: Dependencies,
  output: Write,
  errorOutput: Write,
): Promise<number> {
  const options = readOptions(args);
  if (options === null) {
    errorOutput(`${USAGE}\n`);
    return USAGE_EXIT_CODE;
  }
  let songs: Song[];
  try {
    songs = parseSongList(await dependencies.readText(options.songsPath));
  } catch (error) {
    if (!(error instanceof SongListError) && !isSystemError(error)) {
      throw error;
    }
    errorOutput(`cannot use song list: ${error.message}\n`);
    return USAGE_EXIT_CODE;
  }
  const store = dependencies.createStore(options.outputDirectory);
  const results = await fetchAll(songs, dependencies.client, store);
  output(formatReport(results));
  return exitCode(results);
}

function readOptions(args: readonly string[]): { songsPath: string; outputDirectory: string } | null {
  // util.parseArgs is not used: it accepts forms the spec rejects, such as repeated options.
  if (args.length !== 4) {
    return null;
  }
  const [firstName = "", firstValue = "", secondName = "", secondValue = ""] = args;
  const options = new Map([
    [firstName, firstValue],
    [secondName, secondValue],
  ]);
  const songsPath = options.get("--songs");
  const outputDirectory = options.get("--output");
  return songsPath && outputDirectory ? { songsPath, outputDirectory } : null;
}
