from pixelart import Pic

TITLE = "꽃과 식물"
LEAF = "#6BAA5C"


def pics():
    out = []

    p = Pic("튤립", ["#E05A47", LEAF, "#F7A8B8"])
    p.cap(50, 50, 50, 94, 6, 2)
    p.ellipse(34, 76, 8, 18, 2, rot=-35).ellipse(66, 80, 8, 16, 2, rot=35)
    p.poly([(26, 20), (38, 34), (50, 14), (62, 34), (74, 20), (76, 44), (66, 58), (34, 58), (24, 44)], 1, shade="sphere")
    p.poly([(44, 38), (50, 22), (56, 38), (50, 50)], 3, shade="flat")
    out.append(p)

    p = Pic("해바라기", ["#F6C85F", "#7A4E2D", LEAF])
    p.cap(50, 60, 50, 96, 6, 3)
    p.ellipse(66, 82, 12, 6, 3, rot=-30)
    import math
    for i in range(12):
        a = math.radians(i * 30)
        p.ellipse(50 + 26 * math.cos(a), 42 + 26 * math.sin(a), 12, 6, 1, rot=i * 30)
    p.circle(50, 42, 20, 2)
    for x, y in ((44, 36), (56, 36), (50, 46), (42, 48), (58, 48)):
        p.circle(x, y, 2, 2, shade="flat", tone=0)
    out.append(p)

    p = Pic("선인장", ["#6BAA5C", "#D97B4A", "#F7A8B8"])
    p.poly([(20, 74), (80, 74), (74, 96), (26, 96)], 2, shade="box")
    p.rect(16, 68, 84, 78, 2, r=3)
    p.rect(38, 14, 62, 70, 1, r=12, shade="sphere")
    p.rect(16, 30, 30, 52, 1, r=7, shade="sphere")
    p.rect(20, 46, 42, 56, 1, r=5, shade="flat")
    p.rect(70, 36, 84, 56, 1, r=7, shade="sphere")
    p.rect(58, 50, 80, 60, 1, r=5, shade="flat")
    p.circle(50, 12, 6, 3)
    p.dot(44, 36, 2.6).dot(56, 36, 2.6)
    out.append(p)

    p = Pic("버섯", ["#E05A47", "#FFF3E2", "#F4E6C8"])
    p.rect(36, 54, 64, 92, 3, r=8, shade="box")
    p.ellipse(50, 44, 42, 30, 1)
    p.erase_rect(0, 56, 100, 60)
    p.rect(36, 54, 64, 92, 3, r=8, shade="box")
    for x, y, r in ((34, 30, 7), (60, 26, 6), (72, 44, 5), (28, 48, 5), (50, 44, 5)):
        p.circle(x, y, r, 2, shade="flat")
    out.append(p)

    p = Pic("나무", ["#6BAA5C", "#8A5A3C", "#E05A47"])
    p.rect(42, 58, 58, 94, 2, r=3)
    p.circle(50, 30, 22, 1).circle(30, 48, 20, 1).circle(70, 48, 20, 1).circle(50, 54, 20, 1)
    for x, y in ((38, 30), (64, 38), (44, 56), (26, 50), (70, 60)):
        p.circle(x, y, 4, 3)
    out.append(p)

    p = Pic("네잎클로버", ["#6BAA5C", "#A6D98C"])
    p.cap(54, 60, 70, 94, 5, 1)
    for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        cx, cy = 50 + dx * 17, 46 + dy * 17
        p.circle(cx - dx * 6, cy, 13, 1).circle(cx, cy - dy * 6, 13, 1)
    for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        p.cap(50, 46, 50 + dx * 18, 46 + dy * 18, 3, 2)
    out.append(p)

    p = Pic("장미", ["#D9364A", LEAF, "#F28A9A"])
    p.cap(50, 60, 50, 96, 5, 2)
    p.ellipse(34, 74, 12, 6, 2, rot=-30).ellipse(66, 84, 12, 6, 2, rot=30)
    p.circle(50, 38, 28, 1)
    p.arc(50, 38, 18, 4, 200, 20, 3).arc(52, 38, 9, 4, 30, 250, 3)
    out.append(p)

    p = Pic("화분", ["#6BAA5C", "#D97B4A", "#F6C85F"])
    p.poly([(24, 58), (76, 58), (70, 94), (30, 94)], 2, shade="box")
    p.rect(20, 54, 80, 64, 2, r=3)
    p.ellipse(34, 34, 10, 20, 1, rot=-30).ellipse(66, 34, 10, 20, 1, rot=30).ellipse(50, 26, 10, 22, 1)
    p.circle(50, 12, 6, 3)
    out.append(p)

    p = Pic("벚꽃", ["#F7C4CF", "#E07A8F", "#F6C85F"])
    import math
    for i in range(5):
        a = math.radians(-90 + i * 72)
        p.ellipse(50 + 22 * math.cos(a), 50 + 22 * math.sin(a), 17, 13, 1, rot=i * 72 - 90 + 90)
        p.poly([(50 + 36 * math.cos(a - 0.12), 50 + 36 * math.sin(a - 0.12)), (50 + 30 * math.cos(a), 50 + 30 * math.sin(a)),
                (50 + 36 * math.cos(a + 0.12), 50 + 36 * math.sin(a + 0.12)), (50 + 44 * math.cos(a), 50 + 44 * math.sin(a))], 0, shade="flat")
    p.circle(50, 50, 9, 2)
    for i in range(5):
        a = math.radians(-54 + i * 72)
        p.circle(50 + 13 * math.cos(a), 50 + 13 * math.sin(a), 2.6, 3, shade="flat")
    out.append(p)

    p = Pic("단풍잎", ["#E8743B", "#C8323A", "#8A5A3C"])
    pts = [(50, 6), (58, 26), (74, 16), (70, 38), (94, 36), (80, 52), (90, 62), (64, 64), (66, 80), (52, 70),
           (50, 72), (48, 70), (34, 80), (36, 64), (10, 62), (20, 52), (6, 36), (30, 38), (26, 16), (42, 26)]
    p.poly(pts, 1, shade="sphere")
    p.cap(50, 20, 50, 94, 3.5, 3)
    p.cap(50, 50, 72, 38, 3, 2).cap(50, 50, 28, 38, 3, 2).cap(50, 60, 66, 64, 3, 2).cap(50, 60, 34, 64, 3, 2)
    out.append(p)
    return out
