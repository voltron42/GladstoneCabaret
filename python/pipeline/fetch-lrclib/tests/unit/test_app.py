import io
from collections.abc import Callable

import pytest
from doubles import FakeClient, FakeStore, usable_response

from fetch_lrclib.app import Dependencies, run

VALID_LIST = '[{"artist":"A","songTitle":"One","lrclibRecordId":1}]'


def run_app(arguments: list[str], read_text: Callable[[str], str]) -> tuple[int, str, str]:
    output, error_output = io.StringIO(), io.StringIO()
    dependencies = Dependencies(read_text, FakeClient(usable_response()), lambda _: FakeStore())
    code = run(arguments, dependencies, output, error_output)
    return code, output.getvalue(), error_output.getvalue()


def test_accepts_options_in_either_order() -> None:
    paths: list[str] = []

    def read_text(path: str) -> str:
        paths.append(path)
        return VALID_LIST

    code, output, error_output = run_app(["--output", "out", "--songs", "list"], read_text)

    assert (code, error_output, paths) == (0, "", ["list"])
    assert output.endswith("Summary: go=1, no-go=0, error=0\n")


@pytest.mark.parametrize(
    "arguments",
    [
        [],
        ["--songs", "list"],
        ["--songs", "list", "--songs", "other"],
        ["--songs", "list", "--wrong", "out"],
        ["--songs", "", "--output", "out"],
        ["--songs", "list", "--output", "out", "extra"],
    ],
)
def test_rejects_wrong_arguments(arguments: list[str]) -> None:
    code, output, error_output = run_app(arguments, lambda _: VALID_LIST)

    assert (code, output) == (64, "")
    assert error_output.startswith("usage:")


@pytest.mark.parametrize(
    "failure", [FileNotFoundError(), PermissionError(), UnicodeDecodeError("utf-8", b"", 0, 1, "bad")]
)
def test_rejects_an_unreadable_song_list(failure: Exception) -> None:
    def read_text(_: str) -> str:
        raise failure

    code, _, error_output = run_app(["--songs", "list", "--output", "out"], read_text)

    assert code == 64
    assert error_output.startswith("cannot use song list:")


def test_rejects_an_invalid_song_list() -> None:
    code, _, error_output = run_app(["--songs", "list", "--output", "out"], lambda _: "[]")

    assert code == 64
    assert error_output == "cannot use song list: song list must be a non-empty JSON array\n"
