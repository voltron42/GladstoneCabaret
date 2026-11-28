import os

import pytest
from doubles import FakeFileSystem

from fetch_lrclib.lyric_file_store import LyricFileStore

DESTINATION = os.path.join("out", "song.txt")


def test_saves_and_leaves_no_temporary_file() -> None:
    file_system = FakeFileSystem()

    assert LyricFileStore(file_system, "out").save_if_absent("song.txt", b"ours")
    assert file_system.files == {DESTINATION: b"ours"}


def test_keeps_an_existing_file() -> None:
    file_system = FakeFileSystem({DESTINATION: b"corrected"})

    assert not LyricFileStore(file_system, "out").save_if_absent("song.txt", b"ours")
    assert file_system.files == {DESTINATION: b"corrected"}


def test_keeps_a_file_created_during_the_save() -> None:
    file_system = FakeFileSystem(lose_race=True)

    assert not LyricFileStore(file_system, "out").save_if_absent("song.txt", b"ours")
    assert file_system.files == {DESTINATION: b"theirs"}


def test_removes_a_partial_file_when_writing_fails() -> None:
    file_system = FakeFileSystem(fail_write=True)

    with pytest.raises(OSError):
        LyricFileStore(file_system, "out").save_if_absent("song.txt", b"ours")
    assert file_system.files == {}
