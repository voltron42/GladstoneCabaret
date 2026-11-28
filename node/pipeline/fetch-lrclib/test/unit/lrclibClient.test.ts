import assert from "node:assert/strict";
import { test } from "node:test";
import { createLrclibClient, NETWORK_FAILURE, TIMEOUT_MILLISECONDS } from "../../src/lrclibClient.ts";

test("requests the record with a generic user agent and a timeout signal", async () => {
  const sent: { url: unknown; init: RequestInit | undefined }[] = [];
  const client = createLrclibClient({
    fetch: (url, init) => {
      sent.push({ url, init });
      return Promise.resolve(new Response("ab"));
    },
  });

  const response = await client.getRecord(42);

  assert.equal(sent[0]?.url, "https://lrclib.net/api/get/42");
  assert.deepEqual(sent[0].init?.headers, { "User-Agent": "fetch-lrclib-poc" });
  assert.ok(sent[0].init.signal instanceof AbortSignal);
  assert.equal(TIMEOUT_MILLISECONDS, 30_000);
  assert.deepEqual(response, { status: 200, body: new Uint8Array([97, 98]), hasRetryAfter: false });
});

test("reports retry-after", async () => {
  const client = createLrclibClient({
    fetch: () => Promise.resolve(new Response("", { status: 503, headers: { "Retry-After": "5" } })),
  });

  const response = await client.getRecord(1);

  assert.deepEqual([response.status, response.hasRetryAfter], [503, true]);
});

test("turns failures into a network failure", async () => {
  const client = createLrclibClient({ fetch: () => Promise.reject(new TypeError("fetch failed")) });

  assert.equal(await client.getRecord(1), NETWORK_FAILURE);
});
