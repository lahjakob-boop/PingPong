"""Synthesises crowd sounds for the PingPong game (no external samples).

cheer.wav     - crowd cheers when the player scores
applause.wav  - polite applause when the computer scores
win.wav       - big, long cheer when the player wins the game
lose.wav      - "aww" + applause when the player loses the game
"""
import sys
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve
from scipy.io import wavfile

SR = 44100
rng = np.random.default_rng(67)


def bandpass(x, lo, hi, order=2):
    sos = butter(order, [lo, hi], btype="band", fs=SR, output="sos")
    return sosfilt(sos, x)


def lowpass(x, hi, order=2):
    sos = butter(order, hi, btype="low", fs=SR, output="sos")
    return sosfilt(sos, x)


def envelope(n, attack, release_start, release_len):
    t = np.arange(n) / SR
    env = np.clip(t / max(attack, 1e-3), 0, 1)
    rel = np.clip((t - release_start) / max(release_len, 1e-3), 0, 1)
    return env * (1 - rel) ** 1.6


def formant_gain(f, formants):
    g = np.full_like(f, 0.02, dtype=float)
    for fc, bw, amp in formants:
        g += amp / (1 + ((f - fc) / bw) ** 2)
    return g


VOWELS = {
    "ah": [(750, 120, 1.0), (1200, 160, 0.6), (2600, 250, 0.25)],
    "eh": [(550, 100, 1.0), (1800, 200, 0.55), (2600, 250, 0.25)],
    "oh": [(500, 100, 1.0), (900, 140, 0.6), (2500, 250, 0.15)],
    "aw": [(650, 110, 1.0), (1000, 150, 0.5), (2500, 250, 0.12)],
}


def voice(dur, f0, vowel, rise=0.15, fall=0.18, vib_rate=5.5, vib_depth=0.015):
    n = int(dur * SR)
    t = np.arange(n) / SR
    # pitch contour: quick rise, then a slow fall (excited "yeaah!")
    contour = 1 + rise * np.clip(t / 0.18, 0, 1) - fall * np.clip((t - 0.2) / max(dur - 0.2, 0.1), 0, 1)
    vib = 1 + vib_depth * np.sin(2 * np.pi * vib_rate * t + rng.uniform(0, 6.28))
    jitter = 1 + 0.01 * lowpass(rng.standard_normal(n), 20)
    f = f0 * contour * vib * jitter
    phase = 2 * np.pi * np.cumsum(f) / SR
    out = np.zeros(n)
    form = VOWELS[vowel]
    for k in range(1, 26):
        fk = f * k
        if np.min(fk) > 5000:
            break
        g = formant_gain(fk, form) / k ** 0.6
        g[fk > 6000] = 0
        out += g * np.sin(k * phase)
    # breathiness
    out += 0.08 * bandpass(rng.standard_normal(n), 600, 3500)
    env = envelope(n, rng.uniform(0.04, 0.12), dur * rng.uniform(0.35, 0.6), dur * 0.5)
    return out * env


def clap():
    n = int(0.03 * SR)
    t = np.arange(n) / SR
    burst = rng.standard_normal(n) * np.exp(-t / rng.uniform(0.004, 0.008))
    lo = rng.uniform(700, 1100)
    return bandpass(burst, lo, lo * rng.uniform(2.2, 3.2))


def whistle(dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = 2300 + 500 * np.sin(np.pi * t / dur) + 40 * np.sin(2 * np.pi * 7 * t)
    phase = 2 * np.pi * np.cumsum(f) / SR
    env = envelope(n, 0.03, dur * 0.7, dur * 0.3)
    return np.sin(phase) * env


def hall_reverb(x, length=1.4, mix=0.28):
    n = int(length * SR)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-t / (length / 5))
    ir = lowpass(ir, 4500)
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = fftconvolve(x, ir)[: len(x)]
    return (1 - mix) * x + mix * wet * 0.9


def place(buf, sig, start):
    i = int(start * SR)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sig))
    buf[i:j] += sig[: j - i]


def crowd(total, n_voices, vowels, voice_gain, claps_per_sec, clap_gain, bed_gain, whistles, swell=0.18, rise=(0.08, 0.25), fall=(0.1, 0.3), voice_spread=0.35):
    n = int(total * SR)
    buf = np.zeros(n)

    # noise bed: the general roar of a crowd
    bed = bandpass(rng.standard_normal(n), 250, 2600)
    lfo = 1 + 0.25 * lowpass(rng.standard_normal(n), 3) * 30
    buf += bed_gain * bed * envelope(n, swell, total * 0.45, total * 0.55) * np.clip(lfo, 0.5, 1.5)

    # voices
    for _ in range(n_voices):
        female = rng.random() < 0.45
        f0 = rng.uniform(210, 380) if female else rng.uniform(105, 190)
        dur = rng.uniform(0.7, min(2.4, total * 0.75))
        v = voice(dur, f0, rng.choice(vowels), rise=rng.uniform(*rise), fall=rng.uniform(*fall))
        place(buf, v * voice_gain * rng.uniform(0.35, 1.0), rng.uniform(0.0, voice_spread))

    # applause
    t = 0.12
    while t < total - 0.2:
        density = claps_per_sec * (1 - (t / total) ** 2.2)
        place(buf, clap() * clap_gain * rng.uniform(0.3, 1.0), t)
        t += rng.exponential(1 / max(density, 1))

    for _ in range(whistles):
        place(buf, whistle(rng.uniform(0.35, 0.6)) * 0.12, rng.uniform(0.15, 0.8))

    buf = hall_reverb(buf)
    # gentle fade-out and soft limiting
    fade = np.ones(n)
    fl = int(0.4 * SR)
    fade[-fl:] = np.linspace(1, 0, fl) ** 2
    buf *= fade
    buf /= np.max(np.abs(buf)) + 1e-9
    buf = np.tanh(buf * 1.6) / np.tanh(1.6)
    return buf * 0.9


def save(path, x):
    wavfile.write(path, SR, (np.clip(x, -1, 1) * 32767).astype(np.int16))


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    save(f"{out}/CrowdCheer.wav", crowd(3.2, 70, ["ah", "eh", "oh"], 0.55, 75, 1.0, 0.35, 2))
    save(f"{out}/CrowdApplause.wav", crowd(2.6, 8, ["eh", "oh"], 0.25, 55, 1.0, 0.18, 0))
    save(f"{out}/CrowdWin.wav", crowd(5.0, 150, ["ah", "eh", "oh"], 0.6, 95, 1.0, 0.45, 6, swell=0.25, voice_spread=2.2))
    save(f"{out}/CrowdLose.wav", crowd(3.4, 60, ["aw", "oh"], 0.6, 35, 0.8, 0.25, 0, rise=(0.0, 0.04), fall=(0.3, 0.45)))
    print("ok")
