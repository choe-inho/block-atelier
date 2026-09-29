import math
from pixelart import Pic

TITLE = "생활 소품"


def pics():
    out = []

    p = Pic("머그컵", ["#E07A5F", "#F4F1EA", "#8A5A3C", "#F7A8B8"])
    p.arc(78, 54, 13, 7, 270, 90, 1)
    p.rect(18, 28, 76, 88, 1, r=8)
    p.ellipse(47, 30, 27, 6, 3, shade="flat")
    p.circle(47, 60, 12, 4, shade="flat")
    p.cap(36, 6, 34, 18, 4, 2).cap(52, 4, 50, 18, 4, 2)
    out.append(p)

    p = Pic("전구", ["#F6C85F", "#9AA3B5", "#F28C38"])
    p.rect(38, 64, 62, 88, 2, r=4)
    p.circle(50, 40, 30, 1)
    p.poly([(32, 58), (68, 58), (62, 70), (38, 70)], 1)
    p.line(38, 74, 62, 74, 3.5).line(38, 82, 62, 82, 3.5)
    p.cap(44, 44, 50, 34, 4, 3).cap(50, 34, 56, 44, 4, 3)
    out.append(p)

    p = Pic("시계", ["#F4F1EA", "#E05A47", "#F6C85F"])
    p.circle(24, 18, 12, 2).circle(76, 18, 12, 2)
    p.cap(26, 86, 20, 96, 7, 2).cap(74, 86, 80, 96, 7, 2)
    p.circle(50, 54, 40, 2)
    p.circle(50, 54, 31, 1)
    for i in range(12):
        a = math.radians(i * 30)
        p.circle(50 + 25 * math.cos(a), 54 + 25 * math.sin(a), 2.6 if i % 3 else 3.8, 3, shade="flat")
    p.line(50, 54, 50, 34).line(50, 54, 64, 60)
    out.append(p)

    p = Pic("선물상자", ["#5DA9E9", "#E05A47", "#F6C85F"])
    p.rect(12, 44, 88, 94, 1, r=3)
    p.rect(8, 32, 92, 48, 1, r=3)
    p.rect(44, 32, 56, 94, 2, shade="flat")
    p.ellipse(34, 22, 15, 10, 2, rot=20).ellipse(66, 22, 15, 10, 2, rot=-20)
    p.circle(50, 28, 7, 2)
    for x, y in ((24, 66), (74, 78), (28, 84), (72, 58)):
        p.circle(x, y, 4, 3, shade="flat")
    out.append(p)

    p = Pic("하트", ["#E05A77", "#F7A8B8"])
    p.circle(32, 36, 24, 1).circle(68, 36, 24, 1)
    p.poly([(10, 44), (90, 44), (50, 92)], 1, shade="sphere")
    p.ellipse(28, 30, 7, 5, 2, shade="flat", rot=-30)
    out.append(p)

    p = Pic("왕관", ["#F6C85F", "#E05A47", "#5DA9E9", "#81B29A"])
    p.poly([(10, 30), (30, 54), (50, 20), (70, 54), (90, 30), (84, 80), (16, 80)], 1)
    p.rect(14, 72, 86, 86, 1, r=3, shade="top")
    p.circle(10, 28, 6, 2).circle(50, 16, 7, 3).circle(90, 28, 6, 2)
    p.circle(32, 79, 4, 4, shade="flat").circle(50, 79, 5, 2, shade="flat").circle(68, 79, 4, 4, shade="flat")
    out.append(p)

    p = Pic("열쇠", ["#F6C85F", "#81B29A"])
    p.circle(28, 36, 22, 1)
    p.erase_ellipse(28, 36, 9, 9)
    p.rect(44, 32, 92, 42, 1, shade="flat")
    p.rect(70, 40, 78, 56, 1, shade="flat").rect(84, 40, 92, 52, 1, shade="flat")
    p.cap(14, 62, 22, 82, 6, 2).cap(22, 82, 36, 88, 6, 2)
    out.append(p)

    p = Pic("음표", ["#9B7FD4", "#F7A8B8"])
    p.ellipse(28, 76, 16, 12, 1, rot=-20).ellipse(74, 68, 16, 12, 1, rot=-20)
    p.rect(38, 18, 45, 76, 1, shade="flat").rect(84, 10, 91, 68, 1, shade="flat")
    p.poly([(38, 16), (91, 6), (91, 22), (38, 32)], 1, shade="flat")
    p.circle(14, 26, 5, 2).circle(62, 44, 4, 2)
    out.append(p)

    p = Pic("연필", ["#F6C85F", "#F7A8B8", "#F2D6B3", "#9AA3B5"])
    ang = math.radians(-45)
    def pt(t, o):
        return (50 + t * math.cos(ang) - o * math.sin(ang), 50 + t * math.sin(ang) + o * math.cos(ang))
    p.poly([pt(-26, -11), pt(30, -11), pt(30, 11), pt(-26, 11)], 1)
    p.poly([pt(30, -11), pt(38, -11), pt(38, 11), pt(30, 11)], 4, shade="flat")
    p.poly([pt(38, -11), pt(48, -11), pt(48, 11), pt(38, 11)], 2)
    p.poly([pt(-26, -11), pt(-26, 11), pt(-44, 0)], 3)
    p.poly([pt(-38, -3.5), pt(-38, 3.5), pt(-46, 0)], -1, shade="flat")
    p.line(*pt(-24, 0), *pt(28, 0), 3)
    out.append(p)

    p = Pic("책", ["#5DA9E9", "#F4F1EA", "#E05A47", "#F6C85F"])
    p.rect(14, 12, 86, 88, 1, r=4)
    p.rect(20, 80, 86, 92, 2, shade="flat")
    p.rect(14, 12, 24, 88, 1, shade="flat", tone=2)
    p.rect(34, 24, 76, 44, 2, r=3, shade="flat")
    p.poly([(62, 12), (72, 12), (72, 38), (67, 32), (62, 38)], 3, shade="flat")
    p.poly([(52, 58), (56, 66), (64, 67), (58, 72), (60, 80), (52, 76), (44, 80), (46, 72), (40, 67), (48, 66)], 4, shade="flat")
    out.append(p)
    return out
