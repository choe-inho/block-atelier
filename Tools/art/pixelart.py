"""
벡터 도형으로 그린 그림을 픽셀 아트로 바꾸는 도구.

- 좌표는 0~100 캔버스 (y는 아래로), 해상도와 무관하게 그린다.
- 색 번호 1~5는 게임에서 칠하는 색, 'L'은 처음부터 그려진 윤곽선(색칠 공부의 선).
- 명암(밝음/기본/어두움)은 모양마다 빛 방향(왼쪽 위)으로 자동 계산한다. 명암은 같은 색으로 칠한다.
- 바깥 윤곽선은 자동으로 1픽셀 두른다.

출력: rows(색 번호 또는 'L'), shades(0/1/2) 문자열 목록.
"""
import math

LINE = -1


class Shape:
    def __init__(self, kind, params, color, shade="sphere", tone=None):
        self.kind = kind
        self.p = params
        self.color = color
        self.shade = shade
        self.tone = tone

    # ---- 포함 판정 ----
    def contains(self, x, y):
        k, p = self.kind, self.p
        if k == "ellipse":
            cx, cy, rx, ry, rot = p
            dx, dy = x - cx, y - cy
            if rot:
                c, s = math.cos(-rot), math.sin(-rot)
                dx, dy = dx * c - dy * s, dx * s + dy * c
            return (dx / rx) ** 2 + (dy / ry) ** 2 <= 1.0
        if k == "rect":
            x0, y0, x1, y1, r = p
            if not (x0 <= x <= x1 and y0 <= y <= y1):
                return False
            if r <= 0:
                return True
            cx = min(max(x, x0 + r), x1 - r)
            cy = min(max(y, y0 + r), y1 - r)
            return (x - cx) ** 2 + (y - cy) ** 2 <= r * r
        if k == "poly":
            pts = p[0]
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]
                xj, yj = pts[j]
                if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            return inside
        if k == "capsule":
            x0, y0, x1, y1, w = p
            vx, vy = x1 - x0, y1 - y0
            L2 = vx * vx + vy * vy
            t = 0 if L2 == 0 else max(0, min(1, ((x - x0) * vx + (y - y0) * vy) / L2))
            px, py = x0 + t * vx, y0 + t * vy
            return (x - px) ** 2 + (y - py) ** 2 <= (w / 2) ** 2
        if k == "arc":  # 두꺼운 호 (웃는 입, 무지개)
            cx, cy, r, w, a0, a1 = p
            d = math.hypot(x - cx, y - cy)
            if abs(d - r) > w / 2:
                return False
            a = math.degrees(math.atan2(y - cy, x - cx)) % 360
            a0 %= 360
            a1 %= 360
            return (a0 <= a <= a1) if a0 <= a1 else (a >= a0 or a <= a1)
        raise ValueError(k)

    def bounds(self):
        k, p = self.kind, self.p
        if k == "ellipse":
            cx, cy, rx, ry, _ = p
            r = max(rx, ry)
            return cx - r, cy - r, cx + r, cy + r
        if k == "rect":
            return p[0], p[1], p[2], p[3]
        if k == "poly":
            xs = [a for a, _ in p[0]]
            ys = [b for _, b in p[0]]
            return min(xs), min(ys), max(xs), max(ys)
        if k == "capsule":
            x0, y0, x1, y1, w = p
            return min(x0, x1) - w / 2, min(y0, y1) - w / 2, max(x0, x1) + w / 2, max(y0, y1) + w / 2
        if k == "arc":
            cx, cy, r, w, _, _ = p
            return cx - r - w, cy - r - w, cx + r + w, cy + r + w

    # ---- 명암 ----
    def tone_at(self, x, y):
        if self.tone is not None:
            return self.tone
        if self.shade == "flat":
            return 1
        x0, y0, x1, y1 = self.bounds()
        w, h = max(x1 - x0, 1e-6), max(y1 - y0, 1e-6)
        if self.kind == "ellipse" or self.shade == "sphere":
            if self.kind == "ellipse":
                cx, cy, rx, ry, _ = self.p
            else:
                cx, cy, rx, ry = (x0 + x1) / 2, (y0 + y1) / 2, w / 2, h / 2
            nx, ny = (x - cx) / rx, (y - cy) / ry
            light = -(nx * 0.62 + ny * 0.78)
            if light > 0.5:
                return 0
            if light < -0.66:
                return 2
            return 1
        # box: 위·왼쪽 가장자리 밝게, 아래·오른쪽 어둡게
        u, v = (x - x0) / w, (y - y0) / h
        if self.shade == "top":
            return 0 if v < 0.25 else (2 if v > 0.8 else 1)
        t = u * 0.4 + v * 0.6
        return 0 if t < 0.16 else (2 if t > 0.88 else 1)


class Pic:
    def __init__(self, name, colors, size=24, outline=True):
        self.name = name
        self.colors = colors        # 게임 색 hex 목록 (번호 1부터)
        self.size = size
        self.shapes = []
        self.outline = outline

    # ---- 그리기 도구 (좌표 0~100) ----
    def ellipse(self, cx, cy, rx, ry, c, shade="sphere", rot=0.0, tone=None):
        self.shapes.append(Shape("ellipse", (cx, cy, rx, ry, math.radians(rot)), c, shade, tone)); return self

    def circle(self, cx, cy, r, c, shade="sphere", tone=None):
        return self.ellipse(cx, cy, r, r, c, shade, 0.0, tone)

    def rect(self, x0, y0, x1, y1, c, r=0, shade="box", tone=None):
        self.shapes.append(Shape("rect", (x0, y0, x1, y1, r), c, shade, tone)); return self

    def poly(self, pts, c, shade="box", tone=None):
        self.shapes.append(Shape("poly", (pts,), c, shade, tone)); return self

    def cap(self, x0, y0, x1, y1, w, c, shade="flat", tone=None):
        self.shapes.append(Shape("capsule", (x0, y0, x1, y1, w), c, shade, tone)); return self

    def arc(self, cx, cy, r, w, a0, a1, c, shade="flat", tone=None):
        self.shapes.append(Shape("arc", (cx, cy, r, w, a0, a1), c, shade, tone)); return self

    def line(self, x0, y0, x1, y1, w=4.5):
        return self.cap(x0, y0, x1, y1, w, LINE)

    def dot(self, x, y, r=3.2):
        return self.circle(x, y, r, LINE, "flat")

    def erase_ellipse(self, cx, cy, rx, ry):
        return self.ellipse(cx, cy, rx, ry, 0, "flat")

    def erase_rect(self, x0, y0, x1, y1):
        return self.rect(x0, y0, x1, y1, 0, 0, "flat")

    # ---- 픽셀로 ----
    def render(self, ss=5):
        n = self.size
        cell = 100.0 / n
        color = [[0] * n for _ in range(n)]
        tone = [[1] * n for _ in range(n)]
        for py in range(n):
            for px in range(n):
                votes = {}
                line_hits = 0
                total = ss * ss
                tone_shape = {}
                for sy in range(ss):
                    for sx in range(ss):
                        x = (px + (sx + 0.5) / ss) * cell
                        y = (py + (sy + 0.5) / ss) * cell
                        hit = None
                        for sh in reversed(self.shapes):
                            if sh.contains(x, y):
                                hit = sh
                                break
                        if hit is None:
                            votes[0] = votes.get(0, 0) + 1
                            continue
                        if hit.color == LINE:
                            line_hits += 1
                            continue
                        votes[hit.color] = votes.get(hit.color, 0) + 1
                        tone_shape.setdefault(hit.color, hit)
                if line_hits >= total * 0.3:
                    color[py][px] = LINE
                    continue
                best = max(votes.items(), key=lambda kv: kv[1])[0] if votes else 0
                # 칸의 절반 이상이 비어 있으면 빈칸
                if votes.get(0, 0) > total * 0.5:
                    best = 0
                color[py][px] = best
                if best > 0:
                    cx, cy = (px + 0.5) * cell, (py + 0.5) * cell
                    # 가운데를 덮는 도형의 명암
                    sh = None
                    for s in reversed(self.shapes):
                        if s.color == best and s.contains(cx, cy):
                            sh = s
                            break
                    sh = sh or tone_shape[best]
                    tone[py][px] = sh.tone_at(cx, cy)
        if self.outline:
            add = []
            for y in range(n):
                for x in range(n):
                    if color[y][x] != 0:
                        continue
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        xx, yy = x + dx, y + dy
                        if 0 <= xx < n and 0 <= yy < n and color[yy][xx] > 0:
                            add.append((x, y))
                            break
            for x, y in add:
                color[y][x] = LINE
        rows = ["".join("L" if v == LINE else str(v) for v in r) for r in color]
        shades = ["".join(str(tone[y][x]) if color[y][x] > 0 else "1" for x in range(n)) for y in range(n)]
        return rows, shades


def hexrgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


LINE_RGB = hexrgb("#2B2838")


def shade_rgb(base_hex, tone):
    """게임(Unity)과 같은 명암 공식"""
    b = hexrgb(base_hex)
    if tone == 0:
        return lerp(b, (255, 255, 255), 0.32)
    if tone == 2:
        return lerp(b, LINE_RGB, 0.3)
    return b
