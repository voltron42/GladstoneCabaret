/** Stand-ins for the web client, file store and file system. */
import type { Client, FetchResponse } from "../src/lrclibClient.ts";
import type { Store } from "../src/lyricFileStore.ts";
import type { FileSystem } from "../src/physicalFileSystem.ts";

export const bytes = (text: string): Uint8Array => new TextEncoder().encode(text);

export const reply = (status: number | null, hasRetryAfter = false): FetchResponse => ({
  status,
  body: new Uint8Array(),
  hasRetryAfter,
});

export function usableResponse(changes: Record<string, unknown> = {}): FetchResponse {
  const record = {
    id: 1,
    artistName: "Artist",
    trackName: "Track",
    instrumental: false,
    plainLyrics: "made up line",
    syncedLyrics: null,
    ...changes,
  };
  return { status: 200, body: bytes(JSON.stringify(record)), hasRetryAfter: false };
}

export function systemError(message: string): Error {
  return Object.assign(new Error(message), { code: "EACCES", syscall: "open" });
}

export class FakeClient implements Client {
  readonly requestedIds: number[] = [];
  readonly #responses: FetchResponse[];

  constructor(...responses: FetchResponse[]) {
    this.#responses = responses;
  }

  getRecord(recordId: number): Promise<FetchResponse> {
    this.requestedIds.push(recordId);
    const response = this.#responses.shift();
    return response ? Promise.resolve(response) : Promise.reject(new Error("unexpected request"));
  }
}

export class FakeStore implements Store {
  readonly files: Map<string, Uint8Array>;
  readonly #failure: Error | undefined;

  constructor(files: [string, Uint8Array][] = [], failure?: Error) {
    this.files = new Map(files);
    this.#failure = failure;
  }

  saveIfAbsent(fileName: string, content: Uint8Array): Promise<boolean> {
    if (this.#failure) {
      return Promise.reject(this.#failure);
    }
    if (this.files.has(fileName)) {
      return Promise.resolve(false);
    }
    this.files.set(fileName, content);
    return Promise.resolve(true);
  }
}

export interface FakeFileSystemOptions {
  readonly files?: [string, Uint8Array][];
  readonly failWrite?: boolean;
  /** Simulates another process creating the destination just before the move. */
  readonly loseRace?: boolean;
}

export class FakeFileSystem implements FileSystem {
  readonly files: Map<string, Uint8Array>;
  readonly #options: FakeFileSystemOptions;

  constructor(options: FakeFileSystemOptions = {}) {
    this.#options = options;
    this.files = new Map(options.files);
  }

  fileExists(path: string): Promise<boolean> {
    return Promise.resolve(this.files.has(path));
  }

  createDirectory(): Promise<void> {
    return Promise.resolve();
  }

  writeBytes(path: string, content: Uint8Array): Promise<void> {
    this.files.set(path, content);
    return this.#options.failWrite ? Promise.reject(systemError("disk full")) : Promise.resolve();
  }

  moveWithoutReplacing(source: string, destination: string): Promise<void> {
    if (this.#options.loseRace) {
      this.files.set(destination, bytes("theirs"));
    }
    const content = this.files.get(source);
    if (this.files.has(destination) || content === undefined) {
      return Promise.reject(systemError("cannot move"));
    }
    this.files.set(destination, content);
    this.files.delete(source);
    return Promise.resolve();
  }

  deleteFile(path: string): Promise<void> {
    this.files.delete(path);
    return Promise.resolve();
  }
}
