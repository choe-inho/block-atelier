"""앨범 미리보기 시트. 사용: python3 sheet.py a01_animals [a02_...] 출력.png"""
import importlib, sys
from PIL import Image, ImageDraw, ImageFont
from pixelart import shade_rgb, LINE_RGB, hexrgb, lerp

CANVAS = hexrgb("#EFE9DC")


def draw(rows, shades, colors, cell=14, painted=True):
    n = len(rows)
    im = Image.new("RGB", (n * cell, n * cell), CANVAS)
    d = ImageDraw.Draw(im)
    for y in range(n):
        for x in range(n):
            ch = rows[y][x]
            if ch == "0":
                continue
            if ch == "L":
                c = LINE_RGB
            else:
                base = colors[int(ch) - 1]
                c = shade_rgb(base, int(shades[y][x])) if painted else lerp(hexrgb(base), CANVAS, 0.68)
            d.rectangle([x * cell, y * cell, x * cell + cell - 1, y * cell + cell - 1], fill=c)
    return im


def main():
    mods = sys.argv[1:-1]
    out = sys.argv[-1]
    tiles = []
    font = ImageFont.truetype("/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc", 16) if False else None
    for m in mods:
        mod = importlib.import_module("albums." + m)
        for p in mod.pics():
            rows, shades = p.render()
            paint = sum(1 for r in rows for ch in r if ch not in "0L")
            a = draw(rows, shades, p.colors)
            b = draw(rows, shades, p.colors, painted=False)
            t = Image.new("RGB", (a.width * 2 + 12, a.height + 22), (255, 255, 255))
            t.paste(a, (0, 0)); t.paste(b, (a.width + 12, 0))
            ImageDraw.Draw(t).text((2, a.height + 4), f"{len(p.colors)}c {paint}px", fill=(60, 60, 60))
            tiles.append(t)
    cols = 5
    w, h = tiles[0].size
    rowsn = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (w + 10) + 10, rowsn * (h + 10) + 10), (255, 255, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t, (10 + (i % cols) * (w + 10), 10 + (i // cols) * (h + 10)))
    sheet.save(out)


if __name__ == "__main__":
    main()
