import pytest

from fetch_lrclib.song_list import Song, SongListError, parse_song_list


def test_reads_songs_and_ignores_other_properties() -> None:
    text = '[{"artist":"A","songTitle":"T","lrclibRecordId":5,"category":"x"}]'

    assert parse_song_list(text) == [Song("A", "T", 5)]


@pytest.mark.parametrize(
    "text",
    [
        "not json",
        "{}",
        "[]",
        "[1]",
        '[{"songTitle":"T","lrclibRecordId":5}]',
        '[{"artist":" ","songTitle":"T","lrclibRecordId":5}]',
        '[{"artist":"A","songTitle":7,"lrclibRecordId":5}]',
        '[{"artist":"A","songTitle":"T"}]',
        '[{"artist":"A","songTitle":"T","lrclibRecordId":0}]',
        '[{"artist":"A","songTitle":"T","lrclibRecordId":"5"}]',
        '[{"artist":"A","songTitle":"T","lrclibRecordId":1.5}]',
        '[{"artist":"A","songTitle":"T","lrclibRecordId":true}]',
    ],
)
def test_rejects_invalid_lists(text: str) -> None:
    with pytest.raises(SongListError):
        parse_song_list(text)
