# Results: fetch lyrics with LRCLIB

Tested 2026-11-28 on the test machine. Each implementation follows the [shared spec](../specs/fetch-lrclib.md).

## Tool results

- All five pinned records were a **go**. Four have line times; Ernie Halter's song does not.
- All three implementations wrote files identical, byte for byte, to those saved on 2026-11-27, and printed identical reports. A second run kept every file.
- A record ID that does not exist produced `record not found` in all three. Invalid arguments and song lists gave the same exit code in all three.
- This shows that the pinned records can be fetched. It does **not** test finding lyrics by artist and title, coverage for other songs, or lyric accuracy.
- Rate limiting and server errors were tested only with stand-ins.

**Integration:** LRCLIB is a web API needing only an HTTP request and JSON parsing. All three could call it without a third-party runtime library.

## Language comparison

This task is small and does not use any AI tool, so it says little about how the languages will compare in later steps.

| | C# | Python | Node (TypeScript) |
|---|---|---|---|
| Versions | .NET SDK 10.0.402 | Python 3.14.6 | Node 24.16.0, TypeScript 6.0.3 |
| Source files / non-blank lines | 10 / 402 | 12 / 388 | 12 / 394 |
| Test non-blank lines | 528 | 440 | 530 |
| Runtime dependencies | None | None | None |
| Test packages | xUnit, test SDK, test runner, coverlet (from the project template) | pytest, pytest-cov | Built into Node |
| Lint and type tools | Built into the SDK | ruff, mypy | TypeScript, ESLint and its TypeScript plugin, Prettier, Node type definitions |
| Coverage | XML report; needs another tool for a readable summary | Readable table | Readable table; feature is experimental |

Line counts come from one author writing one design three times, so small differences mean little.

## Problems found while building

- **C#:** the analyzers rejected a locale-dependent string call at build time.
- **Python:** the client tried to set a read-only property on HTTP error responses, so every error status would have failed. The type checker and the real-HTTP integration test caught it. The formatter's 3.14 style also produced unfamiliar `except` syntax.
- **Python:** the standard library cannot set one timeout for a whole request, so the client needs extra code and still does not cover slow headers.
- **Node:** the lint plugin does not support the latest TypeScript, so TypeScript is pinned to an older major version.

## Not yet assessed

How readable and maintainable each version is. That needs your review.
