# fetch-lrclib: behaviour spec

All implementations of `fetch-lrclib` follow this spec, so that differences between them come from the language rather than the design.

## Command

Both options are required.

```
fetch-lrclib --songs <song list file> --output <directory>
```

## Song list

A UTF-8 JSON array with at least one entry. Each entry needs:

- `artist`: non-blank string
- `songTitle`: non-blank string
- `lrclibRecordId`: positive integer

Other properties are ignored. The whole list is validated before any request is made.

## Requests

- `GET https://lrclib.net/api/get/{lrclibRecordId}`, one at a time, in list order.
- `User-Agent: fetch-lrclib-poc`. It carries no personal or project details.
- Each request, including reading the body, times out after 30 seconds.
- If a response has status 429, or is not 200 and has a `Retry-After` header, no further requests are made. The tool does not retry.

## Record

A usable response is a JSON object where:

- `id` equals the requested ID
- `artistName` and `trackName` are non-blank strings
- `instrumental` is a boolean
- `plainLyrics` and `syncedLyrics` are each a string, null or absent

## Outcomes

| Outcome | When | Reason text |
|---|---|---|
| go | Not instrumental, and `plainLyrics` is not blank | |
| no-go | Instrumental | `instrumental` |
| no-go | `plainLyrics` is blank, null or absent | `no plain lyrics` |
| error | Status 404. A missing pinned record does not show that the song has no lyrics | `record not found` |
| error | Status 429 | `rate limited` |
| error | Any other status except 200 | `service error: HTTP <status>` |
| error | Connection failure or timeout | `network failure` |
| error | The response is not a usable record | `invalid response` |
| error | A song after requests stopped | `not attempted: service asked us to wait` |
| error | A file could not be written | `file write failed` |

An error on one song does not stop the others, except as described under Requests.

## Files

Written only for a go.

- Name: `<artistName> - <trackName>` from the record. Each of `< > : " / \ | ? *` and each character below U+0020 becomes `_`. Trailing dots and spaces are then removed.
- `.json`: the response body, byte for byte.
- `.txt`: `plainLyrics` as UTF-8 without a byte order mark, with nothing added or changed.
- `.lrc`: `syncedLyrics`, written the same way, only when it is not blank.
- The output directory is created if missing.
- An existing file is never overwritten. It counts as **kept**.
- A file appears complete or not at all: write a temporary file in the output directory, then move or link it into place without replacing anything. Temporary files are removed.

## Report

Written to standard output, one line per song in list order, then a summary. `<song>` is `<artist> - <songTitle>` from the song list. Outcome labels are padded to seven characters.

```
GO     <song> (line times: yes; saved: 3, kept: 0)
NO-GO  <song> (no plain lyrics)
ERROR  <song> (network failure)
Summary: go=1, no-go=1, error=1
```

`line times` is `yes` when the record's `syncedLyrics` is not blank, otherwise `no`.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Every song is a go |
| 1 | At least one no-go, and no errors |
| 2 | At least one error |
| 64 | Wrong arguments, or the song list is missing or invalid. A message goes to standard error |
