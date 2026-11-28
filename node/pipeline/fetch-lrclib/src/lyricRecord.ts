import { isEncodableText, isNonBlankText, isObject } from "./jsonValues.ts";

export interface LyricRecord {
  readonly artistName: string;
  readonly trackName: string;
  readonly instrumental: boolean;
  readonly plainLyrics: string | null;
  readonly syncedLyrics: string | null;
}

/** Returns null when the body is not a usable record for the requested ID. */
export function parseLyricRecord(body: Uint8Array, expectedId: number): LyricRecord | null {
  let record: unknown;
  try {
    record = JSON.parse(new TextDecoder("utf-8", { fatal: true, ignoreBOM: true }).decode(body));
  } catch {
    return null;
  }
  if (!isObject(record) || record["id"] !== expectedId) {
    return null;
  }
  const { artistName, trackName, instrumental, plainLyrics = null, syncedLyrics = null } = record;
  if (!isNonBlankText(artistName) || !isNonBlankText(trackName) || typeof instrumental !== "boolean") {
    return null;
  }
  if (!isOptionalText(plainLyrics) || !isOptionalText(syncedLyrics)) {
    return null;
  }
  return { artistName, trackName, instrumental, plainLyrics, syncedLyrics };
}

function isOptionalText(value: unknown): value is string | null {
  return value === null || isEncodableText(value);
}
