export const BASE_URL = "https://lrclib.net";
export const TIMEOUT_MILLISECONDS = 30_000;
export const USER_AGENT = "fetch-lrclib-poc";

/** A status of null means the request never completed. */
export interface FetchResponse {
  readonly status: number | null;
  readonly body: Uint8Array;
  readonly hasRetryAfter: boolean;
}

export const NETWORK_FAILURE: FetchResponse = { status: null, body: new Uint8Array(), hasRetryAfter: false };

export interface Client {
  getRecord(recordId: number): Promise<FetchResponse>;
}

export interface ClientOptions {
  readonly baseUrl?: string;
  readonly timeoutMilliseconds?: number;
  readonly fetch?: typeof fetch;
}

export function createLrclibClient(options: ClientOptions = {}): Client {
  const {
    baseUrl = BASE_URL,
    timeoutMilliseconds = TIMEOUT_MILLISECONDS,
    fetch: send = globalThis.fetch,
  } = options;
  return {
    async getRecord(recordId) {
      try {
        // The signal covers the whole exchange, including reading the body.
        const response = await send(`${baseUrl}/api/get/${recordId}`, {
          headers: { "User-Agent": USER_AGENT },
          signal: AbortSignal.timeout(timeoutMilliseconds),
        });
        return {
          status: response.status,
          body: new Uint8Array(await response.arrayBuffer()),
          hasRetryAfter: response.headers.has("Retry-After"),
        };
      } catch {
        return NETWORK_FAILURE;
      }
    },
  };
}
