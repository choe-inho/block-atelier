from pixelart import Pic

TITLE = "과일 바구니"
LEAF = "#6BAA5C"
STEM = "#8A5A3C"


def pics():
    out = []

    p = Pic("사과", ["#E05A47", LEAF, STEM])
    p.cap(50, 30, 54, 12, 5, 3)
    p.ellipse(64, 18, 12, 6, 2, rot=-25)
    p.circle(36, 58, 28, 1).circle(64, 58, 28, 1)
    p.ellipse(50, 70, 30, 22, 1)
    p.ellipse(36, 48, 5, 8, 1, tone=0, shade="flat", rot=20)
    out.append(p)

    p = Pic("딸기", ["#E05A47", LEAF, "#FFF1C1"])
    p.poly([(14, 34), (86, 34), (72, 72), (50, 94), (28, 72)], 1, shade="sphere")
    p.ellipse(50, 36, 36, 10, 1)
    for x, y in ((30, 46), (50, 48), (70, 46), (38, 62), (62, 62), (50, 76)):
        p.ellipse(x, y, 2.6, 3.4, 3, shade="flat")
    p.poly([(30, 30), (50, 14), (70, 30), (60, 34), (50, 26), (40, 34)], 2, shade="flat")
    p.cap(50, 20, 50, 8, 5, 2)
    out.append(p)

    p = Pic("수박", ["#E05A47", "#4E9A5C", "#FFF3E2"])
    p.poly([(6, 34), (94, 34), (50, 92)], 2, shade="flat")
    p.poly([(12, 34), (88, 34), (50, 84)], 3, shade="flat")
    p.poly([(16, 34), (84, 34), (50, 78)], 1, shade="box")
    for x, y in ((34, 42), (50, 46), (66, 42), (42, 56), (58, 56), (50, 66)):
        p.ellipse(x, y, 2.4, 3.4, -1, shade="flat")
    out.append(p)

    p = Pic("바나나", ["#F6C85F", "#8A5A3C"])
    p.arc(50, 6, 62, 24, 50, 130, 1, shade="flat")
    p.arc(50, 6, 56, 6, 58, 122, 1, shade="flat", tone=0)
    p.arc(50, 6, 68, 5, 56, 124, 1, shade="flat", tone=2)
    p.cap(88, 52, 94, 42, 7, 2)
    p.cap(12, 52, 8, 46, 5, 2)
    out.append(p)

    p = Pic("체리", ["#C8323A", LEAF, STEM])
    p.line(34, 66, 52, 18, 4).line(68, 64, 52, 18, 4)
    p.ellipse(62, 16, 14, 7, 2, rot=-20)
    p.circle(32, 70, 18, 1).circle(68, 68, 18, 1)
    p.ellipse(26, 64, 3, 5, 1, tone=0, shade="flat").ellipse(62, 62, 3, 5, 1, tone=0, shade="flat")
    out.append(p)

    p = Pic("포도", ["#8E6CC8", LEAF, STEM])
    p.cap(50, 24, 52, 8, 5, 3)
    p.ellipse(66, 16, 13, 7, 2, rot=-20)
    for x, y in ((32, 32), (50, 30), (68, 32), (24, 48), (41, 47), (59, 47), (76, 48),
                 (32, 62), (50, 62), (68, 62), (41, 76), (59, 76), (50, 89)):
        p.circle(x, y, 9.5, 1)
    out.append(p)

    p = Pic("복숭아", ["#F7A8B8", "#F4A259", LEAF])
    p.ellipse(62, 18, 13, 7, 3, rot=-25)
    p.circle(50, 58, 36, 1)
    p.ellipse(62, 66, 18, 20, 2, shade="flat")
    p.arc(46, 58, 30, 3.2, 290, 360, -1)
    out.append(p)

    p = Pic("레몬", ["#F6D35F", "#FFF6C8", LEAF])
    p.ellipse(50, 56, 38, 28, 1, rot=-20)
    p.ellipse(15, 70, 7, 5, 1, rot=-20).ellipse(85, 42, 7, 5, 1, rot=-20)
    p.ellipse(40, 46, 12, 5, 2, rot=-20, shade="flat")
    p.ellipse(72, 30, 14, 6, 3, rot=-40)
    out.append(p)

    p = Pic("파인애플", ["#F6C85F", "#6BAA5C", "#C8913C"])
    for pts in ([(50, 4), (40, 34), (60, 34)], [(30, 12), (36, 36), (50, 34)], [(70, 12), (50, 34), (64, 36)],
                [(16, 26), (34, 38), (44, 34)], [(84, 26), (56, 34), (66, 38)]):
        p.poly(pts, 2, shade="flat")
    p.ellipse(50, 66, 28, 30, 1)
    for x, y in ((38, 50), (62, 50), (50, 60), (30, 64), (70, 64), (38, 72), (62, 72), (50, 84), (50, 40)):
        p.poly([(x, y - 4), (x + 4, y), (x, y + 4), (x - 4, y)], 3, shade="flat")
    out.append(p)

    p = Pic("배", ["#D9D46A", "#EFEAB0", STEM])
    p.cap(50, 22, 54, 8, 5, 3)
    p.circle(50, 38, 18, 1)
    p.circle(50, 66, 28, 1)
    p.ellipse(40, 58, 6, 10, 2, shade="flat", rot=15)
    for x, y in ((56, 70), (64, 58), (46, 80)):
        p.circle(x, y, 1.8, 3, shade="flat")
    out.append(p)
    return out
