from pixelart import Pic

TITLE = "달콤한 간식"


def pics():
    out = []

    p = Pic("컵케이크", ["#F7A8B8", "#C8913C", "#E05A47", "#FFF3E2"])
    p.poly([(22, 56), (78, 56), (70, 92), (30, 92)], 2, shade="box")
    for x in (36, 50, 64):
        p.cap(x, 60, x - 2 * (x - 50) / 14, 88, 3, 2, tone=2)
    p.ellipse(50, 50, 32, 14, 1)
    p.ellipse(50, 38, 24, 12, 1)
    p.ellipse(50, 28, 14, 9, 1)
    p.circle(52, 14, 7, 3)
    for x, y in ((34, 46), (60, 40), (44, 30), (66, 52)):
        p.ellipse(x, y, 2.5, 1.5, 4, shade="flat")
    out.append(p)

    p = Pic("도넛", ["#D9A066", "#F7A8B8", "#FFF3E2"])
    p.circle(50, 52, 40, 1)
    p.ellipse(50, 48, 36, 32, 2)
    import math
    for i in range(10):
        a = math.radians(i * 36 + 10)
        p.cap(50 + 24 * math.cos(a), 48 + 22 * math.sin(a), 50 + 28 * math.cos(a + 0.15), 48 + 26 * math.sin(a + 0.15), 3, 3)
    p.erase_ellipse(50, 50, 11, 10)
    out.append(p)

    p = Pic("아이스크림", ["#F7A8B8", "#F4E6C8", "#D9A066", "#8BD0C4"])
    p.poly([(28, 52), (72, 52), (50, 96)], 3, shade="box")
    for i in range(3):
        p.cap(34 + i * 10, 56, 48 + i * 6, 86, 2.6, 3, tone=2)
    p.circle(36, 46, 16, 2).circle(64, 46, 16, 2)
    p.circle(50, 28, 18, 1)
    p.circle(50, 10, 5, 4)
    out.append(p)

    p = Pic("조각 케이크", ["#FFF3E2", "#F7A8B8", "#E05A47", "#F4C27A"])
    p.poly([(10, 50), (70, 30), (90, 44), (90, 84), (10, 90)], 1, shade="box")
    p.poly([(10, 50), (70, 30), (90, 44), (30, 60)], 2, shade="flat")
    p.rect(12, 68, 88, 74, 2, shade="flat")
    p.rect(12, 82, 88, 88, 4, shade="flat")
    p.circle(62, 26, 8, 3)
    p.cap(62, 18, 66, 10, 3, 3, tone=2)
    out.append(p)

    p = Pic("쿠키", ["#D9A066", "#6B4430"])
    p.circle(50, 52, 40, 1)
    for x, y in ((34, 36), (58, 30), (68, 50), (40, 58), (58, 70), (28, 60), (48, 44)):
        p.ellipse(x, y, 5, 4, 2, shade="flat")
    p.poly([(80, 22), (92, 34), (84, 40)], 0, shade="flat")
    out.append(p)

    p = Pic("푸딩", ["#F6D98A", "#8A5A3C", "#E05A47", "#FFF3E2"])
    p.ellipse(50, 86, 42, 8, 4, shade="flat")
    p.poly([(24, 38), (76, 38), (84, 84), (16, 84)], 1, shade="box")
    p.ellipse(50, 38, 26, 8, 2)
    p.poly([(26, 38), (74, 38), (72, 50), (64, 46), (56, 52), (44, 46), (36, 52), (28, 46)], 2, shade="flat")
    p.circle(50, 24, 8, 3)
    p.cap(50, 16, 56, 8, 3, 3, tone=2)
    out.append(p)

    p = Pic("사탕", ["#E05A47", "#FFF3E2", "#F6C85F"])
    p.poly([(28, 50), (6, 34), (10, 66)], 3)
    p.poly([(72, 50), (94, 34), (90, 66)], 3)
    p.circle(50, 50, 24, 1)
    import math
    for i in range(3):
        a = math.radians(i * 120)
        p.arc(50 + 8 * math.cos(a), 50 + 8 * math.sin(a), 12, 5, i * 120, i * 120 + 120, 2)
    out.append(p)

    p = Pic("햄버거", ["#E0A45A", "#7A4E2D", "#81B29A", "#F6C85F"])
    p.poly([(12, 48), (18, 26), (50, 14), (82, 26), (88, 48)], 1, shade="sphere")
    p.rect(10, 48, 90, 56, 3, r=4, shade="flat")
    p.poly([(12, 54), (88, 54), (84, 64), (16, 64)], 4, shade="flat")
    p.rect(12, 62, 88, 74, 2, r=5)
    p.rect(14, 74, 86, 88, 1, r=6)
    for x, y in ((34, 28), (50, 22), (66, 28), (42, 36), (58, 36)):
        p.ellipse(x, y, 2.4, 1.6, 0, shade="flat")
    out.append(p)

    p = Pic("피자", ["#F6C85F", "#E05A47", "#D9A066", "#81B29A"])
    p.poly([(10, 18), (90, 18), (50, 94)], 1, shade="box")
    p.rect(8, 10, 92, 22, 3, r=6)
    for x, y in ((34, 34), (60, 32), (48, 54), (40, 72), (62, 50)):
        p.circle(x, y, 7, 2, shade="flat")
    for x, y in ((50, 36), (36, 52), (58, 66)):
        p.ellipse(x, y, 4, 2.4, 4, shade="flat")
    out.append(p)

    p = Pic("김밥", ["#3E5A4A", "#F4F1EA", "#F6C85F", "#E8743B"])
    p.circle(50, 52, 42, 1)
    p.circle(50, 52, 34, 2, shade="flat")
    p.circle(42, 44, 7, 3, shade="flat").circle(60, 46, 6, 4, shade="flat")
    p.rect(40, 58, 62, 66, 3, r=2, shade="flat")
    p.circle(38, 60, 5, 4, shade="flat")
    for x, y in ((30, 34), (70, 64), (64, 32), (34, 74), (52, 78)):
        p.ellipse(x, y, 2, 1.4, 2, shade="flat", tone=2)
    out.append(p)
    return out
