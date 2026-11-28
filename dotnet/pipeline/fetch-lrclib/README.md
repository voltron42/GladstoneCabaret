# fetch-lrclib (C#)

Proof of concept: fetches the pinned LRCLIB lyric record for each test song. Behaviour follows the [shared spec](../../../pipeline/specs/fetch-lrclib.md).

Needs the .NET 10 SDK. Run commands from this folder.

## Run

```
dotnet run --project src/FetchLrclib -- --songs ../../../pipeline/testingData/shoppingList.json --output ../../../pipeline/testingData/lyrics
```

The output folder is ignored by git. Existing files are never overwritten. To compare implementations, give each an empty output folder.

## Check

```
dotnet build                          # compiles with analyzers; warnings are errors
dotnet format --verify-no-changes     # formatting and style
dotnet test
```

Set `LRCLIB_LIVE=1` to include the test that calls the real service. Without it, that test returns early and is counted as **passed**, not skipped, because this xUnit version has no simple way to skip while running. The Python and Node versions report it as skipped.

## Coverage

```
dotnet test tests/FetchLrclib.Tests --collect:"XPlat Code Coverage" --results-directory TestResults/unit
dotnet test tests/FetchLrclib.IntegrationTests --collect:"XPlat Code Coverage" --results-directory TestResults/integration
```

Each writes a Cobertura XML report. `Program.cs` and `PhysicalFileSystem.cs` are thin adapters without unit tests. Integration tests cover the file system adapter; `Program.cs` is checked only by running the tool.
