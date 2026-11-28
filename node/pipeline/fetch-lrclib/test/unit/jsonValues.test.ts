import assert from "node:assert/strict";
import { test } from "node:test";
import { isEncodableText, isNonBlankText, isObject } from "../../src/jsonValues.ts";

test("isObject accepts only plain JSON objects", () => {
  assert.deepEqual([{}, [], null, "a", 1].map(isObject), [true, false, false, false, false]);
});

test("isEncodableText rejects lone surrogates and non-strings", () => {
  assert.deepEqual(["a", "", "\ud800", 1].map(isEncodableText), [true, true, false, false]);
});

test("isNonBlankText rejects blank strings", () => {
  assert.deepEqual(["a", " \n", null].map(isNonBlankText), [true, false, false]);
});
