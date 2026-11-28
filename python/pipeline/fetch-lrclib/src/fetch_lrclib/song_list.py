from dataclasses import dataclass
from typing import cast

from fetch_lrclib.json_text import is_encodable_text, is_whole_number, parse_json


@dataclass(frozen=True)
class Song:
    artist: str
    song_title: str
    lrclib_record_id: int


class SongListError(Exception):
    pass


def parse_song_list(text: str) -> list[Song]:
    try:
        entries = parse_json(text)
    except ValueError as error:
        raise SongListError("song list is not valid JSON text") from error
    if not isinstance(entries, list) or not entries:
        raise SongListError("song list must be a non-empty JSON array")
    return [_parse_song(entry, position) for position, entry in enumerate(entries, start=1)]


def _parse_song(entry: object, position: int) -> Song:
    if not isinstance(entry, dict):
        raise SongListError(f"song {position} must be an object")
    return Song(
        artist=_require_text(entry, "artist", position),
        song_title=_require_text(entry, "songTitle", position),
        lrclib_record_id=_require_record_id(entry, position),
    )


def _require_text(entry: dict[str, object], name: str, position: int) -> str:
    value = entry.get(name)
    if is_encodable_text(value) and cast(str, value).strip():
        return cast(str, value)
    raise SongListError(f"song {position}: {name} must be a non-blank string")


def _require_record_id(entry: dict[str, object], position: int) -> int:
    value = entry.get("lrclibRecordId")
    if is_whole_number(value) and cast(int, value) > 0:
        return cast(int, value)
    raise SongListError(f"song {position}: lrclibRecordId must be a positive integer")
