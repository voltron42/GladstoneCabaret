RESERVED_CHARACTERS = '<>:"/\\|?*'


def base_name(artist_name: str, track_name: str) -> str:
    characters = (
        "_" if character < " " or character in RESERVED_CHARACTERS else character
        for character in f"{artist_name} - {track_name}"
    )
    return "".join(characters).rstrip(". ")
