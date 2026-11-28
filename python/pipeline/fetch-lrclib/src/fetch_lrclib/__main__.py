import io
import sys
from pathlib import Path

from fetch_lrclib.app import Dependencies, run
from fetch_lrclib.lrclib_client import LrclibClient
from fetch_lrclib.lyric_file_store import LyricFileStore
from fetch_lrclib.physical_file_system import PhysicalFileSystem


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if isinstance(stream, io.TextIOWrapper):
            stream.reconfigure(encoding="utf-8", newline="\n")
    dependencies = Dependencies(
        read_text=lambda path: Path(path).read_text(encoding="utf-8-sig"),
        client=LrclibClient(),
        create_store=lambda directory: LyricFileStore(PhysicalFileSystem(), directory),
    )
    return run(sys.argv[1:], dependencies, sys.stdout, sys.stderr)


if __name__ == "__main__":
    sys.exit(main())
