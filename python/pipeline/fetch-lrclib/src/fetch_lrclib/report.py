from collections.abc import Sequence

from fetch_lrclib.fetch_service import Outcome, SongResult


def format_report(results: Sequence[SongResult]) -> str:
    lines = [_format_line(result) for result in results]
    lines.append(
        f"Summary: go={_count(results, Outcome.GO)}, no-go={_count(results, Outcome.NO_GO)}, "
        f"error={_count(results, Outcome.ERROR)}"
    )
    return "".join(f"{line}\n" for line in lines)


def exit_code(results: Sequence[SongResult]) -> int:
    if _count(results, Outcome.ERROR):
        return 2
    return 1 if _count(results, Outcome.NO_GO) else 0


def _count(results: Sequence[SongResult], outcome: Outcome) -> int:
    return sum(result.outcome is outcome for result in results)


def _format_line(result: SongResult) -> str:
    if result.outcome is Outcome.GO:
        line_times = "yes" if result.has_line_times else "no"
        detail = f"line times: {line_times}; saved: {result.saved_count}, kept: {result.kept_count}"
    else:
        detail = result.reason
    return f"{result.outcome.value:<7}{result.song.artist} - {result.song.song_title} ({detail})"
