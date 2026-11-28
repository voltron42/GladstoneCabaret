import type { Outcome, SongResult } from "./fetchService.ts";

export function formatReport(results: readonly SongResult[]): string {
  const summary =
    `Summary: go=${count(results, "GO")}, no-go=${count(results, "NO-GO")}, ` +
    `error=${count(results, "ERROR")}`;
  return [...results.map(formatLine), summary].map((line) => `${line}\n`).join("");
}

export function exitCode(results: readonly SongResult[]): number {
  if (count(results, "ERROR") > 0) {
    return 2;
  }
  return count(results, "NO-GO") > 0 ? 1 : 0;
}

function count(results: readonly SongResult[], outcome: Outcome): number {
  return results.filter((result) => result.outcome === outcome).length;
}

function formatLine(result: SongResult): string {
  const detail =
    result.outcome === "GO"
      ? `line times: ${result.hasLineTimes ? "yes" : "no"}; saved: ${result.savedCount}, kept: ${result.keptCount}`
      : result.reason;
  return `${result.outcome.padEnd(7)}${result.song.artist} - ${result.song.songTitle} (${detail})`;
}
