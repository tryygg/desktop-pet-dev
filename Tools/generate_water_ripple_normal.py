import argparse
import math
import os
import random
import struct
import zlib


def fade(t: float) -> float:
    return t * t * (3.0 - 2.0 * t)


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def hash2(ix: int, iy: int, seed: int) -> float:
    n = ix * 374761393 + iy * 668265263 + seed * 1442695041
    n = (n ^ (n >> 13)) & 0xFFFFFFFF
    n = (n * 1274126177) & 0xFFFFFFFF
    n ^= n >> 16
    return (n & 0xFFFFFFFF) / 4294967295.0


def periodic_value_noise(x: float, y: float, period: int, seed: int) -> float:
    x0 = math.floor(x)
    y0 = math.floor(y)
    x1 = x0 + 1
    y1 = y0 + 1

    sx = fade(x - x0)
    sy = fade(y - y0)

    n00 = hash2(x0 % period, y0 % period, seed)
    n10 = hash2(x1 % period, y0 % period, seed)
    n01 = hash2(x0 % period, y1 % period, seed)
    n11 = hash2(x1 % period, y1 % period, seed)

    ix0 = lerp(n00, n10, sx)
    ix1 = lerp(n01, n11, sx)
    return lerp(ix0, ix1, sy) * 2.0 - 1.0


def periodic_fractal_noise(x: float, y: float, base_period: int, seed: int) -> float:
    value = 0.0
    amplitude = 1.0
    frequency = 1.0
    amplitude_sum = 0.0

    for octave in range(4):
        period = max(1, int(base_period * frequency))
        value += periodic_value_noise(x * frequency, y * frequency, period, seed + octave * 97) * amplitude
        amplitude_sum += amplitude
        amplitude *= 0.5
        frequency *= 2.0

    return value / amplitude_sum if amplitude_sum > 0.0 else 0.0


def wrapped_delta(a: float, b: float) -> float:
    delta = a - b
    if delta > 0.5:
        delta -= 1.0
    elif delta < -0.5:
        delta += 1.0
    return delta


def ripple_height(nx: float, ny: float, centers) -> float:
    height = 0.0
    for cx, cy, freq, amp, phase in centers:
        dx = wrapped_delta(nx, cx)
        dy = wrapped_delta(ny, cy)
        distance = math.sqrt(dx * dx + dy * dy)
        envelope = math.exp(-distance * 4.5)
        wave = math.sin(distance * freq - phase)
        height += wave * amp * envelope
    return height


def build_heightmap(size: int, seed: int):
    rng = random.Random(seed)
    centers = []
    for _ in range(7):
        centers.append(
            (
                rng.uniform(0.0, 1.0),
                rng.uniform(0.0, 1.0),
                rng.uniform(26.0, 48.0),
                rng.uniform(0.45, 0.9),
                rng.uniform(0.0, math.tau),
            )
        )

    heightmap = [[0.0 for _ in range(size)] for _ in range(size)]

    for y in range(size):
        ny = y / float(size)
        for x in range(size):
            nx = x / float(size)

            ripple = ripple_height(nx, ny, centers)
            large_noise = periodic_fractal_noise(nx * 4.0, ny * 4.0, 4, seed) * 0.28
            fine_noise = periodic_fractal_noise(nx * 12.0, ny * 12.0, 12, seed + 211) * 0.11
            directional = (
                math.sin((nx * 2.0 + ny * 3.0) * math.tau + 0.8)
                + math.cos((nx * 5.0 - ny * 2.0) * math.tau + 1.6)
            ) * 0.06

            heightmap[y][x] = ripple + large_noise + fine_noise + directional

    return heightmap


def sample(heightmap, x: int, y: int) -> float:
    size = len(heightmap)
    return heightmap[y % size][x % size]


def height_to_normal(heightmap, strength: float):
    size = len(heightmap)
    pixels = bytearray()

    for y in range(size):
        row = bytearray([0])
        for x in range(size):
            h_l = sample(heightmap, x - 1, y)
            h_r = sample(heightmap, x + 1, y)
            h_d = sample(heightmap, x, y - 1)
            h_u = sample(heightmap, x, y + 1)

            dx = (h_r - h_l) * strength
            dy = (h_u - h_d) * strength

            nx = -dx
            ny = -dy
            nz = 1.0
            length = math.sqrt(nx * nx + ny * ny + nz * nz)
            nx /= length
            ny /= length
            nz /= length

            row.extend(
                (
                    int((nx * 0.5 + 0.5) * 255.0),
                    int((ny * 0.5 + 0.5) * 255.0),
                    int((nz * 0.5 + 0.5) * 255.0),
                    255,
                )
            )
        pixels.extend(row)

    return pixels


def save_png(path: str, width: int, height: int, rgba_bytes: bytes):
    def chunk(tag: bytes, data: bytes) -> bytes:
        return (
            struct.pack("!I", len(data))
            + tag
            + data
            + struct.pack("!I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        )

    png_signature = b"\x89PNG\r\n\x1a\n"
    ihdr = struct.pack("!IIBBBBB", width, height, 8, 6, 0, 0, 0)
    idat = zlib.compress(rgba_bytes, 9)

    with open(path, "wb") as f:
        f.write(png_signature)
        f.write(chunk(b"IHDR", ihdr))
        f.write(chunk(b"IDAT", idat))
        f.write(chunk(b"IEND", b""))


def main():
    parser = argparse.ArgumentParser(description="Generate a seamless tiled water ripple normal noise texture.")
    parser.add_argument("--size", type=int, default=512)
    parser.add_argument("--strength", type=float, default=6.5)
    parser.add_argument("--seed", type=int, default=240325)
    parser.add_argument(
        "--output",
        default=os.path.join("Assets", "Resources", "Generated", "water_ripple_normal_seamless.png"),
    )
    args = parser.parse_args()

    os.makedirs(os.path.dirname(args.output), exist_ok=True)

    heightmap = build_heightmap(args.size, args.seed)
    pixels = height_to_normal(heightmap, args.strength)
    save_png(args.output, args.size, args.size, pixels)

    print(f"Generated: {os.path.abspath(args.output)}")


if __name__ == "__main__":
    main()
