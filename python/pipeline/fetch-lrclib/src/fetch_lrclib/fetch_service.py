from collections.abc import Sequence
from dataclasses import dataclass
from enum import Enum

from fetch_lrclib.file_names import base_name
from fetch_lrclib.lrclib_client import Client, FetchResponse
from fetch_lrclib.lyric_file_store import Store
from fetch_lrclib.lyric_record import LyricRecord, parse_lyric_record
from fetch_lrclib.song_list import Song


class Outcome(Enum):
    GO = "GO"
    NO_GO = "NO-GO"
    ERROR = "ERROR"


@dataclass(frozen=True)
class SongResult:
    song: Song
    outcome: Outcome
    reason: str = ""
    has_line_times: bool = False
    saved_count: int = 0
    kept_count: int = 0


class FetchService:
    def __init__(self, client: Client, store: Store) -> None:
        self._client = client
        self._store = store

    def fetch_all(self, songs: Sequence[Song]) -> list[SongResult]:
        results: list[SongResult] = []
        stopped = False
        for song in songs:
            if stopped:
                results.append(_error(song, "not attempted: service asked us to wait"))
                continue
            response = self._client.get_record(song.lrclib_record_id)
            stopped = _asks_us_to_wait(response)
            results.append(self._evaluate(song, response))
        return results

    def _evaluate(self, song: Song, response: FetchResponse) -> SongResult:
        if response.status != 200:
            return _error(song, _failure_reason(response.status))
        record = parse_lyric_record(response.body, song.lrclib_record_id)
        if record is None:
            return _error(song, "invalid response")
        if record.instrumental:
            return SongResult(song, Outcome.NO_GO, "instrumental")
        if record.plain_lyrics is None or not record.plain_lyrics.strip():
            return SongResult(song, Outcome.NO_GO, "no plain lyrics")
        return self._save(song, record, record.plain_lyrics, response.body)

    def _save(self, song: Song, record: LyricRecord, plain_lyrics: str, body: bytes) -> SongResult:
        name = base_name(record.artist_name, record.track_name)
        files = [(f"{name}.json", body), (f"{name}.txt", plain_lyrics.encode("utf-8"))]
        synced_lyrics = record.synced_lyrics
        has_line_times = synced_lyrics is not None and bool(synced_lyrics.strip())
        if synced_lyrics is not None and has_line_times:
            files.append((f"{name}.lrc", synced_lyrics.encode("utf-8")))
        try:
            saved_count = sum(
                [self._store.save_if_absent(file_name, content) for file_name, content in files]
            )
        except OSError:
            return _error(song, "file write failed")
        return SongResult(song, Outcome.GO, "", has_line_times, saved_count, len(files) - saved_count)


def _asks_us_to_wait(response: FetchResponse) -> bool:
    return response.status == 429 or (response.status not in (None, 200) and response.has_retry_after)


def _failure_reason(status: int | None) -> str:
    match status:
        case None:
            return "network failure"
        case 404:
            return "record not found"
        case 429:
            return "rate limited"
        case _:
            return f"service error: HTTP {status}"


def _error(song: Song, reason: str) -> SongResult:
    return SongResult(song, Outcome.ERROR, reason)
