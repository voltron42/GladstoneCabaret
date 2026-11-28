# Karaoke Video Pipeline: Roadmap

**Status:** Moving from ideation to proof of concept (POC). No pipeline code yet. This updates the original outline in [plan.md](plan.md).

## Goal

Turn a source song into a karaoke video with a backing track and accurately timed lyrics.

## Principles

- **Independent of the experience:** the experience plays videos from any source. It does not depend on this pipeline or its working files.
- **Lyrics are burned in:** the finished video carries its own text. No separate subtitle file is delivered.
- **Inputs are kept:** the source video, backing track, vocal stem, approved lyrics and editable subtitle file are saved, so a song can be corrected and rendered again.
- **A person approves each song:** automated checks guide the review but never approve anything.

## Process

| # | Stage | Input | Output | Who |
|---|---|---|---|---|
| 1 | Extract | Source link | Source video and audio | Automatic |
| 2 | Isolate | Source audio | Backing track and vocal stem | Automatic |
| 3 | Fetch lyrics | Artist and title | Lyrics, plus line times if available | Automatic |
| 4 | Transcribe | Vocal stem | Transcript with rough times | Automatic |
| 5 | Compare | Lyrics and transcript | Difference report | Automatic |
| 6 | Resolve | Difference report and audio | Approved lyrics | Person |
| 7 | Align | Approved lyrics, vocal stem, rough times if needed | Timed words | Automatic |
| 8 | Format | Timed words | Draft subtitles | Automatic |
| 9 | Review | Draft subtitles, source audio and backing track | Approved subtitles | Person |
| 10 | Render | Source video, backing track, approved subtitles | Finished video | Automatic |

A song's progress is shown by which of its files exist.

1. **Extract**
   - Save the source video and its audio.
2. **Isolate**
   - The backing track goes into the video.
   - The vocal stem is used for transcription, alignment and review.
3. **Fetch lyrics**
   - Fetched lyrics may be missing, wrong, untimed, or timed to a different edit of the song.
4. **Transcribe**
   - Do not give the model the fetched lyrics, so the transcript is an independent check.
5. **Compare**
   - For comparison only, ignore case, punctuation, line breaks and section labels. Standardize numbers and spelling variants, and expand repeat shorthand such as "x2".
   - Group differences into passages with approximate times.
   - List missing and extra passages before single words.
6. **Resolve**
   - Decide by listening. The transcript may mishear, skip repeats or invent text. Both sources can also agree and be wrong.
7. **Align**
   - Time the approved lyrics, not the transcript's text.
   - If the aligner needs rough times, try the fetched line times where they match the recording, then the transcript's times. Some passages may need times entered by hand.
8. **Format**
   - Create editable, highlightable subtitles.
   - Add line breaks, and a cue before singing resumes after a break.
9. **Review**
   - Play the whole song with subtitles overlaid, without rendering.
   - Fix timing and text, then replay the corrected parts.
   - Check timing against the source audio, and the singer's experience against the backing track.
   - Check for wrong or missing words, bad line breaks, missing cues and audible damage from vocal removal.
   - Approve the song. A [flagging report](#flagging-report) could help guide the review.
10. **Render**
    - Render approved songs only, replacing the audio with the backing track and burning in the subtitles.
    - Proposed delivery: `Artist - Title.mp4`, H.264 video and AAC audio.

### Flagging report

A potential enhancement, not yet discussed: a list of suspect spots, worst first, each with a time and reason. It would not replace the full playthrough.

| Check | Possible problem |
|---|---|
| A word has a low score or no time | Failed alignment, or text that does not match the audio |
| A word is implausibly short or long | Bunched or stretched words (held notes may be false alarms) |
| Words fall where the vocal stem is nearly silent | Misplaced highlight |
| The stem has sustained sound but no words | Missing or shifted lyrics (backing vocals or leftover instruments may be false alarms) |
| Line starts differ widely from fetched times | Bad alignment, or times for a different edit |

A similar difference on every line would be reported once, as a constant offset.

## Proof of concept

The POC is for finding out which tools can do each step.

1. **Test by step:** try each step's tools separately, using the [test songs](shoppingList.md). Run them by hand, with throwaway scripts where needed. Where tools are being compared, give them the same inputs.
2. **Document:** record what was run (versions, models and settings), problems installing or running it, processing time, output quality, manual effort, and how our code could call it.
3. **Compare:** set the results for each step side by side.
4. **Discuss:** weigh pros and cons, including how the preferred tools would work together and what language our own code would use.

## Tool options

Taken from project pages and documentation on 2026-11-28. **Nothing has been installed or tested.** Speech and alignment results on singing are unknown.

- **Forms:** a *CLI* is a command-line program that code in any language can run. A *library* is called from code in a supported language. A *web API* is called over the network. A *desktop app* is used by hand.
- **Language:** for a CLI or web API, the tool's own language does not dictate ours. It still matters if we have to debug or change the tool.
- Language lists show the ones relevant here (Python, C#) and are not complete.

### Extract

| Tool | Written in | Forms | Notes |
|---|---|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Python | CLI | Library use not checked |

### Isolate

| Tool | Written in | Forms | Model families | Notes |
|---|---|---|---|---|
| [Demucs](https://github.com/adefossez/demucs) | Python | CLI, Python library | Demucs | Use this fork; the [original](https://github.com/facebookresearch/demucs) is archived. The fork's author says they are not actively working on it |
| [audio-separator](https://github.com/nomadkaraoke/python-audio-separator) | Python | CLI, Python library | MDX-Net, VR, Demucs, RoFormer | Can run Demucs models |
| [sherpa-onnx](https://k2-fsa.github.io/sherpa/onnx/source-separation/index.html) | C++ (inferred) | CLI, libraries including Python and C# | Spleeter (2-stem), UVR MDX-Net | [C# examples](https://github.com/k2-fsa/sherpa-onnx/tree/master/dotnet-examples) include vocal removal |
| [Ultimate Vocal Remover](https://github.com/Anjok07/ultimatevocalremovergui) | Python | Desktop app | MDX-Net, VR, MDX23C, Demucs | No documented CLI or library found on its main page |

### Fetch lyrics

| Source | Forms | Notes |
|---|---|---|
| [LRCLIB](https://lrclib.net/docs) | Web API | Plain and line-timed lyrics; some records have word times |
| [Genius](https://docs.genius.com) | Web API | Documentation could not be fetched. Earlier notes say no synced lyrics, and no lyric text from the official API; unverified |

### Transcribe and align

"Align" means timing text that we supply.

| Tool | Written in | Forms | Transcribe | Align |
|---|---|---|---|---|
| [openai-whisper](https://github.com/openai/whisper) | Python | CLI, Python library | Yes | Not documented on its main page |
| [WhisperX](https://github.com/m-bain/whisperX) | Python | CLI, Python library | Yes, with word times | Python alignment step takes timed text segments. Supplying our own lyrics there, directly or via CLI, needs testing |
| [whisper.cpp](https://github.com/ggml-org/whisper.cpp) | C/C++ | CLI, C library, local web server | Yes; word times experimental | Not documented on its main page |
| [Whisper.net](https://github.com/sandrohanea/whisper.net) | C# wrapper for whisper.cpp | C# library | Yes, with segment times | Not documented on its main page |
| [CrispASR](https://github.com/CrispStrobe/CrispASR) | C++ | CLI, libraries including Python and [C#](https://github.com/CrispStrobe/CrispASR/blob/main/docs/bindings.md), local web server | Yes | Yes: [CLI](https://github.com/CrispStrobe/CrispASR/blob/main/docs/cli.md) accepts audio and plain text. Need for rough times not confirmed |
| [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) | C++ (inferred) | CLI, libraries including Python and C# | Yes | Documented for the libraries, with C# examples. CLI alignment not confirmed |

### Format

No ready-made tool has been chosen. We expect to write this conversion. ASS is the proposed subtitle format because it supports karaoke highlighting.

### Review

| Tool | Forms | Notes |
|---|---|---|
| [Aegisub](https://github.com/TypesettingTools/Aegisub) | Desktop app with Lua scripting | Subtitle timing editor. Switching between our two audio tracks needs testing |

### Render

| Tool | Written in | Forms |
|---|---|---|
| [FFmpeg](https://ffmpeg.org) | C | CLI, C libraries |

## Constraints

- **Test machine (checked 2026-11-27):** Intel N100 (4 cores), 15.8 GB memory, integrated graphics only, 113.1 GB free disk. Plan on running AI tools on the processor.
- **Python:** installing or upgrading it is not a concern. The concern is how readable, maintainable and testable our code is.

## Current questions

For the POC to answer.

- **Tools:** which are good enough at each step?
- **Speed:** is the test machine fast enough?
- **Lyrics:** do the fetched sources cover our songs, and how accurate are they?
- **Transcription:** is it accurate enough to help find lyric errors?
- **Alignment:** how well does supplied text align to singing? Does it need rough times, and where would they come from?
- **Highlighting:** word by word, or line by line? The answer affects what alignment must do.
- **Review:** how long does correcting a song take, is that acceptable, and does the editor meet our needs?
- **Integration:** how would the preferred tools connect, and what language would our code use?

## Future questions

Recorded so they are not forgotten. These are not tasks yet, and some may become irrelevant once the current questions are answered.

- **Enhancements:** is automating the lyric comparison or building a flagging report worthwhile?
- **File state:** how are approved files told apart from drafts, and out-of-date files recognized?
- **Corrections:** how are earlier timing fixes kept when lyrics change?
- **Use:** how does the user run stages and resolve lyric differences?
- **Storage:** how are working files stored, and how much space do they need?
- **Audience:** home or public use? What rights and site terms apply to downloading and playing the sources?
- **Display:** what should subtitles and cues look like? How are backing vocals, duets, ad-libs and existing on-screen text handled?
- **Delivery:** is the proposed format and naming right? The experience should list other files by filename.
- **Playback:** which Raspberry Pi model plays the videos, at what resolution, and how do videos get to it?
