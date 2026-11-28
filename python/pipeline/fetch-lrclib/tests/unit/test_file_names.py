import pytest

from fetch_lrclib.file_names import base_name


@pytest.mark.parametrize(
    ("artist", "track", "expected"),
    [
        ("Guns N' Roses", "Sweet Child O' Mine", "Guns N' Roses - Sweet Child O' Mine"),
        ("AC/DC", 'a<b>c:d"e\\f|g?h*i\tj', "AC_DC - a_b_c_d_e_f_g_h_i_j"),
        ("A", "Wait... ", "A - Wait"),
    ],
)
def test_makes_a_safe_name(artist: str, track: str, expected: str) -> None:
    assert base_name(artist, track) == expected
