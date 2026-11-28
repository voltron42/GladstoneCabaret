import assert from "node:assert/strict";
import { test } from "node:test";
import { run, type Dependencies } from "../../src/app.ts";
import { FakeClient, FakeStore, systemError, usableResponse } from "../doubles.ts";

const validList = '[{"artist":"A","songTitle":"One","lrclibRecordId":1}]';

async function runApp(args: string[], readText: Dependencies["readText"] = () => Promise.resolve(validList)) {
  let output = "";
  let errorOutput = "";
  const dependencies = {
    readText,
    client: new FakeClient(usableResponse()),
    createStore: () => new FakeStore(),
  };
  const code = await run(
    args,
    dependencies,
    (text) => (output += text),
    (text) => (errorOutput += text),
  );
  return { code, output, errorOutput };
}

const goodArguments = ["--songs", "list", "--output", "out"];

test("accepts options in either order", async () => {
  const paths: string[] = [];

  const result = await runApp(["--output", "out", "--songs", "list"], (path) => {
    paths.push(path);
    return Promise.resolve(validList);
  });

  assert.deepEqual([result.code, result.errorOutput, paths], [0, "", ["list"]]);
  assert.match(result.output, /Summary: go=1, no-go=0, error=0\n$/);
});

test("rejects wrong arguments", async () => {
  const wrong = [
    [],
    ["--songs", "list"],
    ["--songs", "list", "--songs", "other"],
    ["--songs", "list", "--wrong", "out"],
    ["--songs", "", "--output", "out"],
    [...goodArguments, "extra"],
  ];
  for (const args of wrong) {
    const result = await runApp(args);

    assert.deepEqual([result.code, result.output], [64, ""]);
    assert.match(result.errorOutput, /^usage:/);
  }
});

test("rejects an unreadable song list", async () => {
  const result = await runApp(goodArguments, () => Promise.reject(systemError("denied")));

  assert.deepEqual([result.code, result.errorOutput], [64, "cannot use song list: denied\n"]);
});

test("rejects an invalid song list", async () => {
  const result = await runApp(goodArguments, () => Promise.resolve("[]"));

  assert.equal(result.errorOutput, "cannot use song list: song list must be a non-empty JSON array\n");
});

test("does not hide a programming error", async () => {
  await assert.rejects(
    runApp(goodArguments, () => Promise.reject(new TypeError("bug"))),
    TypeError,
  );
});
