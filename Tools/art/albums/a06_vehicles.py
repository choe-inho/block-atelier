from pixelart import Pic

TITLE = "부릉부릉 탈것"
GLASS = "#A8D8F0"
TIRE = "#474556"


def pics():
    out = []

    p = Pic("자동차", ["#E05A47", GLASS, TIRE, "#F6C85F"])
    p.poly([(28, 40), (36, 22), (66, 22), (76, 40)], 1, shade="box")
    p.rect(8, 38, 92, 68, 1, r=8)
    p.poly([(34, 38), (40, 27), (50, 27), (50, 38)], 2, shade="flat").poly([(54, 38), (54, 27), (63, 27), (70, 38)], 2, shade="flat")
    p.circle(28, 70, 12, 3).circle(72, 70, 12, 3)
    p.circle(28, 70, 5, 4, shade="flat").circle(72, 70, 5, 4, shade="flat")
    p.rect(84, 46, 92, 52, 4, shade="flat")
    out.append(p)

    p = Pic("버스", ["#F6C85F", GLASS, TIRE, "#E05A47"])
    p.rect(6, 20, 94, 76, 1, r=8)
    for x in (12, 32, 52, 72):
        p.rect(x, 28, x + 16, 44, 2, r=2, shade="flat")
    p.rect(6, 52, 94, 58, 4, shade="flat")
    p.rect(80, 48, 92, 72, 2, r=2, shade="flat")
    p.circle(26, 78, 10, 3).circle(70, 78, 10, 3)
    out.append(p)

    p = Pic("비행기", ["#F4F1EA", "#5DA9E9", "#E05A47"])
    p.poly([(40, 44), (62, 44), (48, 10), (38, 10)], 2)
    p.poly([(40, 56), (62, 56), (48, 90), (38, 90)], 2)
    p.poly([(8, 42), (16, 42), (22, 50), (16, 58), (8, 58)], 3)
    p.ellipse(52, 50, 44, 11, 1)
    for x in (40, 52, 64, 76):
        p.circle(x, 48, 2.8, 2, shade="flat")
    p.ellipse(92, 50, 5, 5, 3, shade="flat")
    out.append(p)

    p = Pic("돛단배", ["#F4F1EA", "#E05A47", "#8A5A3C", "#5DA9E9"])
    p.rect(48, 10, 52, 70, 3, shade="flat")
    p.poly([(54, 12), (88, 60), (54, 60)], 1, shade="box")
    p.poly([(46, 20), (46, 60), (18, 60)], 2, shade="box")
    p.poly([(10, 66), (90, 66), (78, 84), (22, 84)], 3, shade="box")
    p.arc(30, 100, 14, 5, 200, 340, 4).arc(66, 100, 14, 5, 200, 340, 4)
    out.append(p)

    p = Pic("로켓", ["#F4F1EA", "#E05A47", GLASS, "#F6C85F"])
    p.poly([(38, 62), (22, 82), (38, 80)], 2).poly([(62, 62), (78, 82), (62, 80)], 2)
    p.poly([(44, 82), (56, 82), (50, 98)], 4, shade="flat")
    p.ellipse(50, 48, 16, 36, 1)
    p.poly([(38, 26), (50, 8), (62, 26)], 2, shade="flat")
    p.circle(50, 44, 8, 3)
    p.rect(40, 76, 60, 84, 2, r=2)
    out.append(p)

    p = Pic("기차", ["#5DA9E9", "#E05A47", TIRE, "#F6C85F"])
    p.rect(8, 22, 44, 70, 1, r=4)
    p.rect(14, 30, 38, 44, 4, r=2, shade="flat")
    p.rect(40, 42, 92, 70, 1, r=4)
    p.rect(60, 22, 70, 42, 3, r=2)
    p.rect(56, 16, 74, 22, 2, r=2)
    p.rect(4, 16, 48, 24, 2, r=3)
    p.poly([(92, 56), (98, 72), (86, 72)], 2)
    for x in (20, 42, 64, 84):
        p.circle(x, 76, 8, 3)
    out.append(p)

    p = Pic("열기구", ["#E05A47", "#F6C85F", "#8A5A3C", "#5DA9E9"])
    p.circle(50, 38, 32, 1)
    p.poly([(26, 58), (74, 58), (58, 74), (42, 74)], 1, shade="box")
    p.ellipse(50, 38, 13, 32, 2)
    p.ellipse(50, 38, 4, 32, 4, shade="flat")
    p.line(42, 74, 42, 84, 2.6).line(58, 74, 58, 84, 2.6)
    p.rect(38, 82, 62, 96, 3, r=3)
    out.append(p)

    p = Pic("잠수함", ["#F6C85F", GLASS, "#E8743B", "#5DA9E9"])
    p.rect(50, 20, 58, 34, 3, shade="flat").rect(50, 18, 66, 24, 3, shade="flat")
    p.rect(36, 30, 64, 46, 1, r=6)
    p.ellipse(50, 58, 42, 20, 1)
    for x in (30, 50, 70):
        p.circle(x, 56, 6, 2)
    p.poly([(6, 48), (14, 58), (6, 68)], 3)
    for x, y, r in ((82, 30, 4), (90, 20, 3), (84, 10, 2.4)):
        p.circle(x, y, r, 4, shade="flat")
    out.append(p)

    p = Pic("헬리콥터", ["#81B29A", GLASS, TIRE, "#F6C85F"])
    p.rect(8, 16, 92, 22, 3, r=3)
    p.rect(46, 20, 54, 30, 3, shade="flat")
    p.cap(50, 52, 94, 44, 8, 1)
    p.rect(88, 32, 96, 52, 1, r=3)
    p.ellipse(42, 54, 28, 22, 1)
    p.ellipse(32, 50, 12, 11, 2)
    p.rect(22, 80, 64, 84, 3, shade="flat")
    p.line(30, 74, 30, 82, 3).line(56, 74, 56, 82, 3)
    p.circle(50, 60, 4, 4, shade="flat")
    out.append(p)

    p = Pic("자전거", ["#5DA9E9", TIRE, "#E05A47", "#F6C85F"])
    p.circle(24, 66, 20, 2).circle(76, 66, 20, 2)
    p.circle(24, 66, 14, 0, shade="flat").circle(76, 66, 14, 0, shade="flat")
    p.cap(24, 66, 44, 40, 5, 1).cap(44, 40, 68, 40, 5, 1).cap(68, 40, 76, 66, 5, 1).cap(44, 40, 50, 66, 5, 1).cap(24, 66, 50, 66, 5, 1)
    p.cap(40, 30, 50, 30, 7, 3).cap(44, 30, 44, 40, 4, 1)
    p.cap(66, 26, 68, 40, 4, 1).cap(62, 24, 74, 24, 5, 2)
    p.circle(50, 66, 5, 4)
    out.append(p)
    return out
