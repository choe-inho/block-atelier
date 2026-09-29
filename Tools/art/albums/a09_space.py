import math
from pixelart import Pic

TITLE = "우주와 판타지"


def pics():
    out = []

    p = Pic("토성", ["#F2B872", "#B8A9D9", "#E07A5F"])
    p.ellipse(50, 50, 47, 13, 2, rot=-18, shade="flat")
    p.ellipse(50, 50, 38, 6, 0, rot=-18, shade="flat")
    p.circle(50, 50, 27, 1)
    p.cap(30, 44, 68, 32, 5, 3)
    p.cap(34, 62, 66, 54, 4, 3)
    ring = []
    for a in range(0, 181, 12):
        r = math.radians(a)
        ring.append((50 + 47 * math.cos(r), 50 + 13 * math.sin(r)))
    for a in range(180, -1, -12):
        r = math.radians(a)
        ring.append((50 + 38 * math.cos(r), 50 + 6 * math.sin(r)))
    c, s = math.cos(math.radians(-18)), math.sin(math.radians(-18))
    ring = [(50 + (x - 50) * c - (y - 50) * s, 50 + (x - 50) * s + (y - 50) * c) for x, y in ring]
    p.poly(ring, 2, shade="flat")
    out.append(p)

    p = Pic("외계인", ["#8FD694", "#474556", "#F7A8B8"])
    p.cap(36, 20, 28, 6, 4, 1).cap(64, 20, 72, 6, 4, 1)
    p.circle(27, 6, 5, 3).circle(73, 6, 5, 3)
    p.ellipse(50, 60, 22, 30, 1)
    p.ellipse(50, 42, 34, 26, 1)
    p.ellipse(37, 44, 9, 12, 2, rot=-25).ellipse(63, 44, 9, 12, 2, rot=25)
    p.circle(34, 40, 2.5, 3, shade="flat", tone=0).circle(60, 40, 2.5, 3, shade="flat", tone=0)
    out.append(p)

    p = Pic("UFO", ["#9AA3B5", "#A8D8F0", "#F6C85F", "#8FD694"])
    p.poly([(28, 70), (72, 70), (90, 100), (10, 100)], 4, shade="flat", tone=0)
    p.ellipse(50, 36, 22, 20, 2)
    p.ellipse(50, 56, 46, 14, 1)
    for x in (22, 38, 50, 62, 78):
        p.circle(x, 58, 4, 3, shade="flat")
    out.append(p)

    p = Pic("유령", ["#F4F1EA", "#F7A8B8", "#B8A9D9"])
    p.ellipse(50, 42, 34, 34, 1)
    p.rect(16, 42, 84, 82, 1, shade="flat")
    for x in (22, 50, 78):
        p.circle(x, 84, 11, 1, shade="flat")
    for x in (36, 64):
        p.erase_ellipse(x, 90, 6, 8)
    p.ellipse(8, 58, 8, 5, 1, rot=-30).ellipse(92, 58, 8, 5, 1, rot=30)
    p.dot(40, 44, 3.5).dot(60, 44, 3.5)
    p.ellipse(50, 58, 5, 6, 3, shade="flat")
    p.circle(30, 54, 4, 2, shade="flat").circle(70, 54, 4, 2, shade="flat")
    out.append(p)

    p = Pic("마법모자", ["#6C5BB5", "#F6C85F", "#E07A5F"])
    p.ellipse(50, 80, 46, 12, 1)
    p.poly([(22, 80), (78, 80), (60, 30), (74, 8), (46, 22)], 1, shade="box")
    p.poly([(26, 70), (74, 70), (72, 78), (28, 78)], 3, shade="flat")
    for cx, cy, r in ((50, 50, 7), (62, 34, 4)):
        pts = []
        for i in range(8):
            rr = r if i % 2 == 0 else r * 0.42
            a = math.radians(-90 + i * 45)
            pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
        p.poly(pts, 2, shade="flat")
    out.append(p)

    p = Pic("보석", ["#5DA9E9", "#A8D8F0", "#2F6FB0"])
    p.poly([(26, 18), (74, 18), (94, 40), (50, 92), (6, 40)], 1, shade="flat")
    p.poly([(26, 18), (40, 40), (6, 40)], 2, shade="flat")
    p.poly([(40, 40), (60, 40), (50, 18)], 2, shade="flat")
    p.poly([(74, 18), (94, 40), (60, 40)], 1, shade="flat")
    p.poly([(60, 40), (94, 40), (50, 92)], 3, shade="flat")
    p.poly([(6, 40), (40, 40), (50, 92)], 1, shade="flat", tone=0)
    p.line(6, 40, 94, 40, 2.8)
    out.append(p)

    p = Pic("성", ["#C9D1DE", "#E05A47", "#8A5A3C", "#5DA9E9"])
    p.rect(22, 44, 78, 94, 1, shade="box")
    for x in (8, 76):
        p.rect(x, 36, x + 16, 94, 1)
        p.poly([(x - 3, 38), (x + 19, 38), (x + 8, 12)], 2)
    for x in (26, 40, 54, 68):
        p.rect(x, 36, x + 7, 46, 1, shade="flat")
    p.rect(40, 70, 60, 94, 3, r=10)
    p.circle(50, 54, 5, 4, shade="flat")
    p.cap(88, 12, 88, 2, 2.5, 3).poly([(88, 2), (98, 5), (88, 8)], 2, shade="flat")
    out.append(p)

    p = Pic("로봇", ["#9AA3B5", "#5DA9E9", "#E05A47", "#F6C85F"])
    p.cap(50, 14, 50, 4, 3, 1).circle(50, 5, 5, 3)
    p.rect(24, 14, 76, 48, 1, r=6)
    p.rect(30, 22, 70, 40, 2, r=4, shade="flat")
    p.circle(40, 31, 5, 4, shade="flat").circle(60, 31, 5, 4, shade="flat")
    p.rect(20, 52, 80, 86, 1, r=6)
    p.rect(8, 54, 18, 78, 1, r=4).rect(82, 54, 92, 78, 1, r=4)
    p.rect(38, 60, 62, 76, 2, r=3, shade="flat")
    p.circle(44, 68, 3, 3, shade="flat").circle(56, 68, 3, 4, shade="flat")
    p.rect(28, 86, 42, 96, 1).rect(58, 86, 72, 96, 1)
    out.append(p)

    p = Pic("아기용", ["#7CC47F", "#F6D98A", "#E07A5F", "#F7A8B8"])
    p.poly([(60, 40), (84, 18), (80, 52)], 3, shade="flat")
    p.cap(70, 80, 94, 70, 12, 1).poly([(90, 62), (100, 70), (92, 78)], 3, shade="flat")
    p.ellipse(56, 70, 26, 22, 1)
    p.ellipse(56, 76, 14, 14, 2, shade="flat")
    p.circle(34, 40, 24, 1)
    p.poly([(22, 20), (26, 4), (32, 18)], 2, shade="flat").poly([(38, 18), (46, 4), (48, 20)], 2, shade="flat")
    p.ellipse(22, 50, 12, 9, 1)
    p.dot(38, 36, 3.5)
    p.circle(18, 48, 2, 2, shade="flat", tone=2)
    p.circle(44, 48, 4, 4, shade="flat")
    p.ellipse(42, 94, 8, 5, 1).ellipse(68, 94, 8, 5, 1)
    out.append(p)

    p = Pic("요정", ["#F7C8A8", "#B8A9D9", "#F7A8B8", "#F6C85F"])
    p.ellipse(28, 40, 20, 14, 2, rot=-30).ellipse(72, 40, 20, 14, 2, rot=30)
    p.ellipse(32, 62, 14, 9, 2, rot=30).ellipse(68, 62, 14, 9, 2, rot=-30)
    p.poly([(50, 44), (66, 86), (34, 86)], 3)
    p.circle(50, 32, 15, 1)
    p.ellipse(50, 22, 17, 10, 4)
    p.dot(44, 34, 2.8).dot(56, 34, 2.8)
    p.cap(64, 56, 84, 70, 3, 4)
    p.circle(86, 72, 5, 4, shade="flat")
    out.append(p)
    return out
