# Custom Karaoke Video Pipeline

1. Extract Media: Use `yt-dlp` to download the source video and audio.
2. Isolate Vocals: Use AI tools like Ultimate Vocal Remover to separate the backing track.
3. Fetch Lyrics: Retrieve synchronized lyrics using APIs like Genius.
4. Auto-Sync: Generate time-stamped subtitles using forced alignment tools like WhisperX.
5. Mux Streams: Combine the original video, isolated audio, and timed subtitles into a single MP4 file using `FFmpeg`.
