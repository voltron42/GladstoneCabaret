from pathlib import Path

import pytest

from fetch_lrclib.lyric_file_store import LyricFileStore
from fetch_lrclib.physical_file_system import PhysicalFileSystem


def test_store_creates_the_directory_saves_then_keeps_without_leftovers(tmp_path: Path) -> None:
    output = tmp_path / "nested" / "out"
    store = LyricFileStore(PhysicalFileSystem(), str(output))

    assert store.save_if_absent("song.txt", b"first")
    assert not store.save_if_absent("song.txt", b"second")

    assert [path.name for path in output.iterdir()] == ["song.txt"]
    assert (output / "song.txt").read_bytes() == b"first"


def test_move_refuses_to_overwrite_like_the_stand_in(tmp_path: Path) -> None:
    source, destination = tmp_path / "source", tmp_path / "destination"
    source.write_bytes(b"new")
    destination.write_bytes(b"old")

    with pytest.raises(FileExistsError):
        PhysicalFileSystem().move_without_replacing(str(source), str(destination))
    assert destination.read_bytes() == b"old"
