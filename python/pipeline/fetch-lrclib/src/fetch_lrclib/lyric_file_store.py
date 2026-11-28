import os
import uuid
from typing import Protocol

from fetch_lrclib.physical_file_system import FileSystem


class Store(Protocol):
    def save_if_absent(self, file_name: str, content: bytes) -> bool:
        """Return True when saved, or False when an existing file was kept."""
        ...


class LyricFileStore:
    def __init__(self, file_system: FileSystem, output_directory: str) -> None:
        self._file_system = file_system
        self._output_directory = output_directory

    def save_if_absent(self, file_name: str, content: bytes) -> bool:
        destination = os.path.join(self._output_directory, file_name)
        if self._file_system.file_exists(destination):
            return False
        self._file_system.create_directory(self._output_directory)
        temporary = os.path.join(self._output_directory, f".{uuid.uuid4().hex}.tmp")
        try:
            self._file_system.write_bytes(temporary, content)
            self._file_system.move_without_replacing(temporary, destination)
            return True
        except OSError:
            if self._file_system.file_exists(destination):
                return False
            raise
        finally:
            if self._file_system.file_exists(temporary):
                self._file_system.delete_file(temporary)
