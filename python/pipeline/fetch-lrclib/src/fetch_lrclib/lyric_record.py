from dataclasses import dataclass
from typing import cast

from fetch_lrclib.json_text import is_encodable_text, is_whole_number, parse_json


@dataclass(frozen=True)
class LyricRecord:
    artist_name: str
    track_name: str
    instrumental: bool
    plain_lyrics: str | None
    synced_lyrics: str | None


def parse_lyric_record(body: bytes, expected_id: int) -> LyricRecord | None:
    """Return None when the body is not a usable record for the requested ID."""
    try:
        record = parse_json(body.decode("utf-8"))
    except ValueError:
        return None
    if not isinstance(record, dict):
        return None
    if not is_whole_number(record.get("id")) or record["id"] != expected_id:
        return None
    artist_name = record.get("artistName")
    track_name = record.get("trackName")
    instrumental = record.get("instrumental")
    plain_lyrics = record.get("plainLyrics")
    synced_lyrics = record.get("syncedLyrics")
    if not (_is_required_text(artist_name) and _is_required_text(track_name)):
        return None
    if not isinstance(instrumental, bool):
        return None
    if not (_is_optional_text(plain_lyrics) and _is_optional_text(synced_lyrics)):
        return None
    return LyricRecord(
        cast(str, artist_name),
        cast(str, track_name),
        instrumental,
        cast("str | None", plain_lyrics),
        cast("str | None", synced_lyrics),
    )


def _is_required_text(value: object) -> bool:
    return is_encodable_text(value) and bool(cast(str, value).strip())


def _is_optional_text(value: object) -> bool:
    return value is None or is_encodable_text(value)
