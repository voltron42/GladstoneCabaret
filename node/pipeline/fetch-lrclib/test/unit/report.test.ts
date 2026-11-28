import assert from "node:assert/strict";
import { test } from "node:test";
import type { SongResult } from "../../src/fetchService.ts";
import { exitCode, formatReport } from "../../src/report.ts";

const song = { artist: "A", songTitle: "One", lrclibRecordId: 1 };
const go: SongResult = { song, outcome: "GO", hasLineTimes: true, savedCount: 3, keptCount: 0 };
const kept: SongResult = { song, outcome: "GO", hasLineTimes: false, savedCount: 0, keptCount: 2 };
const noGo: SongResult = { song, outcome: "NO-GO", reason: "instrumental" };
const error: SongResult = { song, outcome: "ERROR", reason: "network failure" };

test("writes one line per song and a summary", () => {
  assert.equal(
    formatReport([go, kept, noGo, error]),
    "GO     A - One (line times: yes; saved: 3, kept: 0)\n" +
      "GO     A - One (line times: no; saved: 0, kept: 2)\n" +
      "NO-GO  A - One (instrumental)\n" +
      "ERROR  A - One (network failure)\n" +
      "Summary: go=2, no-go=1, error=1\n",
  );
});

test("the exit code reflects the worst outcome", () => {
  assert.deepEqual([exitCode([go]), exitCode([go, noGo]), exitCode([go, noGo, error])], [0, 1, 2]);
});
