import argparse
import math
import os
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


def periodic_fbm(x: float, y: float, base_period: int, seed: int, octaves: int = 5) -> float:
    value = 0.0
    amplitude = 1.0
    frequency = 1.0
    amplitude_sum = 0.0

    for octave in range(octaves):
        period = max(1, int(base_period * frequency))
        value += periodic_value_noise(x * frequency, y * frequency, period, seed + octave * 131) * amplitude
        amplitude_sum += amplitude
        amplitude *= 0.5
        frequency *= 2.0

    return value / amplitude_sum if amplitude_sum > 0.0 else 0.0


def ridge_noise(x: float, y: float, base_period: int, seed: int, octaves: int = 4) -> float:
    value = 0.0
    amplitude = 1.0
    frequency = 1.0
    amplitude_sum = 0.0

    for octave in range(octaves):
        period = max(1, int(base_period * frequency))
        n = periodic_value_noise(x * frequency, y * frequency, period, seed + octave * 173)
        ridge = 1.0 - abs(n)
        ridge *= ridge
        value += ridge * amplitude
        amplitude_sum += amplitude
        amplitude *= 0.55
        frequency *= 2.0

    return value / amplitude_sum if amplitude_sum > 0.0 else 0.0


def build_heightmap(size: int, seed: int):
    heightmap = [[0.0 for _ in range(size)] for _ in range(size)]

    for y in range(size):
        ny = y / float(size)
        for x in range(size):
            nx = x / float(size)

            macro = periodic_fbm(nx * 3.0, ny * 3.0, 3, seed, 4) * 0.75
            medium = ridge_noise(nx * 7.0, ny * 7.0, 7, seed + 91, 4) * 0.55
            detail = periodic_fbm(nx * 20.0, ny * 20.0, 20, seed + 211, 5) * 0.18
            pitting = ridge_noise(nx * 36.0, ny * 36.0, 36, seed + 377, 3) * 0.12
            strata = math.sin((nx * 1.7 + ny * 1.25) * math.tau) * 0.08

            h = macro + medium + detail - pitting + strata
            h = math.copysign(abs(h) ** 1.15, h)
            heightmap[y][x] = h

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
    parser = argparse.ArgumentParser(description="Generate a seamless rough stone normal texture.")
    parser.add_argument("--size", type=int, default=512)
    parser.add_argument("--strength", type=float, default=9.0)
    parser.add_argument("--seed", type=int, default=250325)
    parser.add_argument(
        "--output",
        default=os.path.join("Assets", "Resources", "Generated", "rough_stone_normal.png"),
    )
    args = parser.parse_args()

    os.makedirs(os.path.dirname(args.output), exist_ok=True)

    heightmap = build_heightmap(args.size, args.seed)
    pixels = height_to_normal(heightmap, args.strength)
    save_png(args.output, args.size, args.size, pixels)

    print(f"Generated: {os.path.abspath(args.output)}")


if __name__ == "__main__":
    main()
