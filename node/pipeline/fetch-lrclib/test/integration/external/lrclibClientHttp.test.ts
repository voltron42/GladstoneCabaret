import assert from "node:assert/strict";
import { createServer, type RequestListener } from "node:http";
import type { AddressInfo } from "node:net";
import { test, type TestContext } from "node:test";
import { createLrclibClient, NETWORK_FAILURE } from "../../../src/lrclibClient.ts";
import { parseLyricRecord } from "../../../src/lyricRecord.ts";
import { bytes } from "../../doubles.ts";

async function serve(context: TestContext, handler: RequestListener): Promise<string> {
  const server = createServer(handler);
  await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
  context.after(() => {
    server.closeAllConnections();
    server.close();
  });
  return `http://127.0.0.1:${(server.address() as AddressInfo).port}`;
}

test("reads status, body and retry-after over real HTTP", async (context) => {
  const baseUrl = await serve(context, (_request, response) => {
    response.writeHead(503, { "Retry-After": "5" }).end("no");
  });

  const result = await createLrclibClient({ baseUrl }).getRecord(1);

  assert.deepEqual(result, { status: 503, body: bytes("no"), hasRetryAfter: true });
});

test("a server that never answers times out", async (context) => {
  const baseUrl = await serve(context, () => undefined);

  const result = await createLrclibClient({ baseUrl, timeoutMilliseconds: 500 }).getRecord(1);

  assert.equal(result, NETWORK_FAILURE);
});

// Checks the real service still matches what our stand-ins assume.
test("a live record is usable", { skip: process.env["LRCLIB_LIVE"] !== "1" }, async () => {
  const result = await createLrclibClient().getRecord(16012238);

  assert.equal(result.status, 200);
  assert.notEqual(parseLyricRecord(result.body, 16012238), null);
});
