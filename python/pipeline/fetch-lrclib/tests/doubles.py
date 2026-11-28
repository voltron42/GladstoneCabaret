"""Stand-ins for the web client, file store and file system."""

import json

from fetch_lrclib.lrclib_client import FetchResponse


def usable_response(record_id: int = 1, /, **changes: object) -> FetchResponse:
    record: dict[str, object] = {
        "id": record_id,
        "artistName": "Artist",
        "trackName": "Track",
        "instrumental": False,
        "plainLyrics": "made up line",
        "syncedLyrics": None,
    }
    record.update(changes)
    return FetchResponse(200, json.dumps(record).encode("utf-8"))


class FakeClient:
    def __init__(self, *responses: FetchResponse) -> None:
        self._responses = list(responses)
        self.requested_ids: list[int] = []

    def get_record(self, record_id: int) -> FetchResponse:
        self.requested_ids.append(record_id)
        return self._responses.pop(0)


class FakeStore:
    def __init__(self, files: dict[str, bytes] | None = None, failure: OSError | None = None) -> None:
        self.files = dict(files or {})
        self._failure = failure

    def save_if_absent(self, file_name: str, content: bytes) -> bool:
        if self._failure:
            raise self._failure
        if file_name in self.files:
            return False
        self.files[file_name] = content
        return True


class FakeFileSystem:
    def __init__(
        self, files: dict[str, bytes] | None = None, fail_write: bool = False, lose_race: bool = False
    ) -> None:
        self.files = dict(files or {})
        self._fail_write = fail_write
        # Simulates another process creating the destination just before the move.
        self._lose_race = lose_race

    def file_exists(self, path: str) -> bool:
        return path in self.files

    def create_directory(self, path: str) -> None:
        pass

    def write_bytes(self, path: str, content: bytes) -> None:
        self.files[path] = content
        if self._fail_write:
            raise OSError("disk full")

    def move_without_replacing(self, source: str, destination: str) -> None:
        if self._lose_race:
            self.files[destination] = b"theirs"
        if destination in self.files:
            raise FileExistsError(destination)
        self.files[destination] = self.files.pop(source)

    def delete_file(self, path: str) -> None:
        del self.files[path]
