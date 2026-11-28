from fetch_lrclib.fetch_service import Outcome, SongResult
from fetch_lrclib.report import exit_code, format_report
from fetch_lrclib.song_list import Song

SONG = Song("A", "One", 1)
GO = SongResult(SONG, Outcome.GO, "", True, 3, 0)
NO_GO = SongResult(SONG, Outcome.NO_GO, "instrumental")
ERROR = SongResult(SONG, Outcome.ERROR, "network failure")


def test_writes_one_line_per_song_and_a_summary() -> None:
    report = format_report([GO, SongResult(SONG, Outcome.GO, "", False, 0, 2), NO_GO, ERROR])

    assert report == (
        "GO     A - One (line times: yes; saved: 3, kept: 0)\n"
        "GO     A - One (line times: no; saved: 0, kept: 2)\n"
        "NO-GO  A - One (instrumental)\n"
        "ERROR  A - One (network failure)\n"
        "Summary: go=2, no-go=1, error=1\n"
    )


def test_exit_code_reflects_the_worst_outcome() -> None:
    assert exit_code([GO]) == 0
    assert exit_code([GO, NO_GO]) == 1
    assert exit_code([GO, NO_GO, ERROR]) == 2
