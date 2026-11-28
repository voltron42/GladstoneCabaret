import email.message
import http.client
import urllib.error
import urllib.request
from typing import Self
from unittest.mock import patch

import pytest

from fetch_lrclib.lrclib_client import NETWORK_FAILURE, TIMEOUT_SECONDS, FetchResponse, LrclibClient


class StubReply:
    def __init__(self, status: int, chunks: list[bytes], headers: tuple[str, ...] = ()) -> None:
        self.status = status
        self.headers = headers
        self._chunks = list(chunks)

    def read1(self, size: int) -> bytes:
        return self._chunks.pop(0) if self._chunks else b""

    def __enter__(self) -> Self:
        return self

    def __exit__(self, *details: object) -> None:
        pass


def test_requests_the_record_with_a_generic_user_agent_and_timeout() -> None:
    sent: list[tuple[urllib.request.Request, float]] = []

    def open_url(request: urllib.request.Request, timeout: float) -> StubReply:
        sent.append((request, timeout))
        return StubReply(200, [b"a", b"b"])

    response = LrclibClient(open_url=open_url).get_record(42)

    request, timeout = sent[0]
    assert request.full_url == "https://lrclib.net/api/get/42"
    assert request.headers == {"User-agent": "fetch-lrclib-poc"}
    assert timeout == TIMEOUT_SECONDS == 30
    assert response == FetchResponse(200, b"ab", False)


def test_reports_retry_after() -> None:
    client = LrclibClient(open_url=lambda *_: StubReply(503, [], ("Retry-After",)))

    assert client.get_record(1) == FetchResponse(503, b"", True)


@pytest.mark.parametrize(
    "failure", [urllib.error.URLError("down"), TimeoutError(), http.client.IncompleteRead(b"")]
)
def test_turns_failures_into_network_failure(failure: Exception) -> None:
    def open_url(*_: object) -> StubReply:
        raise failure

    assert LrclibClient(open_url=open_url).get_record(1) == NETWORK_FAILURE


def test_a_slow_body_exceeds_the_overall_deadline() -> None:
    times = iter([0.0, 10.0, 31.0])
    client = LrclibClient(open_url=lambda *_: StubReply(200, [b"a", b"b"]), clock=lambda: next(times))

    assert client.get_record(1) == NETWORK_FAILURE


def test_default_opener_returns_error_statuses_as_replies() -> None:
    headers = email.message.Message()
    headers["Retry-After"] = "5"
    error = urllib.error.HTTPError("url", 429, "slow down", headers, None)

    with (
        patch.object(urllib.request, "urlopen", side_effect=error),
        patch.object(error, "read1", create=True) as read,
    ):
        read.return_value = b""
        response = LrclibClient().get_record(1)

    assert response == FetchResponse(429, b"", True)


def test_default_opener_passes_successful_replies_through() -> None:
    with patch.object(urllib.request, "urlopen", return_value=StubReply(200, [b"ok"])) as urlopen:
        response = LrclibClient().get_record(1)

    assert urlopen.call_args.kwargs == {"timeout": 30.0}
    assert response == FetchResponse(200, b"ok", False)
