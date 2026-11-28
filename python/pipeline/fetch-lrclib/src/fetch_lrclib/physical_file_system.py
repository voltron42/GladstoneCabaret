"""A thin adapter over the real file system, covered by external integration tests."""

import os
from pathlib import Path
from typing import Protocol


class FileSystem(Protocol):
    def file_exists(self, path: str) -> bool: ...

    def create_directory(self, path: str) -> None: ...

    def write_bytes(self, path: str, content: bytes) -> None: ...

    def move_without_replacing(self, source: str, destination: str) -> None: ...

    def delete_file(self, path: str) -> None: ...


class PhysicalFileSystem:
    def file_exists(self, path: str) -> bool:
        return os.path.isfile(path)

    def create_directory(self, path: str) -> None:
        os.makedirs(path, exist_ok=True)

    def write_bytes(self, path: str, content: bytes) -> None:
        Path(path).write_bytes(content)

    def move_without_replacing(self, source: str, destination: str) -> None:
        # A hard link fails if the destination exists on every platform; a rename does not.
        os.link(source, destination)
        os.remove(source)

    def delete_file(self, path: str) -> None:
        os.remove(path)
