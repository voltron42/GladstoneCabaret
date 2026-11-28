import assert from "node:assert/strict";
import { test } from "node:test";
import { isSystemError } from "../../src/systemError.ts";
import { systemError } from "../doubles.ts";

test("recognizes operating system errors only", () => {
  assert.equal(isSystemError(systemError("denied")), true);
  assert.equal(isSystemError(new TypeError("bug")), false);
  assert.equal(isSystemError("text"), false);
});
