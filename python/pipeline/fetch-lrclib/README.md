# fetch-lrclib (Python)

Proof of concept: fetches the pinned LRCLIB lyric record for each test song. Behaviour follows the [shared spec](../../../pipeline/specs/fetch-lrclib.md).

Needs Python 3.13 or later (tested on 3.14.6). The tool itself uses only the standard library. Run commands from this folder, in Git Bash.

## Set up

This creates a private environment (`.venv`) for this project's development tools.

```
python -m venv .venv
.venv/Scripts/python -m pip install -e ".[dev]"
```

On macOS or Linux, and after activating the environment, the path differs: use `.venv/bin/python`.

## Run

```
.venv/Scripts/python -m fetch_lrclib --songs ../../../pipeline/testingData/shoppingList.json --output ../../../pipeline/testingData/lyrics
```

The output folder is ignored by git. Existing files are never overwritten. To compare implementations, give each an empty output folder.

## Check

```
.venv/Scripts/python -m ruff check .
.venv/Scripts/python -m ruff format --check .
.venv/Scripts/python -m mypy
.venv/Scripts/python -m pytest
```

Python does not enforce types when it runs, so the `mypy` step is required. Set `LRCLIB_LIVE=1` to include the test that calls the real service.

## Coverage

```
.venv/Scripts/python -m pytest tests/unit --cov --cov-report=term-missing
.venv/Scripts/python -m pytest tests/integration --cov --cov-report=term-missing
```

`__main__.py` and `physical_file_system.py` are thin adapters without unit tests. Integration tests cover the file system adapter; `__main__.py` is checked only by running the tool.

## Notes

- Ruff targets Python 3.13 so that it keeps the parentheses in `except (A, B):`. Its 3.14 style removes them.
- The standard library's request timeout limits each network operation, not the whole request. The client adds an overall deadline while reading the body, but a server that sends headers very slowly could exceed the spec's 30 seconds.
