import { baseName } from "./fileNames.ts";
import type { Client, FetchResponse } from "./lrclibClient.ts";
import type { Store } from "./lyricFileStore.ts";
import { parseLyricRecord, type LyricRecord } from "./lyricRecord.ts";
import type { Song } from "./songList.ts";
import { isSystemError } from "./systemError.ts";

export type Outcome = "GO" | "NO-GO" | "ERROR";

export type SongResult =
  | {
      readonly song: Song;
      readonly outcome: "GO";
      readonly hasLineTimes: boolean;
      readonly savedCount: number;
      readonly keptCount: number;
    }
  | { readonly song: Song; readonly outcome: "NO-GO" | "ERROR"; readonly reason: string };

export async function fetchAll(songs: readonly Song[], client: Client, store: Store): Promise<SongResult[]> {
  const results: SongResult[] = [];
  let stopped = false;
  for (const song of songs) {
    if (stopped) {
      results.push(failure(song, "not attempted: service asked us to wait"));
      continue;
    }
    const response = await client.getRecord(song.lrclibRecordId);
    stopped = asksUsToWait(response);
    results.push(await evaluate(song, response, store));
  }
  return results;
}

function asksUsToWait({ status, hasRetryAfter }: FetchResponse): boolean {
  return status === 429 || (status !== null && status !== 200 && hasRetryAfter);
}

async function evaluate(song: Song, response: FetchResponse, store: Store): Promise<SongResult> {
  if (response.status !== 200) {
    return failure(song, failureReason(response.status));
  }
  const record = parseLyricRecord(response.body, song.lrclibRecordId);
  if (record === null) {
    return failure(song, "invalid response");
  }
  if (record.instrumental) {
    return { song, outcome: "NO-GO", reason: "instrumental" };
  }
  if (record.plainLyrics === null || record.plainLyrics.trim() === "") {
    return { song, outcome: "NO-GO", reason: "no plain lyrics" };
  }
  return save(song, record, record.plainLyrics, response.body, store);
}

async function save(
  song: Song,
  record: LyricRecord,
  plainLyrics: string,
  body: Uint8Array,
  store: Store,
): Promise<SongResult> {
  const name = baseName(record.artistName, record.trackName);
  const encoder = new TextEncoder();
  const files: [string, Uint8Array][] = [
    [`${name}.json`, body],
    [`${name}.txt`, encoder.encode(plainLyrics)],
  ];
  const { syncedLyrics } = record;
  const hasLineTimes = syncedLyrics !== null && syncedLyrics.trim() !== "";
  if (hasLineTimes) {
    files.push([`${name}.lrc`, encoder.encode(syncedLyrics)]);
  }
  let savedCount = 0;
  try {
    for (const [fileName, content] of files) {
      if (await store.saveIfAbsent(fileName, content)) {
        savedCount += 1;
      }
    }
  } catch (error) {
    if (!isSystemError(error)) {
      throw error;
    }
    return failure(song, "file write failed");
  }
  return { song, outcome: "GO", hasLineTimes, savedCount, keptCount: files.length - savedCount };
}

function failureReason(status: number | null): string {
  switch (status) {
    case null:
      return "network failure";
    case 404:
      return "record not found";
    case 429:
      return "rate limited";
    default:
      return `service error: HTTP ${status}`;
  }
}

function failure(song: Song, reason: string): SongResult {
  return { song, outcome: "ERROR", reason };
}
