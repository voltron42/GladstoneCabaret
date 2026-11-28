/** Checks on parsed JSON, shared by the song list and lyric record. */

export function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** True for a string that can be written as UTF-8, which excludes lone surrogates. */
export function isEncodableText(value: unknown): value is string {
  return typeof value === "string" && value.isWellFormed();
}

export function isNonBlankText(value: unknown): value is string {
  return isEncodableText(value) && value.trim() !== "";
}
