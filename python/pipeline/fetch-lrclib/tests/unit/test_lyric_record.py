import pytest
from doubles import usable_response

from fetch_lrclib.lyric_record import LyricRecord, parse_lyric_record


def test_reads_a_usable_record() -> None:
    body = usable_response(syncedLyrics="[00:01.00] made up").body

    assert parse_lyric_record(body, 1) == LyricRecord(
        "Artist", "Track", False, "made up line", "[00:01.00] made up"
    )


def test_allows_absent_lyrics() -> None:
    body = b'{"id":1,"artistName":"A","trackName":"T","instrumental":true}'

    assert parse_lyric_record(body, 1) == LyricRecord("A", "T", True, None, None)


@pytest.mark.parametrize(
    "body",
    [
        b"<html>",
        b"\xff",
        b"[]",
        b'{"artistName":"A","trackName":"T","instrumental":false}',
        b'{"id":2,"artistName":"A","trackName":"T","instrumental":false}',
        b'{"id":"1","artistName":"A","trackName":"T","instrumental":false}',
        b'{"id":true,"artistName":"A","trackName":"T","instrumental":false}',
        b'{"id":1,"trackName":"T","instrumental":false}',
        b'{"id":1,"artistName":"A","trackName":"  ","instrumental":false}',
        b'{"id":1,"artistName":"A","trackName":"T"}',
        b'{"id":1,"artistName":"A","trackName":"T","instrumental":"no"}',
        b'{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":3}',
        b'{"id":1,"artistName":"A","trackName":"T","instrumental":false,"syncedLyrics":[]}',
        b'{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":"\\ud800"}',
    ],
)
def test_rejects_unusable_bodies(body: bytes) -> None:
    assert parse_lyric_record(body, 1) is None
