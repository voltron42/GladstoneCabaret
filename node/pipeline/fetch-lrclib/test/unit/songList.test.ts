import assert from "node:assert/strict";
import { test } from "node:test";
import { parseSongList, SongListError } from "../../src/songList.ts";

test("reads songs and ignores other properties", () => {
  const text = '[{"artist":"A","songTitle":"T","lrclibRecordId":5,"category":"x"}]';

  assert.deepEqual(parseSongList(text), [{ artist: "A", songTitle: "T", lrclibRecordId: 5 }]);
});

const invalidLists = [
  "not json",
  "{}",
  "[]",
  "[1]",
  '[{"songTitle":"T","lrclibRecordId":5}]',
  '[{"artist":" ","songTitle":"T","lrclibRecordId":5}]',
  '[{"artist":"A","songTitle":7,"lrclibRecordId":5}]',
  '[{"artist":"A","songTitle":"T"}]',
  '[{"artist":"A","songTitle":"T","lrclibRecordId":0}]',
  '[{"artist":"A","songTitle":"T","lrclibRecordId":"5"}]',
  '[{"artist":"A","songTitle":"T","lrclibRecordId":1.5}]',
];

for (const text of invalidLists) {
  test(`rejects ${text}`, () => {
    assert.throws(() => parseSongList(text), SongListError);
  });
}
