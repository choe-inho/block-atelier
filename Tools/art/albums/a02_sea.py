from pixelart import Pic

TITLE = "바다 친구들"


def pics():
    out = []

    p = Pic("열대어", ["#F4A259", "#FFF3E2", "#5DA9E9"])
    p.poly([(70, 50), (92, 28), (92, 72)], 1)
    p.ellipse(44, 50, 34, 24, 1)
    p.poly([(36, 27), (52, 14), (58, 30)], 1, tone=2)
    p.cap(36, 30, 36, 70, 7, 2).cap(56, 29, 56, 71, 7, 2)
    p.dot(22, 46, 3.8)
    out.append(p)

    p = Pic("고래", ["#5DA9E9", "#DDEFFB", "#A8D8F0"])
    p.cap(62, 16, 58, 26, 4, 3).cap(70, 12, 66, 26, 4, 3).cap(78, 16, 72, 26, 4, 3)
    p.poly([(80, 52), (96, 36), (94, 60)], 1)
    p.ellipse(46, 58, 38, 26, 1)
    p.ellipse(42, 70, 30, 12, 2, shade="flat")
    p.dot(24, 54, 3.6)
    p.ellipse(56, 68, 9, 5, 1, rot=-20, tone=2, shade="flat")
    out.append(p)

    p = Pic("문어", ["#E07A8F", "#F7C4CF"])
    for x in (18, 32, 46, 60, 74):
        p.cap(x + 6, 56, x, 88, 9, 1)
    p.ellipse(50, 38, 32, 30, 1)
    for x, y in ((38, 20), (60, 24), (70, 40)):
        p.circle(x, y, 4, 2, shade="flat")
    p.dot(38, 44, 4).dot(62, 44, 4)
    p.ellipse(50, 55, 6, 4, -1, shade="flat")
    out.append(p)

    p = Pic("게", ["#E05A47", "#F7A8B8"])
    p.circle(16, 26, 11, 1).circle(84, 26, 11, 1)
    p.poly([(10, 18), (22, 14), (16, 26)], 0, shade="flat").poly([(90, 18), (78, 14), (84, 26)], 0, shade="flat")
    p.cap(22, 36, 32, 50, 6, 1).cap(78, 36, 68, 50, 6, 1)
    for x in (18, 26, 74, 82):
        p.cap(x, 64, x + (-8 if x < 50 else 8), 78, 5, 1)
    p.ellipse(50, 62, 32, 22, 1)
    p.cap(40, 44, 40, 34, 4, 1).cap(60, 44, 60, 34, 4, 1)
    p.circle(40, 32, 5, 2, shade="flat").circle(60, 32, 5, 2, shade="flat")
    p.dot(40, 32, 2.6).dot(60, 32, 2.6)
    p.arc(50, 60, 12, 3.5, 30, 150, -1)
    out.append(p)

    p = Pic("해파리", ["#B79BE8", "#F1E8FF", "#F7A8B8"])
    for x in (30, 42, 58, 70):
        p.cap(x, 50, x + (4 if x < 50 else -4), 90, 5, 2)
    p.cap(50, 50, 50, 84, 6, 3)
    p.poly([(14, 52), (18, 30), (34, 14), (50, 10), (66, 14), (82, 30), (86, 52)], 1, shade="sphere")
    p.ellipse(50, 52, 36, 7, 1, tone=2, shade="flat")
    p.dot(40, 36, 3.6).dot(60, 36, 3.6)
    p.ellipse(32, 44, 4, 2.6, 3, shade="flat").ellipse(68, 44, 4, 2.6, 3, shade="flat")
    out.append(p)

    p = Pic("불가사리", ["#F6C85F", "#F4A259"])
    import math
    pts = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        r = 44 if i % 2 == 0 else 19
        pts.append((50 + r * math.cos(a), 54 + r * math.sin(a)))
    p.poly(pts, 1, shade="sphere")
    for i in range(5):
        a = math.radians(-90 + i * 72)
        p.circle(50 + 26 * math.cos(a), 54 + 26 * math.sin(a), 3.4, 2, shade="flat")
    p.dot(43, 50, 3.4).dot(57, 50, 3.4)
    p.arc(50, 54, 8, 3, 30, 150, -1)
    out.append(p)

    p = Pic("거북이", ["#81B29A", "#4E8A6E", "#F6D98A"])
    p.ellipse(28, 76, 8, 10, 1, rot=20).ellipse(64, 76, 8, 10, 1, rot=-20)
    p.ellipse(10, 62, 7, 4, 1, rot=-20)
    p.circle(84, 50, 11, 1)
    import math as _m
    dome = [(46 + 36 * _m.cos(_m.radians(a)), 66 - 40 * _m.sin(_m.radians(a))) for a in range(0, 181, 10)]
    p.poly(dome, 2, shade="sphere")
    p.rect(10, 62, 82, 71, 3, r=4, shade="flat")
    for x, y in ((46, 44), (30, 54), (62, 54)):
        p.circle(x, y, 7, 2, shade="flat", tone=0)
    p.dot(88, 47, 3)
    out.append(p)

    p = Pic("조개", ["#F7A8B8", "#FFE3E8", "#F4F1EA"])
    p.rect(38, 70, 62, 84, 1, r=4, shade="flat", tone=2)
    p.poly([(10, 62), (16, 34), (34, 16), (50, 12), (66, 16), (84, 34), (90, 62), (62, 76), (38, 76)], 1, shade="sphere")
    import math
    for a in (-56, -28, 0, 28, 56):
        r = math.radians(a - 90)
        p.cap(50 + 8 * math.cos(r), 72 + 8 * math.sin(r), 50 + 50 * math.cos(r), 72 + 50 * math.sin(r), 3.2, 1, tone=2)
    p.circle(50, 88, 7, 3)
    out.append(p)

    p = Pic("돌고래", ["#7FA7D9", "#E4EEF9"])
    p.poly([(14, 70), (4, 58), (10, 82)], 1)
    p.poly([(44, 26), (58, 10), (62, 30)], 1)
    p.ellipse(52, 50, 38, 20, 1, rot=-25)
    p.ellipse(56, 58, 26, 9, 2, rot=-25, shade="flat")
    p.cap(84, 30, 94, 24, 8, 1)
    p.dot(76, 36, 3.2)
    p.ellipse(52, 66, 8, 5, 1, rot=30, tone=2, shade="flat")
    out.append(p)

    p = Pic("복어", ["#F6C85F", "#FFF3E2", "#E8743B"])
    import math
    for i in range(12):
        a = math.radians(i * 30)
        p.poly([(50 + 30 * math.cos(a - 0.2), 52 + 30 * math.sin(a - 0.2)),
                (50 + 42 * math.cos(a), 52 + 42 * math.sin(a)),
                (50 + 30 * math.cos(a + 0.2), 52 + 30 * math.sin(a + 0.2))], 3, shade="flat")
    p.circle(50, 52, 33, 1)
    p.ellipse(50, 64, 22, 14, 2, shade="flat")
    p.dot(38, 46, 4.2).dot(62, 46, 4.2)
    p.circle(50, 60, 4.5, 3, shade="flat")
    out.append(p)
    return out
