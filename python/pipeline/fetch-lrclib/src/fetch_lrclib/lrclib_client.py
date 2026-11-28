import http.client
import time
import urllib.error
import urllib.request
from collections.abc import Callable
from dataclasses import dataclass
from typing import Protocol, Self, cast

BASE_URL = "https://lrclib.net"
TIMEOUT_SECONDS = 30.0
USER_AGENT = "fetch-lrclib-poc"
_CHUNK_SIZE = 65536


@dataclass(frozen=True)
class FetchResponse:
    """A status of None means the request never completed."""

    status: int | None
    body: bytes = b""
    has_retry_after: bool = False


NETWORK_FAILURE = FetchResponse(status=None)


class Client(Protocol):
    def get_record(self, record_id: int) -> FetchResponse: ...


class _Headers(Protocol):
    def __contains__(self, name: str, /) -> bool: ...


class HttpReply(Protocol):
    @property
    def status(self) -> int: ...

    @property
    def headers(self) -> _Headers: ...

    def read1(self, size: int, /) -> bytes: ...

    def __enter__(self) -> Self: ...

    def __exit__(self, *details: object) -> None: ...


OpenUrl = Callable[[urllib.request.Request, float], HttpReply]


def _open_url(request: urllib.request.Request, timeout: float) -> HttpReply:
    try:
        reply: HttpReply = urllib.request.urlopen(request, timeout=timeout)
    except urllib.error.HTTPError as error:
        # urllib raises for error statuses, but the error is itself a readable reply. Its declared
        # types are looser than HttpReply, so the type checker has to be told.
        reply = cast(HttpReply, error)
    return reply


class LrclibClient:
    def __init__(
        self,
        base_url: str = BASE_URL,
        timeout_seconds: float = TIMEOUT_SECONDS,
        open_url: OpenUrl = _open_url,
        clock: Callable[[], float] = time.monotonic,
    ) -> None:
        self._base_url = base_url
        self._timeout_seconds = timeout_seconds
        self._open_url = open_url
        self._clock = clock

    def get_record(self, record_id: int) -> FetchResponse:
        request = urllib.request.Request(
            f"{self._base_url}/api/get/{record_id}", headers={"User-Agent": USER_AGENT}
        )
        # urllib's timeout limits each socket operation, so the body also needs an overall deadline.
        deadline = self._clock() + self._timeout_seconds
        try:
            with self._open_url(request, self._timeout_seconds) as reply:
                body = self._read_body(reply, deadline)
                return FetchResponse(reply.status, body, "Retry-After" in reply.headers)
        except (OSError, http.client.HTTPException):
            return NETWORK_FAILURE

    def _read_body(self, reply: HttpReply, deadline: float) -> bytes:
        chunks: list[bytes] = []
        while True:
            if self._clock() > deadline:
                raise TimeoutError("reading the response took too long")
            chunk = reply.read1(_CHUNK_SIZE)
            if not chunk:
                return b"".join(chunks)
            chunks.append(chunk)
