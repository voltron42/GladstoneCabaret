# fetch-lrclib (Node, TypeScript)

Proof of concept: fetches the pinned LRCLIB lyric record for each test song. Behaviour follows the [shared spec](../../../pipeline/specs/fetch-lrclib.md).

Needs Node 24 or later, which runs TypeScript files directly. There is no build step, and no runtime dependencies. Run commands from this folder.

## Set up

```
npm ci
```

## Run

```
npm start -- --songs ../../../pipeline/testingData/shoppingList.json --output ../../../pipeline/testingData/lyrics
```

`npm start` adds its own header lines. To see only the report, run `node src/main.ts` with the same options.

The output folder is ignored by git. Existing files are never overwritten. To compare implementations, give each an empty output folder.

## Check

```
npm run typecheck
npm run lint
npm test
```

Node removes types without checking them, so the `typecheck` step is required. Set `LRCLIB_LIVE=1` to include the test that calls the real service.

## Coverage

```
npm run coverage:unit
npm run coverage:integration
```

Node's coverage feature is still marked experimental, and it lists only files that tests load. `main.ts` (checked only by running the tool) and `physicalFileSystem.ts` (covered by integration tests) are thin adapters without unit tests, so neither appears in unit coverage.

## Notes

- TypeScript is pinned to 6.0 because the lint plugin does not yet support TypeScript 7.
- JavaScript does not distinguish `5.0` from `5`, so a record ID written as `5.0` is accepted here and rejected by the C# and Python versions.
