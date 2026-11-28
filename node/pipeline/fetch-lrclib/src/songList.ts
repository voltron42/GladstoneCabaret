import { isNonBlankText, isObject } from "./jsonValues.ts";

export interface Song {
  readonly artist: string;
  readonly songTitle: string;
  readonly lrclibRecordId: number;
}

export class SongListError extends Error {}

export function parseSongList(text: string): Song[] {
  let entries: unknown;
  try {
    entries = JSON.parse(text);
  } catch {
    throw new SongListError("song list is not valid JSON text");
  }
  if (!Array.isArray(entries) || entries.length === 0) {
    throw new SongListError("song list must be a non-empty JSON array");
  }
  return entries.map((entry: unknown, index) => parseSong(entry, index + 1));
}

function parseSong(entry: unknown, position: number): Song {
  if (!isObject(entry)) {
    throw new SongListError(`song ${position} must be an object`);
  }
  return {
    artist: requireText(entry, "artist", position),
    songTitle: requireText(entry, "songTitle", position),
    lrclibRecordId: requireRecordId(entry, position),
  };
}

function requireText(entry: Record<string, unknown>, name: string, position: number): string {
  const value = entry[name];
  if (isNonBlankText(value)) {
    return value;
  }
  throw new SongListError(`song ${position}: ${name} must be a non-blank string`);
}

function requireRecordId(entry: Record<string, unknown>, position: number): number {
  const value = entry["lrclibRecordId"];
  if (typeof value === "number" && Number.isSafeInteger(value) && value > 0) {
    return value;
  }
  throw new SongListError(`song ${position}: lrclibRecordId must be a positive integer`);
}
