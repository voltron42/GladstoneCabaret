import assert from "node:assert/strict";
import { test } from "node:test";
import { fetchAll, type SongResult } from "../../src/fetchService.ts";
import type { FetchResponse } from "../../src/lrclibClient.ts";
import { bytes, FakeClient, FakeStore, reply, systemError, usableResponse } from "../doubles.ts";

const first = { artist: "A", songTitle: "One", lrclibRecordId: 1 };
const second = { artist: "B", songTitle: "Two", lrclibRecordId: 2 };

async function fetchOne(response: FetchResponse, store = new FakeStore()): Promise<SongResult | undefined> {
  const results = await fetchAll([first], new FakeClient(response), store);
  return results[0];
}

const go = (hasLineTimes: boolean, savedCount: number, keptCount: number) => ({
  song: first,
  outcome: "GO",
  hasLineTimes,
  savedCount,
  keptCount,
});

test("a record with line times saves three files", async () => {
  const store = new FakeStore();
  const response = usableResponse({ syncedLyrics: "timed" });

  assert.deepEqual(await fetchOne(response, store), go(true, 3, 0));
  assert.deepEqual(
    [...store.files],
    [
      ["Artist - Track.json", response.body],
      ["Artist - Track.txt", bytes("made up line")],
      ["Artist - Track.lrc", bytes("timed")],
    ],
  );
});

test("a record without line times is still a go", async () => {
  for (const syncedLyrics of [null, " "]) {
    assert.deepEqual(await fetchOne(usableResponse({ syncedLyrics })), go(false, 2, 0));
  }
});

test("existing files are counted as kept", async () => {
  const store = new FakeStore([["Artist - Track.txt", bytes("corrected")]]);

  assert.deepEqual(await fetchOne(usableResponse(), store), go(false, 1, 1));
});

test("a record without usable lyrics is a no-go and saves nothing", async () => {
  const cases: [Record<string, unknown>, string][] = [
    [{ instrumental: true }, "instrumental"],
    [{ plainLyrics: null }, "no plain lyrics"],
    [{ plainLyrics: "  " }, "no plain lyrics"],
  ];
  for (const [changes, reason] of cases) {
    const store = new FakeStore();

    assert.deepEqual(await fetchOne(usableResponse(changes), store), {
      song: first,
      outcome: "NO-GO",
      reason,
    });
    assert.equal(store.files.size, 0);
  }
});

test("failed requests are errors", async () => {
  const cases: [number | null, string][] = [
    [null, "network failure"],
    [404, "record not found"],
    [429, "rate limited"],
    [500, "service error: HTTP 500"],
    [200, "invalid response"],
  ];
  for (const [status, reason] of cases) {
    assert.deepEqual(await fetchOne(reply(status)), { song: first, outcome: "ERROR", reason });
  }
});

test("a failed write is an error", async () => {
  const result = await fetchOne(usableResponse(), new FakeStore([], systemError("denied")));

  assert.deepEqual(result, { song: first, outcome: "ERROR", reason: "file write failed" });
});

test("a programming error is not reported as a failed write", async () => {
  await assert.rejects(fetchOne(usableResponse(), new FakeStore([], new TypeError("bug"))), TypeError);
});

test("a request to wait stops further requests", async () => {
  for (const response of [reply(429), reply(503, true)]) {
    const client = new FakeClient(response);

    const results = await fetchAll([first, second], client, new FakeStore());

    assert.deepEqual(client.requestedIds, [1]);
    assert.deepEqual(results[1], {
      song: second,
      outcome: "ERROR",
      reason: "not attempted: service asked us to wait",
    });
  }
});

test("other responses do not stop later requests", async () => {
  for (const response of [reply(503), reply(200, true)]) {
    const client = new FakeClient(response, usableResponse({ id: 2 }));

    const results = await fetchAll([first, second], client, new FakeStore());

    assert.deepEqual(client.requestedIds, [1, 2]);
    assert.equal(results[1]?.outcome, "GO");
  }
});
