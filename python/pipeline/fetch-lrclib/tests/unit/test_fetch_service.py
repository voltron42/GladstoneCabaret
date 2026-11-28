import pytest
from doubles import FakeClient, FakeStore, usable_response

from fetch_lrclib.fetch_service import FetchService, Outcome, SongResult
from fetch_lrclib.lrclib_client import FetchResponse
from fetch_lrclib.song_list import Song

FIRST = Song("A", "One", 1)
SECOND = Song("B", "Two", 2)


def fetch_one(response: FetchResponse, store: FakeStore | None = None) -> SongResult:
    return FetchService(FakeClient(response), store or FakeStore()).fetch_all([FIRST])[0]


def test_a_record_with_line_times_saves_three_files() -> None:
    store = FakeStore()
    response = usable_response(syncedLyrics="timed")

    assert fetch_one(response, store) == SongResult(FIRST, Outcome.GO, "", True, 3, 0)
    assert list(store.files.items()) == [
        ("Artist - Track.json", response.body),
        ("Artist - Track.txt", b"made up line"),
        ("Artist - Track.lrc", b"timed"),
    ]


@pytest.mark.parametrize("synced", [None, " "])
def test_a_record_without_line_times_is_still_a_go(synced: str | None) -> None:
    assert fetch_one(usable_response(syncedLyrics=synced)) == SongResult(FIRST, Outcome.GO, "", False, 2, 0)


def test_existing_files_are_counted_as_kept() -> None:
    store = FakeStore({"Artist - Track.txt": b"corrected"})

    assert fetch_one(usable_response(), store) == SongResult(FIRST, Outcome.GO, "", False, 1, 1)


@pytest.mark.parametrize(
    ("changes", "reason"),
    [
        ({"instrumental": True}, "instrumental"),
        ({"plainLyrics": None}, "no plain lyrics"),
        ({"plainLyrics": "  "}, "no plain lyrics"),
    ],
)
def test_a_record_without_usable_lyrics_is_a_no_go_and_saves_nothing(
    changes: dict[str, object], reason: str
) -> None:
    store = FakeStore()

    assert fetch_one(usable_response(**changes), store) == SongResult(FIRST, Outcome.NO_GO, reason)
    assert store.files == {}


@pytest.mark.parametrize(
    ("status", "reason"),
    [
        (None, "network failure"),
        (404, "record not found"),
        (429, "rate limited"),
        (500, "service error: HTTP 500"),
        (200, "invalid response"),
    ],
)
def test_failed_requests_are_errors(status: int | None, reason: str) -> None:
    assert fetch_one(FetchResponse(status)) == SongResult(FIRST, Outcome.ERROR, reason)


def test_a_failed_write_is_an_error() -> None:
    result = fetch_one(usable_response(), FakeStore(failure=PermissionError()))

    assert result == SongResult(FIRST, Outcome.ERROR, "file write failed")


@pytest.mark.parametrize(("status", "has_retry_after"), [(429, False), (503, True)])
def test_a_request_to_wait_stops_further_requests(status: int, has_retry_after: bool) -> None:
    client = FakeClient(FetchResponse(status, b"", has_retry_after))

    results = FetchService(client, FakeStore()).fetch_all([FIRST, SECOND])

    assert client.requested_ids == [1]
    assert results[1] == SongResult(SECOND, Outcome.ERROR, "not attempted: service asked us to wait")


@pytest.mark.parametrize(("status", "has_retry_after"), [(503, False), (200, True)])
def test_other_responses_do_not_stop_later_requests(status: int, has_retry_after: bool) -> None:
    client = FakeClient(FetchResponse(status, b"", has_retry_after), usable_response(2))

    results = FetchService(client, FakeStore()).fetch_all([FIRST, SECOND])

    assert client.requested_ids == [1, 2]
    assert results[1].outcome is Outcome.GO
