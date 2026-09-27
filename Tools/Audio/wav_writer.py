"""Shared mono PCM encoding for the offline sound generators."""

import array
import sys
import wave


def write_wav(path, samples, peak, sample_rate):
    """Write 16-bit PCM; return centered samples and gain for caller diagnostics."""
    mean = sum(samples) / len(samples)
    samples = [sample - mean for sample in samples]
    scale = peak / max(abs(sample) for sample in samples)
    pcm = array.array("h", (round(sample * scale * 32767) for sample in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(pcm.tobytes())
    return samples, scale
