import os
import socket
import threading
from collections.abc import Iterator

import pytest

from fetch_lrclib.lrclib_client import NETWORK_FAILURE, FetchResponse, LrclibClient
from fetch_lrclib.lyric_record import parse_lyric_record


@pytest.fixture
def listener() -> Iterator[socket.socket]:
    with socket.create_server(("127.0.0.1", 0)) as server:
        yield server


def address(server: socket.socket) -> str:
    return f"http://127.0.0.1:{server.getsockname()[1]}"


def test_reads_status_body_and_retry_after_over_real_http(listener: socket.socket) -> None:
    def answer() -> None:
        connection, _ = listener.accept()
        with connection:
            connection.recv(4096)
            connection.sendall(
                b"HTTP/1.1 503 Busy\r\nRetry-After: 5\r\nContent-Length: 2\r\nConnection: close\r\n\r\nno"
            )

    thread = threading.Thread(target=answer)
    thread.start()
    response = LrclibClient(address(listener), timeout_seconds=5).get_record(1)
    thread.join()

    assert response == FetchResponse(503, b"no", True)


def test_a_server_that_never_answers_times_out(listener: socket.socket) -> None:
    assert LrclibClient(address(listener), timeout_seconds=0.5).get_record(1) == NETWORK_FAILURE


@pytest.mark.skipif(os.environ.get("LRCLIB_LIVE") != "1", reason="opt in with LRCLIB_LIVE=1")
def test_live_record_is_usable() -> None:
    """Checks the real service still matches what our stand-ins assume."""
    response = LrclibClient().get_record(16012238)

    assert response.status == 200
    assert parse_lyric_record(response.body, 16012238) is not None
