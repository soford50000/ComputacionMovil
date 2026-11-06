"""Efectos originales sintetizados para botones y pesca; sin muestras externas."""
from pathlib import Path
import json
import wave
import numpy as np

SR = 44100
OUT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio/Efectos'
OUT.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(25102026)
report = {}


def axis(seconds):
    return np.arange(round(seconds * SR)) / SR


def noise(t, low, high):
    f = np.fft.rfftfreq(len(t), 1 / SR)
    filt = (1 - np.exp(-(f / low) ** 4)) * np.exp(-(f / high) ** 4)
    s = np.fft.irfft(np.fft.rfft(rng.normal(size=len(t))) * filt, n=len(t))
    return s / (np.std(s) + 1e-9)


def save(name, sound, peak=.65):
    sound -= np.mean(sound)
    # Remove hard attacks and tails; these are one-shot effects, not loops.
    n = min(round(.004 * SR), len(sound) // 4)
    sound[:n] *= np.linspace(0, 1, n)
    sound[-n:] *= np.linspace(1, 0, n)
    sound *= peak / max(np.max(np.abs(sound)), 1e-9)
    assert np.isfinite(sound).all() and np.max(np.abs(sound)) < 1
    pcm = np.round(sound * 32767).astype('<i2')
    with wave.open(str(OUT / (name + '.wav')), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    report[name] = {'duration_seconds': len(sound) / SR,
                    'peak_dbfs': float(20 * np.log10(np.max(np.abs(sound)))),
                    'rms_dbfs': float(20 * np.log10(np.sqrt(np.mean(sound ** 2)))),
                    'clipped_samples': int(np.sum(np.abs(sound) >= 1))}


# Wooden click with a soft rounded body, avoiding piercing high frequencies.
t = axis(.10)
s = (np.sin(2 * np.pi * 720 * t) + .35 * np.sin(2 * np.pi * 1140 * t)) * np.exp(-t / .014)
s += .24 * noise(t, 800, 4200) * np.exp(-t / .008)
save('Clic', s, .45)

# Short line whoosh followed by a splash and a water droplet.
t = axis(.72)
s = noise(t, 400, 4800) * np.exp(-((t - .12) / .07) ** 2) * .27
u = np.maximum(t - .30, 0)
s += noise(t, 180, 6500) * (t >= .30) * (1 - np.exp(-u / .006)) * np.exp(-u / .085) * .75
s += (t >= .32) * np.sin(2 * np.pi * (880 * u - 500 * u ** 2)) * np.exp(-u / .10) * .16
save('Lanzar', s)

# Two bright bell taps: an audible but gentle bite notification.
t = axis(.44)
s = np.zeros_like(t)
for start, freq in [(0, 1046.5), (.11, 1568)]:
    u = np.maximum(t - start, 0)
    s += (t >= start) * (np.sin(2 * np.pi * freq * u) + .2 * np.sin(2 * np.pi * 2.01 * freq * u)) * np.exp(-u / .075)
save('Pique', s, .52)

# Three tiny mechanical reel ticks, once per press (not every frame held).
t = axis(.12)
s = np.zeros_like(t)
for start in [0, .032, .064]:
    u = np.maximum(t - start, 0)
    s += (t >= start) * (np.sin(2 * np.pi * 560 * u) + .35 * noise(t, 1800, 5800)) * np.exp(-u / .006)
save('Tirar', s, .36)


def notes(notes, seconds):
    t = axis(seconds)
    s = np.zeros_like(t)
    for start, freq, decay in notes:
        u = np.maximum(t - start, 0)
        s += (t >= start) * (1 - np.exp(-u / .004)) * np.exp(-u / decay) * (
            np.sin(2 * np.pi * freq * u) + .16 * np.sin(2 * np.pi * 2 * freq * u))
    return s


save('Captura', notes([(0, 523.25, .16), (.12, 659.25, .18), (.24, 783.99, .22)], .95), .60)
save('Recoger', notes([(0, 1318.51, .07), (.08, 1567.98, .10)], .38), .48)
t = axis(.48)
s = np.sin(2 * np.pi * (350 * t - 220 * t ** 2)) * np.exp(-t / .10)
s += .15 * noise(t, 150, 2500) * np.exp(-t / .08)
save('Escape', s, .45)
(Path(__file__).resolve().parent / 'verificacion_efectos.json').write_text(
    json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps(report, indent=2))
