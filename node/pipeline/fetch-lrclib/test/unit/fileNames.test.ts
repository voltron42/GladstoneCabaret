import assert from "node:assert/strict";
import { test } from "node:test";
import { baseName } from "../../src/fileNames.ts";

test("makes safe names", () => {
  assert.equal(baseName("Guns N' Roses", "Sweet Child O' Mine"), "Guns N' Roses - Sweet Child O' Mine");
  assert.equal(baseName("AC/DC", 'a<b>c:d"e\\f|g?h*i\tj'), "AC_DC - a_b_c_d_e_f_g_h_i_j");
  assert.equal(baseName("A", "Wait... "), "A - Wait");
});
