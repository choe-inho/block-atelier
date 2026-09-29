import math
from pixelart import Pic

TITLE = "하늘과 날씨"
SUN = "#F6C85F"
CLOUD = "#F4F1EA"
RAIN = "#5DA9E9"


def star(cx, cy, r1, r2, n=5, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = math.radians(rot + i * 180 / n)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def cloud(p, cx, cy, s, c):
    p.circle(cx - 18 * s, cy + 4 * s, 13 * s, c)
    p.circle(cx + 2 * s, cy - 6 * s, 18 * s, c)
    p.circle(cx + 20 * s, cy + 4 * s, 12 * s, c)
    p.rect(cx - 30 * s, cy + 2 * s, cx + 32 * s, cy + 16 * s, c, r=7 * s)


def pics():
    out = []

    p = Pic("해", [SUN, "#F28C38", "#F7A8B8"])
    for i in range(8):
        a = math.radians(i * 45)
        x, y = 50 + 38 * math.cos(a), 50 + 38 * math.sin(a)
        p.poly([(50 + 28 * math.cos(a - 0.28), 50 + 28 * math.sin(a - 0.28)), (x, y),
                (50 + 28 * math.cos(a + 0.28), 50 + 28 * math.sin(a + 0.28))], 2, shade="flat")
    p.circle(50, 50, 27, 1)
    p.dot(41, 47, 3).dot(59, 47, 3)
    p.circle(35, 56, 4, 3, shade="flat").circle(65, 56, 4, 3, shade="flat")
    out.append(p)

    p = Pic("구름", [CLOUD, "#F7A8B8", "#A8D8F0"])
    cloud(p, 50, 50, 1.35, 1)
    p.dot(40, 54, 3).dot(62, 54, 3)
    p.circle(30, 62, 5, 2, shade="flat").circle(72, 62, 5, 2, shade="flat")
    for x in (30, 50, 70):
        p.cap(x, 84, x - 3, 92, 5, 3)
    out.append(p)

    p = Pic("무지개", ["#E05A47", "#F6C85F", "#81B29A", RAIN, CLOUD])
    for i, c in enumerate((1, 2, 3, 4)):
        p.arc(50, 74, 42 - i * 8, 8.5, 180, 360, c)
    cloud(p, 16, 72, 0.55, 5)
    cloud(p, 84, 72, 0.55, 5)
    out.append(p)

    p = Pic("달", [SUN, "#B8A9D9"])
    p.circle(50, 50, 38, 1)
    p.erase_ellipse(70, 38, 30, 30)
    p.circle(80, 76, 4, 2, shade="flat").circle(84, 20, 3, 2, shade="flat")
    p.dot(32, 50, 3)
    out.append(p)

    p = Pic("별", [SUN, "#F28C38"])
    p.poly(star(50, 54, 46, 20), 1, shade="sphere")
    p.poly(star(50, 54, 22, 10), 2, shade="flat", tone=0)
    p.dot(42, 54, 3).dot(58, 54, 3)
    out.append(p)

    p = Pic("번개구름", ["#9AA3B5", SUN, RAIN])
    cloud(p, 50, 36, 1.3, 1)
    p.poly([(52, 58), (38, 80), (50, 80), (42, 98), (66, 72), (54, 72), (62, 58)], 2, shade="flat")
    p.cap(20, 66, 16, 76, 5, 3).cap(80, 66, 76, 76, 5, 3)
    out.append(p)

    p = Pic("눈사람", ["#F4F1EA", "#E05A47", "#F28C38", "#474556"])
    p.circle(50, 70, 26, 1)
    p.circle(50, 34, 19, 1)
    p.rect(30, 8, 70, 16, 4, r=2).rect(38, 0, 62, 12, 4, r=2)
    p.rect(28, 48, 72, 56, 2, r=4, shade="flat")
    p.rect(58, 50, 66, 70, 2, r=2, shade="flat")
    p.poly([(50, 36), (64, 40), (50, 42)], 3, shade="flat")
    p.dot(43, 30, 3).dot(57, 30, 3)
    p.dot(50, 66, 2.8).dot(50, 78, 2.8)
    out.append(p)

    p = Pic("우산", ["#E05A47", "#F4F1EA", "#8A5A3C"])
    p.ellipse(50, 50, 44, 38, 1)
    p.erase_rect(0, 50, 100, 100)
    for x in (16, 38, 62, 84):
        p.erase_ellipse(x, 54, 11, 6)
    p.poly([(50, 12), (40, 48), (60, 48)], 2, shade="flat")
    p.rect(47, 44, 53, 84, 3, shade="flat")
    p.arc(40, 84, 10, 6, 0, 180, 3)
    out.append(p)

    p = Pic("눈꽃", ["#A8D8F0", "#5DA9E9"])
    for i in range(6):
        a = math.radians(i * 60 - 90)
        ex, ey = 50 + 42 * math.cos(a), 50 + 42 * math.sin(a)
        p.cap(50, 50, ex, ey, 9, 1)
        mx, my = 50 + 28 * math.cos(a), 50 + 28 * math.sin(a)
        for s in (-1, 1):
            b = a + s * math.radians(45)
            p.cap(mx, my, mx + 12 * math.cos(b), my + 12 * math.sin(b), 7, 1)
    p.circle(50, 50, 10, 2)
    out.append(p)

    p = Pic("비구름", ["#C9D1DE", RAIN, "#F6C85F"])
    p.circle(76, 26, 14, 3)
    cloud(p, 46, 40, 1.25, 1)
    for x, y in ((26, 70), (48, 78), (70, 70), (36, 88), (60, 90)):
        p.poly([(x, y - 9), (x - 5, y + 2), (x + 5, y + 2)], 2, shade="flat")
        p.circle(x, y + 2, 5, 2, shade="flat")
    p.dot(38, 44, 3).dot(56, 44, 3)
    out.append(p)
    return out
