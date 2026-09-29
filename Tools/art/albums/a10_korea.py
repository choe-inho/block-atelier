import math
from pixelart import Pic

TITLE = "한국의 멋"


def pics():
    out = []

    p = Pic("복주머니", ["#E05A77", "#F6C85F", "#5DA9E9", "#81B29A"])
    p.ellipse(50, 66, 36, 30, 1)
    p.poly([(28, 44), (72, 44), (64, 24), (36, 24)], 1, shade="box")
    p.rect(30, 38, 70, 46, 2, r=3, shade="flat")
    p.circle(50, 42, 6, 2)
    p.cap(46, 46, 40, 62, 4, 3).cap(54, 46, 60, 62, 4, 3)
    p.circle(50, 70, 11, 2)
    p.circle(50, 70, 5, 4, shade="flat")
    out.append(p)

    p = Pic("송편", ["#F4F1EA", "#F7A8B8", "#81B29A", "#8A5A3C"])
    p.ellipse(30, 70, 22, 15, 1, rot=10)
    p.ellipse(72, 70, 22, 15, 2, rot=-10)
    p.ellipse(50, 44, 22, 15, 3)
    p.arc(50, 34, 16, 3.5, 30, 150, 3, tone=0)
    p.cap(80, 30, 94, 18, 6, 4).ellipse(88, 28, 9, 5, 3, rot=30)
    out.append(p)

    p = Pic("부채", ["#E05A47", "#F6C85F", "#5DA9E9", "#8A5A3C"])
    for i in range(9):
        a0, a1 = 196 + i * 16, 196 + (i + 1) * 16
        pts = [(50, 80)]
        for a in (a0, (a0 + a1) / 2, a1):
            pts.append((50 + 46 * math.cos(math.radians(a)), 80 + 46 * math.sin(math.radians(a))))
        p.poly(pts, (1, 2, 3)[i % 3], shade="sphere")
    p.ellipse(50, 80, 14, 14, 0, shade="flat")
    p.rect(46, 70, 54, 96, 4, r=2)
    p.circle(50, 72, 5, 2)
    out.append(p)

    p = Pic("방패연", ["#F4F1EA", "#E05A47", "#5DA9E9", "#F6C85F"])
    p.rect(20, 8, 80, 72, 1, r=2)
    p.circle(50, 40, 11, 0, shade="flat")
    p.poly([(20, 8), (80, 8), (80, 18), (20, 18)], 2, shade="flat")
    p.rect(20, 8, 26, 72, 3, shade="flat").rect(74, 8, 80, 72, 3, shade="flat")
    p.line(20, 8, 80, 72, 2.2).line(80, 8, 20, 72, 2.2)
    p.cap(50, 72, 40, 84, 4, 4).cap(40, 84, 56, 96, 4, 4)
    out.append(p)

    p = Pic("까치", ["#474556", "#F4F1EA", "#4A7FC1", "#F6C85F"])
    p.poly([(70, 60), (98, 86), (86, 92), (64, 70)], 3, shade="flat")
    p.ellipse(48, 58, 28, 20, 1, rot=10)
    p.ellipse(44, 64, 18, 11, 2, rot=10)
    p.ellipse(60, 50, 18, 10, 3, rot=20)
    p.circle(26, 36, 15, 1)
    p.poly([(12, 32), (2, 38), (12, 40)], 4, shade="flat")
    p.circle(24, 32, 3, 2, shade="flat")
    p.line(40, 78, 38, 92, 3).line(54, 78, 54, 92, 3)
    out.append(p)

    p = Pic("호랑이", ["#F28C38", "#F4F1EA", "#474556", "#F7A8B8"])
    p.circle(22, 20, 12, 1).circle(78, 20, 12, 1)
    p.circle(22, 22, 6, 4, shade="flat").circle(78, 22, 6, 4, shade="flat")
    p.ellipse(50, 54, 42, 36, 1)
    p.ellipse(50, 70, 22, 16, 2)
    p.poly([(44, 20), (56, 20), (50, 34)], 3, shade="flat")
    for s in (-1, 1):
        p.poly([(50 + s * 42, 48), (50 + s * 30, 52), (50 + s * 42, 56)], 3, shade="flat")
        p.poly([(50 + s * 40, 64), (50 + s * 30, 66), (50 + s * 38, 70)], 3, shade="flat")
    p.dot(35, 50, 3.5).dot(65, 50, 3.5)
    p.ellipse(50, 64, 6, 4, 4, shade="flat")
    p.arc(44, 70, 6, 3, 20, 160, 3).arc(56, 70, 6, 3, 20, 160, 3)
    out.append(p)

    p = Pic("한옥", ["#474556", "#F2D6B3", "#8A5A3C", "#E05A47"])
    p.poly([(2, 44), (14, 26), (86, 26), (98, 44), (86, 40), (14, 40)], 1, shade="top")
    p.poly([(2, 44), (8, 36), (14, 40)], 1, shade="flat")
    p.rect(14, 44, 86, 84, 2, shade="box")
    p.rect(14, 44, 86, 48, 4, shade="flat")
    for x in (14, 48, 82):
        p.rect(x, 44, x + 4, 84, 3, shade="flat")
    p.rect(24, 54, 42, 76, 2, shade="flat", tone=0).rect(58, 54, 76, 76, 2, shade="flat", tone=0)
    p.line(33, 54, 33, 76, 2).line(24, 65, 42, 65, 2).line(67, 54, 67, 76, 2).line(58, 65, 76, 65, 2)
    p.rect(8, 84, 92, 94, 3, r=2)
    out.append(p)

    p = Pic("남산타워", ["#F4F1EA", "#E05A47", "#81B29A", "#5DA9E9"])
    p.cap(50, 2, 50, 24, 3, 2)
    p.rect(44, 24, 56, 34, 1)
    p.ellipse(50, 38, 18, 8, 4)
    p.rect(46, 44, 54, 76, 1, shade="flat")
    p.rect(40, 48, 60, 54, 2, r=2, shade="flat")
    p.circle(20, 96, 26, 3).circle(80, 96, 26, 3).circle(50, 90, 26, 3)
    out.append(p)

    p = Pic("무궁화", ["#E88FC0", "#C23B6E", "#F6C85F", "#81B29A"])
    p.ellipse(22, 80, 14, 7, 4, rot=-30).ellipse(80, 82, 14, 7, 4, rot=30)
    for i in range(5):
        a = math.radians(-90 + i * 72)
        p.ellipse(50 + 23 * math.cos(a), 50 + 23 * math.sin(a), 21, 17, 1, rot=math.degrees(a))
    p.circle(50, 50, 13, 2)
    for i in range(5):
        a = math.radians(-90 + i * 72 + 36)
        p.cap(50, 50, 50 + 18 * math.cos(a), 50 + 18 * math.sin(a), 3.5, 2, tone=2)
    p.cap(50, 50, 56, 36, 4, 3).circle(57, 34, 3.5, 3)
    out.append(p)

    p = Pic("태극무늬", ["#E05A47", "#3D6FB6", "#F4F1EA"])
    R = 44
    p.circle(50, 50, R + 4, 3, shade="flat")
    p.poly([(50 + R * math.cos(math.radians(a)), 50 + R * math.sin(math.radians(a))) for a in range(180, 361, 10)], 1)
    p.poly([(50 + R * math.cos(math.radians(a)), 50 + R * math.sin(math.radians(a))) for a in range(0, 181, 10)], 2)
    p.circle(50 - R / 2, 50, R / 2, 1, shade="flat", tone=1)
    p.circle(50 + R / 2, 50, R / 2, 2, shade="flat", tone=1)
    out.append(p)
    return out
