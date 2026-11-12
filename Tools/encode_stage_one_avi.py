"""MJPEG AVI writer, using Python's standard library only.

Called by Encode-StageOneVideo.ps1 after System.Drawing produces JPEG frames.
Frames are resampled at a constant rate from their actual capture timestamps.
The resulting development evidence is silent (no audio stream was captured).
"""

import argparse
import bisect
import json
import math
from pathlib import Path
import struct


def chunk(fourcc, data):
    return fourcc + struct.pack("<I", len(data)) + data + (b"\0" if len(data) & 1 else b"")


def avi_header(width, height, fps, frame_count, largest_frame):
    avih = struct.pack(
        "<14I", round(1_000_000 / fps), largest_frame * fps, 0, 0x10,
        frame_count, 0, 1, largest_frame, width, height, 0, 0, 0, 0,
    )
    strh = struct.pack(
        "<4s4sIHH8I4h", b"vids", b"MJPG", 0, 0, 0,
        0, 1, fps, 0, frame_count, largest_frame, 0xFFFFFFFF, 0,
        0, 0, width, height,
    )
    strf = struct.pack(
        "<IiiHH4sIiiII", 40, width, height, 1, 24, b"MJPG",
        width * height * 3, 0, 0, 0, 0,
    )
    stream_list = chunk(b"LIST", b"strl" + chunk(b"strh", strh) + chunk(b"strf", strf))
    return chunk(b"LIST", b"hdrl" + chunk(b"avih", avih) + stream_list)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    width, height, fps = (int(manifest[key]) for key in ("width", "height", "fps"))
    if not 1 <= fps <= 60 or not 1 <= width <= 32767 or not 1 <= height <= 32767:
        raise ValueError("Invalid dimensions or frame rate")
    frames = sorted(manifest["frames"], key=lambda item: float(item["elapsedSeconds"]))
    if len(frames) < 2:
        raise ValueError("At least two source frames are required")
    source_start = float(frames[0]["elapsedSeconds"])
    timestamps = [float(item["elapsedSeconds"]) - source_start for item in frames]
    if any(not math.isfinite(value) or value < 0 for value in timestamps):
        raise ValueError("Invalid capture timestamp")
    paths = [Path(item["file"]) for item in frames]
    largest_frame = max(path.stat().st_size for path in paths)
    duration = timestamps[-1] + 1 / fps
    output_count = max(2, math.ceil(duration * fps))
    samples = [min(len(frames) - 1, max(0, bisect.bisect_right(timestamps, number / fps) - 1))
               for number in range(output_count)]
    header = avi_header(width, height, fps, output_count, largest_frame)
    projected_size = len(header) + sum(8 + paths[i].stat().st_size + (paths[i].stat().st_size & 1)
                                       for i in samples) + 16 * output_count + 32
    if projected_size >= 0xFFFFFFFF:
        raise ValueError("Capture exceeds the AVI 1.0 RIFF limit")
    output = args.output.resolve()
    temporary_output = output.with_suffix(output.suffix + ".partial")
    try:
        with temporary_output.open("wb") as stream:
            stream.write(b"RIFF\0\0\0\0AVI ")
            stream.write(header)
            movi_start = stream.tell()
            stream.write(b"LIST\0\0\0\0movi")
            offsets = []
            cached_index, cached_data = -1, None
            for index in samples:
                if index != cached_index:
                    cached_data = paths[index].read_bytes()
                    if not cached_data.startswith(b"\xff\xd8"):
                        raise ValueError("A source frame is not a JPEG")
                    cached_index = index
                offset = stream.tell() - movi_start - 8
                offsets.append((offset, len(cached_data)))
                stream.write(chunk(b"00dc", cached_data))
            movi_end = stream.tell()
            stream.seek(movi_start + 4)
            stream.write(struct.pack("<I", movi_end - movi_start - 8))
            stream.seek(movi_end)
            index_data = b"".join(struct.pack("<4sIII", b"00dc", 0x10, offset, size)
                                  for offset, size in offsets)
            stream.write(chunk(b"idx1", index_data))
            end = stream.tell()
            stream.seek(4)
            stream.write(struct.pack("<I", end - 8))
        temporary_output.replace(output)
    finally:
        if temporary_output.exists():
            temporary_output.unlink()
    notes = (
        f"Video de evidencia de desarrollo: {output.name}\n"
        f"Formato: AVI 1.0 / Motion JPEG (MJPG), {width} x {height}, {fps} fps.\n"
        f"Frames originales: {len(frames)}; frames del video: {output_count}.\n"
        f"Duración: {output_count / fps:.3f} s.\n"
        f"Primer frame: t={source_start:.3f} s desde el inicio del run.\n"
        "Cadencia: remuestreo de los timestamps reales de resultados.json; "
        "se repite el último frame disponible durante los intervalos entre capturas.\n"
        "Relación de aspecto original conservada; ancho máximo configurado por PowerShell.\n"
        "Sin audio: la captura de frames de Game View no grabó una pista sonora.\n"
        f"Estado funcional de origen: {manifest.get('sourceState', 'unknown')}.\n"
        "La revisión visual y la validación de entrega Android se documentan por separado.\n"
    )
    output.with_suffix(".txt").write_text(notes, encoding="utf-8")
    print(f"AVI generado: {output} ({width}x{height}, {fps} fps, {output_count / fps:.3f} s, sin audio)")


if __name__ == "__main__":
    main()
