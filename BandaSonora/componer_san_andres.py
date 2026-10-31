"""Original Caribbean game cue. Numpy synthesis; no voices or external recordings."""
from pathlib import Path
from functools import lru_cache
import json
import wave
import numpy as np

OUT = Path(__file__).resolve().parent
SR, BPM, BARS = 44100, 104, 64
BEAT = 60 / BPM
N = round(BARS * 4 * BEAT * SR)
DURATION = N / SR
rng = np.random.default_rng(25102026)
music = np.zeros((N, 2), np.float32)
drums = np.zeros_like(music)
sea = np.zeros_like(music)


def time_axis(seconds):
    return np.arange(round(seconds * SR)) / SR


def hz(note):
    return 440 * 2 ** ((note - 69) / 12)


def add(bus, signal, seconds, gain=1, pan=0):
    """Circular placement preserves instruments and ambience across the loop boundary."""
    start = round(seconds * SR) % N
    signal = np.asarray(signal, dtype=np.float32)
    gains = np.array([np.cos((pan + 1) * np.pi / 4),
                      np.sin((pan + 1) * np.pi / 4)], np.float32)
    stereo = signal[:, None] * gains * gain
    first = min(len(signal), N - start)
    bus[start:start + first] += stereo[:first]
    if first < len(signal):
        bus[:len(signal) - first] += stereo[first:]


def noise(seconds, low, high):
    n = round(seconds * SR)
    white = rng.normal(size=n)
    f = np.fft.rfftfreq(n, 1 / SR)
    filt = (1 - np.exp(-(f / max(low, 1)) ** 4)) * np.exp(-(f / high) ** 4)
    result = np.fft.irfft(np.fft.rfft(white) * filt, n=n)
    return (result / (np.std(result) + 1e-10)).astype(np.float32)


@lru_cache(maxsize=256)
def string(note, kind, muted=False):
    # Damped string partials, pick-position notches, body resonances and pick noise.
    lengths = {'guitar': 2.7, 'ukulele': 1.3, 'mandolin': 1.2}
    length = .28 if muted else lengths[kind]
    t = time_axis(length)
    f = hz(note)
    decay = {'guitar': 1.0, 'ukulele': .43, 'mandolin': .48}[kind]
    if muted:
        decay *= .19
    signal = np.zeros(len(t))
    for harmonic in range(1, 22):
        freq = f * harmonic * np.sqrt(1 + .000012 * harmonic ** 2)
        if freq > 14000:
            break
        pick = np.sin(np.pi * harmonic * .22)
        body = 1 + .65 * np.exp(-((freq - 210) / 100) ** 2)
        body += .35 * np.exp(-((freq - 680) / 220) ** 2)
        amp = pick * body / harmonic ** 1.25
        damp = np.exp(-t * (1 + harmonic * .21) / decay)
        signal += amp * np.cos(2 * np.pi * freq * t) * damp
        if kind == 'mandolin':
            signal += .72 * amp * np.cos(2 * np.pi * freq * 1.0024 * t + .12) * damp
    attack = (1 - np.exp(-t / .0025))
    release = np.minimum(1, (length - t) / .06)
    signal *= attack * release
    signal += noise(length, 1200, 7000) * np.exp(-t / .008) * .035 * attack
    return (signal / max(np.max(np.abs(signal)), 1e-6)).astype(np.float32)


@lru_cache(maxsize=128)
def steel(note, beats):
    length = beats * BEAT + 1.1
    t = time_axis(length)
    f = hz(note)
    # Tuned pan modes, short metallic impact and a warm fundamental.
    signal = np.zeros(len(t))
    for ratio, amp, decay in [(1, 1, .62), (2, .48, .34), (3, .07, .20),
                               (4.02, .15, .14), (5.04, .045, .085)]:
        phase = 2 * np.pi * f * ratio * t + .65 * np.sin(2 * np.pi * f * t) * np.exp(-t / .025)
        signal += amp * np.sin(phase) * np.exp(-t / decay)
    signal *= (1 - np.exp(-t / .003)) * np.minimum(1, (length - t) / .08)
    # Gate short notes gently, keeping a natural ringing tail.
    signal *= np.exp(-np.maximum(0, t - beats * BEAT) / .20)
    return (signal / max(np.max(np.abs(signal)), 1e-6)).astype(np.float32)


@lru_cache(maxsize=128)
def bass(note, beats):
    length = beats * BEAT + .09
    t = time_axis(length)
    phase = 2 * np.pi * hz(note) * t
    signal = np.sin(phase) + .22 * np.sin(2 * phase) + .07 * np.sin(3 * phase)
    env = (1 - np.exp(-t / .012)) * np.exp(-t / 1.2)
    env *= np.minimum(1, (length - t) / .10)
    return (signal * env).astype(np.float32)


def percussion(kind):
    length = {'kick': .4, 'rim': .13, 'hat': .13, 'shaker': .09,
              'conga': .34, 'conga_hi': .22, 'clave': .12}[kind]
    t = time_axis(length)
    if kind == 'kick':
        phase = 2 * np.pi * (48 * t + 42 * .035 * (1 - np.exp(-t / .035)))
        signal = np.sin(phase) * np.exp(-t / .095)
        signal += .10 * noise(length, 140, 2200) * np.exp(-t / .009)
    elif kind == 'rim':
        signal = (np.sin(2 * np.pi * 860 * t) + .5 * np.sin(2 * np.pi * 1730 * t)) * np.exp(-t / .014)
        signal += .65 * noise(length, 1100, 7000) * np.exp(-t / .022)
    elif kind in ('hat', 'shaker'):
        signal = noise(length, 4600 if kind == 'hat' else 2700, 12500)
        signal *= np.exp(-t / (.022 if kind == 'hat' else .018))
    elif kind == 'clave':
        signal = (np.sin(2 * np.pi * 2350 * t) + .35 * np.sin(2 * np.pi * 3525 * t)) * np.exp(-t / .017)
    else:
        f = 180 if kind == 'conga' else 285
        phase = 2 * np.pi * f * t - 1.5 * np.exp(-t / .025)
        signal = (np.sin(phase) + .32 * np.sin(1.59 * phase)) * np.exp(-t / .075)
        signal += .14 * noise(length, 800, 5000) * np.exp(-t / .009)
    signal *= (1 - np.exp(-t / .0008)) * np.minimum(1, (length - t) / .015)
    return (signal / max(np.max(np.abs(signal)), 1e-6)).astype(np.float32)


kit = {kind: percussion(kind) for kind in ['kick', 'rim', 'hat', 'shaker', 'conga', 'conga_hi', 'clave']}
# G major; open acoustic voicings and close upper-register ukulele voicings.
chords = {
    'G': ([43, 50, 55, 59, 62, 67], [67, 71, 74, 79], 31),
    'D': ([50, 57, 62, 66, 69], [66, 69, 74, 78], 38),
    'Em': ([40, 47, 52, 55, 59, 64], [67, 71, 76, 79], 40),
    'C': ([48, 55, 60, 64, 67], [67, 72, 76, 79], 36),
    'Am': ([45, 52, 57, 60, 64], [69, 72, 76, 81], 33),
}
progression = ['G', 'D', 'Em', 'C', 'G', 'Am', 'C', 'D']
# Each tuple is onset in beats, MIDI pitch and sounding duration in beats.
theme_a = [
    [(0, 74, .7), (1, 71, .4), (1.5, 74, .4), (2.5, 79, .8)],
    [(.5, 78, .7), (1.5, 76, .4), (2, 74, 1.2)],
    [(0, 71, .7), (1, 74, .4), (1.5, 76, .7), (3, 79, .6)],
    [(.5, 76, .7), (1.5, 74, .4), (2.5, 72, .9)],
    [(0, 71, .4), (.5, 74, .4), (1.5, 79, .7), (2.5, 81, .4), (3, 79, .6)],
    [(.5, 76, .7), (1.5, 72, .4), (2.5, 69, .9)],
    [(0, 72, .7), (1, 76, .4), (1.5, 79, .7), (3, 76, .6)],
    [(.5, 74, .7), (1.5, 69, .4), (2.5, 78, .65)],
]
theme_b = [
    [(0, 79, .65), (1, 81, .4), (1.5, 83, .8), (3, 81, .4)],
    [(.5, 81, .6), (1.5, 78, .5), (2.5, 74, .9)],
    [(0, 79, .7), (1, 76, .4), (2, 83, .75), (3, 81, .4)],
    [(.5, 79, .7), (1.5, 76, .4), (2.5, 72, .9)],
    [(0, 74, .4), (.5, 79, .7), (1.5, 83, .6), (2.5, 81, .4), (3, 79, .4)],
    [(.5, 81, .7), (1.5, 76, .4), (2.5, 72, .9)],
    [(0, 79, .7), (1, 76, .4), (1.5, 74, .4), (2.5, 72, .8)],
    [(.5, 74, .7), (1.5, 78, .4), (2.5, 81, .65), (3.5, 78, .35)],
]
sections = ['A', 'B', 'A-light', 'B', 'A', 'C', 'A', 'B-return']
print('Rendering 64 bars: strings, steel pans, bass and Caribbean percussion...', flush=True)
for bar in range(BARS):
    local = bar % 8
    section = sections[bar // 8]
    light = section in ('A-light', 'C')
    start = bar * 4 * BEAT
    guitar, uke, root = chords[progression[local]]
    # Acoustic finger-picked downbeats, with short reggae upstroke answers.
    for i, note in enumerate(guitar):
        add(music, string(note, 'guitar'), start + i * .014, .043, -.34)
    for b in [1.5, 3.5]:
        for i, note in enumerate(reversed(guitar[2:])):
            add(music, string(note, 'guitar', True), start + b * BEAT + i * .009, .030, -.34)
    for b in ([.5, 2.5] if light else [.5, 1.5, 2.5, 3.5]):
        for i, note in enumerate(uke):
            add(music, string(note, 'ukulele', True), start + b * BEAT + i * .008, .035, .40)
    for b, note, dur in [(0, root, .85), (1.5, root, .4), (2.5, root + 7, .4), (3.25, root + 12, .55)]:
        add(music, bass(note, dur), start + b * BEAT, .17, 0)
    motif = theme_b if section.startswith('B') else theme_a
    for b, note, dur in motif[local]:
        if section == 'C':
            # Mandolin carries the tune in the quieter sea-adventure bridge.
            for k in range(max(1, int(dur * 4))):
                add(music, string(note - 12, 'mandolin'), start + (b + k * .25) * BEAT,
                    .050 * (.86 if k % 2 else 1), .14)
        else:
            add(music, steel(note, dur), start + b * BEAT, .17 if light else .205, -.06)
    # Short mandolin countermelodies occupy spaces between steel-pan phrases.
    if section != 'C' and local % 2 == 1:
        for k, b in enumerate([3.25, 3.5, 3.75]):
            add(music, string(uke[k] - 12, 'mandolin'), start + b * BEAT, .042, .28)
    kick_beats = [0, 2] if light else [0, 1.75, 2, 3.5]
    for b in kick_beats:
        add(drums, kit['kick'], start + b * BEAT, .19 if b in (0, 2) else .11, 0)
    for b in [1, 3]:
        add(drums, kit['rim'], start + b * BEAT + .009, .115, -.10)
    for k in range(8):
        jitter = rng.uniform(-.006, .006)
        add(drums, kit['shaker'], start + k * .5 * BEAT + jitter,
            (.040 if k % 2 else .024) * rng.uniform(.88, 1.06), .48)
        if not light and k % 2:
            add(drums, kit['hat'], start + k * .5 * BEAT, .027, -.4)
    for b, kind, gain in [(.75, 'conga_hi', .052), (1.5, 'conga', .075),
                          (2.75, 'conga_hi', .056), (3.25, 'conga', .065)]:
        add(drums, kit[kind], start + b * BEAT + .006, gain * (.65 if light else 1), -.30)
    for b in ([0, 1.5, 3] if bar % 2 == 0 else [1, 2]):
        add(drums, kit['clave'], start + b * BEAT, .025, .25)
    if local == 7:
        for k in range(4):
            add(drums, kit['conga_hi' if k % 2 else 'conga'], start + (3 + k * .25) * BEAT,
                .045 + .006 * k, -.22 + .14 * k)

print('Designing surf, sea breeze, water and distant gulls...', flush=True)
# Overlapped, slowly shaped noise beds: no recorded or licensed samples.
for i, start in enumerate(np.arange(-4, DURATION, 4.2)):
    length = 9.0
    t = time_axis(length)
    env = np.sin(np.pi * t / length) ** 2
    wind = noise(length, 100, 1800) * env
    add(sea, wind, start, .006, -.7 if i % 2 else .7)
for i, start in enumerate(np.arange(-2, DURATION, 6.6)):
    length = 6.9
    t = time_axis(length)
    env = (1 - np.exp(-t / 1.35)) * np.exp(-t / 2.4)
    env *= np.minimum(1, (length - t) / 1.6)
    surf = noise(length, 170, 6500) * env * (1 + .12 * np.sin(2 * np.pi * 1.7 * t))
    add(sea, surf, start + .2 * np.sin(i), .048, -.55 if i % 2 else .55)
for start in [10, 33, 60, 88, 114, 137]:
    for call in range(2):
        length = .65 + call * .10
        t = time_axis(length)
        f = 1180 + 440 * np.sin(np.pi * t / length) - 260 * t / length
        phase = 2 * np.pi * np.cumsum(f * (1 + .012 * np.sin(2 * np.pi * 27 * t))) / SR
        gull = (np.sin(phase) + .20 * np.sin(2 * phase) + .08 * np.sin(3 * phase))
        gull *= np.sin(np.pi * t / length) ** 2
        add(sea, gull, start + call * .92, .010, .65 * np.sin(start))
for start in np.arange(2, DURATION, 2.7):
    length = .32
    t = time_axis(length)
    f = rng.uniform(450, 850)
    phase = 2 * np.pi * (f * t + 170 * t ** 2)
    water = np.sin(phase) * np.exp(-t / .045) * (1 - np.exp(-t / .004))
    add(sea, water, start + rng.uniform(0, .5), .009, rng.uniform(-.7, .7))


def reflections(bus, wet):
    result = bus.copy()
    for i, delay in enumerate([.047, .083, .139, .211, .307, .431, .593, .797, 1.037]):
        source = bus[:, ::-1] if i % 2 else bus
        result += np.roll(source, round(delay * SR), axis=0) * wet * np.exp(-delay / .55)
    return result


print('Mixing the circular stereo master...', flush=True)
mix = reflections(music, .085) + reflections(drums, .025) + reflections(sea, .04)
mix -= np.mean(mix, axis=0)
mix = np.tanh(mix * 1.25)
mix *= .79 / np.max(np.abs(mix))
# A sub-millisecond endpoint correction removes sample rounding discontinuity.
edge = 64
mix[-edge:] += np.linspace(0, 1, edge, dtype=np.float32)[:, None] * (mix[0] - mix[-1])
assert np.isfinite(mix).all()
assert np.max(np.abs(mix)) < 1
pcm = np.round(mix * 32767).astype('<i2')
path = OUT / 'Brisa_de_San_Andres_104BPM_LOOP.wav'
with wave.open(str(path), 'wb') as wav:
    wav.setnchannels(2)
    wav.setsampwidth(2)
    wav.setframerate(SR)
    wav.writeframes(pcm.tobytes())
windows = [float(np.sqrt(np.mean(mix[i:i + 8 * SR] ** 2))) for i in range(0, N - 8 * SR, 8 * SR)]
report = {
    'title': 'Brisa de San Andrés', 'bpm': BPM, 'bars': BARS, 'meter': '4/4', 'key': 'G major',
    'duration_seconds': DURATION, 'sample_rate': SR, 'channels': 2, 'pcm_bits': 16,
    'peak_dbfs': float(20 * np.log10(np.max(np.abs(mix)))),
    'rms_dbfs': float(20 * np.log10(np.sqrt(np.mean(mix ** 2)))),
    'eight_second_rms_range_db': float(20 * np.log10(max(windows) / min(windows))),
    'loop_endpoint_difference_pcm': (pcm[0].astype(int) - pcm[-1].astype(int)).tolist(),
    'finite_samples': bool(np.isfinite(mix).all()), 'clipped_samples': int(np.sum(np.abs(mix) >= 1)),
    'sections': sections,
    'instruments': ['acoustic guitar', 'ukulele', 'steel pans', 'Caribbean percussion', 'soft bass', 'mandolin'],
    'ambience': ['surf', 'sea breeze', 'distant gulls', 'water'],
    'production': 'Original composition with synthesized instruments and environmental sound design. No voices, external samples or field recordings.',
    'loop_method': '64 complete bars; circular note tails, ambience and reverberation; no fade to silence.',
}
(OUT / 'verificacion_san_andres.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps(report, indent=2, ensure_ascii=False), flush=True)
