import pytest

from fetch_lrclib.json_text import is_encodable_text, is_whole_number, parse_json


def test_parse_json_reads_standard_json() -> None:
    assert parse_json('{"a": [1]}') == {"a": [1]}


@pytest.mark.parametrize("text", ["not json", "NaN", "Infinity", "[" * 100_000])
def test_parse_json_rejects_anything_else(text: str) -> None:
    with pytest.raises(ValueError):
        parse_json(text)


@pytest.mark.parametrize(("value", "expected"), [(1, True), (True, False), (1.0, False), ("1", False)])
def test_is_whole_number(value: object, expected: bool) -> None:
    assert is_whole_number(value) is expected


@pytest.mark.parametrize(("value", "expected"), [("a", True), ("\ud800", False), (1, False)])
def test_is_encodable_text(value: object, expected: bool) -> None:
    assert is_encodable_text(value) is expected
