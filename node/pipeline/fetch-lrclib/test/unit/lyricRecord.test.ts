import assert from "node:assert/strict";
import { test } from "node:test";
import { parseLyricRecord } from "../../src/lyricRecord.ts";
import { bytes, usableResponse } from "../doubles.ts";

test("reads a usable record", () => {
  const { body } = usableResponse({ syncedLyrics: "[00:01.00] made up" });

  assert.deepEqual(parseLyricRecord(body, 1), {
    artistName: "Artist",
    trackName: "Track",
    instrumental: false,
    plainLyrics: "made up line",
    syncedLyrics: "[00:01.00] made up",
  });
});

test("allows absent lyrics", () => {
  const record = parseLyricRecord(bytes('{"id":1,"artistName":"A","trackName":"T","instrumental":true}'), 1);

  assert.deepEqual([record?.plainLyrics, record?.syncedLyrics], [null, null]);
});

const unusableBodies = [
  "<html>",
  "[]",
  '{"artistName":"A","trackName":"T","instrumental":false}',
  '{"id":2,"artistName":"A","trackName":"T","instrumental":false}',
  '{"id":"1","artistName":"A","trackName":"T","instrumental":false}',
  '{"id":1,"trackName":"T","instrumental":false}',
  '{"id":1,"artistName":"A","trackName":"  ","instrumental":false}',
  '{"id":1,"artistName":"A","trackName":"T"}',
  '{"id":1,"artistName":"A","trackName":"T","instrumental":"no"}',
  '{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":3}',
  '{"id":1,"artistName":"A","trackName":"T","instrumental":false,"syncedLyrics":[]}',
  '{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":"\\ud800"}',
];

for (const body of unusableBodies) {
  test(`rejects ${body}`, () => {
    assert.equal(parseLyricRecord(bytes(body), 1), null);
  });
}

test("rejects a body that is not UTF-8", () => {
  assert.equal(parseLyricRecord(new Uint8Array([0xff]), 1), null);
});
