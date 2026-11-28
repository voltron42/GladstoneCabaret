"""Strict JSON reading shared by the song list and lyric record."""

import json
from typing import Never


def _reject_constant(name: str) -> Never:
    raise ValueError(f"{name} is not valid JSON")


def parse_json(text: str) -> object:
    """Raise ValueError for anything that is not standard JSON."""
    try:
        value: object = json.loads(text, parse_constant=_reject_constant)
    except RecursionError as error:
        raise ValueError("JSON is nested too deeply") from error
    return value


def is_whole_number(value: object) -> bool:
    # bool is a subclass of int, so it has to be excluded explicitly.
    return isinstance(value, int) and not isinstance(value, bool)


def is_encodable_text(value: object) -> bool:
    """True for a string that can be written as UTF-8, which excludes lone surrogates."""
    if not isinstance(value, str):
        return False
    try:
        value.encode("utf-8")
    except UnicodeEncodeError:
        return False
    return True
