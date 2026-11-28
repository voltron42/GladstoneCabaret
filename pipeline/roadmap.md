# Karaoke Video Pipeline: Plan and Roadmap

**Status:** Draft for discussion. Development has not started.

This expands on the original outline in [plan.md](plan.md). Where the two differ, this document reflects the later thinking. Tool behaviour described here has not yet been tested in this project.

## Goal

Turn a source song into a karaoke video with a backing track and accurately timed lyrics.

## Principles

- **Independent of the experience:** the experience plays videos from any source. It does not depend on this pipeline or its working files.
- **Lyrics are burned in:** the finished video carries its own text. No separate subtitle file is delivered.
- **Inputs are kept:** the source video, backing track, vocal stem, approved lyrics and editable subtitle file are saved, so a song can be corrected and rendered again.
- **A person approves each song:** automated checks guide the review but never approve anything.

## Stages

| # | Stage | Input | Output | Who |
|---|---|---|---|---|
| 1 | Extract | Source link | Source video and audio | Automatic |
| 2 | Isolate | Source audio | Backing track and vocal stem | Automatic |
| 3 | Fetch lyrics | Artist and title | Lyrics file, plus line times if available | Automatic |
| 4 | Transcribe | Vocal stem | Transcript with rough times | Automatic |
| 5 | Compare | Lyrics file and transcript | Difference report | Automatic |
| 6 | Resolve | Difference report and audio | Approved lyrics | Person |
| 7 | Align | Approved lyrics, vocal stem, rough times | Timed words | Automatic |
| 8 | Format | Timed words | Draft subtitle file | Automatic |
| 9 | Review | Draft subtitles, both audio tracks | Approved subtitles | Person |
| 10 | Render | Source video, backing track, approved subtitles | Finished video | Automatic |

### 1–3. Gather materials

- **Extract:** `yt-dlp` is the proposed tool.
- **Isolate:** keep both outputs. The backing track goes into the video. The vocal stem is used for transcription, alignment and review. Ultimate Vocal Remover is mainly a desktop app, so `audio-separator` and Demucs are candidates for automation.
- **Fetch lyrics:** [LRCLIB](https://lrclib.net) is the candidate source. It has line times for many songs, but not all, and they may belong to a different edit of the song. Genius does not provide synced lyrics, and its official API does not return lyric text.

### 4–6. Validate the lyrics

This catches wrong lyrics before they turn into timing problems.

- **Transcribe independently:** do not give Whisper the fetched lyrics as a hint.
- **Clean up for comparison only:** ignore case, punctuation and line breaks. Remove section labels such as "[Chorus]". Standardize numbers and common spelling variants. Write out repeat shorthand such as "x2". The displayed lyrics keep their formatting.
- **Report differences:** group them into passages, each with an approximate timestamp. List missing and extra passages before single words. Passages missing from the transcript may only have an estimated position.
- **Resolve by listening:** neither source is automatically right. Whisper may mishear words, skip repeats or invent text during instrumentals.
- **Agreement is not proof:** both sources can be wrong in the same place, so the final review checks the words again.

### 7–8. Align and format

- **Align:** call the WhisperX alignment step with the approved lyrics, rather than aligning its own transcript. It needs the text in segments with rough start and end times.
- **Rough times:** use LRCLIB line times where they match the recording. Otherwise, try matching the approved lyrics to the transcript's segment times. Poorly transcribed passages may need times entered by hand.
- **Format:** convert timed words into a subtitle file, including line breaks and a cue before singing resumes after a break. ASS is the proposed format because it supports karaoke highlighting. We expect to need our own conversion step.

### 9. Review the timing

1. Play the whole song with the subtitles overlaid, without rendering.
2. Fix timing and text. [Aegisub](https://aegisub.org) is the proposed editor; try it before building anything custom.
3. Replay the corrected parts.
4. Check against the original vocals for timing, and against the backing track for the singer's experience.
5. Check for wrong or missing words, bad line breaks, missing cues and audible damage from vocal removal.
6. Approve the song.

If alignment fails badly, check the lyrics, vocal stem and rough times to find the cause.

#### Flagging report

A list of suspect spots, worst first, each with a timestamp and a reason.

| Check | Possible problem |
|---|---|
| A word has a low score or no timestamp | Alignment failed, or the text does not match the audio |
| A word is implausibly short or long | Words are bunched up or stretched. Held notes may cause false alarms |
| Words fall where the vocal stem is nearly silent | The highlight is in the wrong place |
| The stem has sustained sound but no words | Lyrics are missing or shifted. Backing vocals and leftover instruments may cause false alarms |
| Line starts differ widely from LRCLIB's | The alignment is wrong, or LRCLIB timed a different edit |

If every line differs from LRCLIB by about the same amount, report one **constant offset**. Check this against the audio, especially if LRCLIB times were used as alignment input.

These checks will not find every error. The full playthrough remains required.

### 10. Render

- Only approved songs are rendered.
- The video's audio is replaced with the backing track, and the subtitles are burned in. `FFmpeg` is the proposed tool.
- **Delivered file (proposed):** MP4 with H.264 video and AAC audio, named `Artist - Title.mp4`. Resolution depends on playback testing on the Pi.

## Song status

Each song needs a recorded status:

**lyrics pending → lyrics approved → timing draft → timing approved → rendered**

- Changing approved lyrics returns the song to **lyrics approved**. Alignment, formatting and timing review must be repeated. Decide how to preserve earlier manual timing fixes before building this.
- Editing approved subtitles returns the song to **timing draft**. A previously rendered video is then out of date.

## Hardware

The hand test will run on the current development machine (checked 2026-11-27):

- **Processor:** Intel N100, 4 cores
- **Memory:** 15.8 GB
- **Graphics:** integrated Intel UHD, no dedicated card. Plan on running the AI tools on the processor.
- **Free disk space:** 113.1 GB
- **Tools:** `ffmpeg` and `yt-dlp` are on PATH. Aegisub is not on PATH, so check whether it is installed. Python command aliases exist, but a working installation has not been verified.

## Roadmap

### Phase 0: Hand test

Run three to five varied songs through the process before building it. Use existing tools and throwaway scripts, including a rough converter that produces files Aegisub can open.

For each song, record:

- Song length, and processing time for isolation, transcription, alignment and a test render
- Which models and settings were used
- Whether alignment with supplied lyrics works well enough on singing
- How many lyric differences were real errors in the lyrics file, and how many were Whisper's mistakes
- Real lyric errors that the comparison missed
- Whether rough times could be obtained without LRCLIB (try this even on songs that have it)
- Time spent resolving lyrics and correcting timing
- The automatic draft and the corrected version, saved separately
- Errors each proposed check would have caught, and its false alarms

**Exit decisions:** choose word or line highlighting, decide whether the comparison step saves enough effort, and decide whether to continue with this approach. The sample is small, so treat the results as a first estimate.

### Phase 1: Core pipeline

Automate stages 1–4 and 7–8, add render and status tracking, and keep review in Aegisub. Resolve lyrics by hand until the comparison report exists.

### Phase 2: Lyric validation

Build the cleanup and difference report (stage 5), if Phase 0 shows it is worthwhile.

### Phase 3: Flagging report

Build only the checks that proved useful. Set thresholds from saved drafts and corrections, and keep measuring as more songs are processed.

## Unresolved issues

### Product

- [ ] Word-by-word or line-by-line highlighting? Line-by-line may allow a simpler process when LRCLIB times match.
- [ ] Is this for home or public use?
- [ ] What are the rights and terms implications of the sources? Downloading with `yt-dlp` can breach a site's terms, and public playback raises separate licensing questions.
- [ ] What happens when the source video already has text on screen?
- [ ] How should backing vocals, duets and ad-libs be displayed?
- [ ] What should the subtitles look like, and how should cues work?
- [ ] Confirm the delivered format and the `Artist - Title.mp4` naming convention. The experience should list files with other names under their filename.

### Technical

- [ ] Is the current machine (no dedicated graphics card) fast enough for regular use, or only for the hand test?
- [ ] Which Raspberry Pi model will play the videos, and what resolution can it play smoothly?
- [ ] Which vocal isolation tool and model give good enough results?
- [ ] Is LRCLIB's coverage good enough for our songs? What is the fallback when it has no lyrics?
- [ ] Where do rough times come from when neither LRCLIB nor the transcript provides them?
- [ ] Does Aegisub meet the review needs, including switching between audio tracks?
- [ ] How are manual timing fixes preserved when lyrics change?
- [ ] What language will the pipeline be written in, and how will it call the Python-based tools?
- [ ] How will the user run each stage and resolve lyric differences?
- [ ] How are working files and song status stored, and how much disk space is needed?
- [ ] How do finished videos get to the Pi?

### To be answered by Phase 0

- [ ] Is alignment with supplied lyrics accurate enough on singing?
- [ ] How long does each song take to correct, and what is the acceptable limit?
- [ ] Does the lyric comparison save more effort than it costs?
- [ ] Which flagging checks are worth building?
