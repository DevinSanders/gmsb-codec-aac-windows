Test fixtures
=============

tone.m4a  — 0.5 s, 440 Hz sine, stereo, 44.1 kHz, AAC-LC in an MP4/M4A container.
tone.aac  — same signal, raw AAC in an ADTS container (the .aac extension).

Both were generated synthetically with ffmpeg (no third-party audio):

    ffmpeg -f lavfi -i "sine=frequency=440:duration=0.5:sample_rate=44100" \
           -ac 2 -c:a aac -b:a 96k tone.m4a

    ffmpeg -f lavfi -i "sine=frequency=440:duration=0.5:sample_rate=44100" \
           -ac 2 -c:a aac -b:a 96k -f adts tone.aac

License: these are original synthetic tones with no copyright restriction —
free to redistribute. Regenerate with the commands above if needed.
