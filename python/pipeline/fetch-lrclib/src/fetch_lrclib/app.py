from collections.abc import Callable, Sequence
from dataclasses import dataclass
from typing import TextIO

from fetch_lrclib.fetch_service import FetchService
from fetch_lrclib.lrclib_client import Client
from fetch_lrclib.lyric_file_store import Store
from fetch_lrclib.report import exit_code, format_report
from fetch_lrclib.song_list import SongListError, parse_song_list

USAGE_EXIT_CODE = 64
USAGE = "usage: fetch-lrclib --songs <song list file> --output <directory>"


@dataclass(frozen=True)
class Dependencies:
    read_text: Callable[[str], str]
    client: Client
    create_store: Callable[[str], Store]


def run(arguments: Sequence[str], dependencies: Dependencies, output: TextIO, error_output: TextIO) -> int:
    options = _read_options(arguments)
    if options is None:
        error_output.write(f"{USAGE}\n")
        return USAGE_EXIT_CODE
    songs_path, output_directory = options
    try:
        songs = parse_song_list(dependencies.read_text(songs_path))
    except (SongListError, OSError, UnicodeDecodeError) as error:
        error_output.write(f"cannot use song list: {error}\n")
        return USAGE_EXIT_CODE
    results = FetchService(dependencies.client, dependencies.create_store(output_directory)).fetch_all(songs)
    output.write(format_report(results))
    return exit_code(results)


def _read_options(arguments: Sequence[str]) -> tuple[str, str] | None:
    # argparse is not used: its exit code, messages and prefix matching differ from the spec.
    if len(arguments) != 4:
        return None
    options = dict(zip(arguments[::2], arguments[1::2], strict=True))
    songs_path = options.get("--songs")
    output_directory = options.get("--output")
    if not songs_path or not output_directory:
        return None
    return songs_path, output_directory
