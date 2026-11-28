"""Whole runs through the real components, with the network and file system replaced."""

import io
import os

from doubles import FakeFileSystem, usable_response

from fetch_lrclib.app import Dependencies, run
from fetch_lrclib.lrclib_client import FetchResponse
from fetch_lrclib.lyric_file_store import LyricFileStore


class RoutingClient:
    def get_record(self, record_id: int) -> FetchResponse:
        return usable_response(1) if record_id == 1 else FetchResponse(404)


def test_a_second_run_keeps_files_including_corrections() -> None:
    file_system = FakeFileSystem()
    songs = (
        '[{"artist":"A","songTitle":"One","lrclibRecordId":1},'
        '{"artist":"B","songTitle":"Two","lrclibRecordId":2}]'
    )
    dependencies = Dependencies(
        lambda _: songs, RoutingClient(), lambda directory: LyricFileStore(file_system, directory)
    )
    arguments = ["--songs", "list", "--output", "out"]
    corrected = os.path.join("out", "Artist - Track.txt")

    first_output = io.StringIO()
    first_code = run(arguments, dependencies, first_output, io.StringIO())
    file_system.files[corrected] = b"corrected"
    second_output = io.StringIO()
    run(arguments, dependencies, second_output, io.StringIO())

    assert first_code == 2
    assert first_output.getvalue() == (
        "GO     A - One (line times: no; saved: 2, kept: 0)\n"
        "ERROR  B - Two (record not found)\n"
        "Summary: go=1, no-go=0, error=1\n"
    )
    assert second_output.getvalue().startswith("GO     A - One (line times: no; saved: 0, kept: 2)\n")
    assert file_system.files[corrected] == b"corrected"
    assert len(file_system.files) == 2
