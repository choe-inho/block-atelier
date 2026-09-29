"""앨범 10개(그림 100장)를 LevelTool 입력 JSON으로 내보낸다. 사용: python3 export.py ../LevelTool/pictures.json"""
import importlib, json, sys

ALBUMS = [("a01_animals", "animals"), ("a02_sea", "sea"), ("a03_fruit", "fruit"), ("a04_dessert", "dessert"),
          ("a05_plants", "plants"), ("a06_vehicles", "vehicles"), ("a07_sky", "sky"), ("a08_things", "things"),
          ("a09_space", "space"), ("a10_korea", "korea")]

out = []
for mod_name, key in ALBUMS:
    mod = importlib.import_module("albums." + mod_name)
    pics = mod.pics()
    assert len(pics) == 10, (mod_name, len(pics))
    for p in pics:
        rows, shades = p.render()
        used = sorted({int(ch) for r in rows for ch in r if ch not in "0L"})
        # 안 쓰인 색은 빼고 번호를 다시 매긴다
        remap = {c: i + 1 for i, c in enumerate(used)}
        rows = ["".join(ch if ch in "0L" else str(remap[int(ch)]) for ch in r) for r in rows]
        out.append({"name": p.name, "album": key, "albumTitle": mod.TITLE, "lineColor": "#2B2838",
                    "palette": ["transparent"] + [p.colors[c - 1] for c in used], "rows": rows, "shades": shades})
json.dump(out, open(sys.argv[1], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(len(out), "pictures")
