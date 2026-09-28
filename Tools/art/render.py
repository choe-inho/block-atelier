import sys, json
from PIL import Image, ImageDraw, ImageFont
def hexc(h): return tuple(int(h[i:i+2],16) for i in (1,3,5))
def render(rows, palette, cell=24, bg=(250,247,240)):
    h=len(rows); w=len(rows[0])
    im=Image.new("RGB",(w*cell,h*cell),bg); d=ImageDraw.Draw(im)
    for y,r in enumerate(rows):
        for x,ch in enumerate(r):
            if ch in ".0": 
                if (x+y)%2==0: d.rectangle([x*cell,y*cell,x*cell+cell-1,y*cell+cell-1],fill=(238,234,226))
                continue
            d.rectangle([x*cell,y*cell,x*cell+cell-1,y*cell+cell-1],fill=hexc(palette[int(ch)-1]))
    return im
def sheet(items, path, cols=5, cell=24):
    font=ImageFont.truetype("/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc",18) if False else None
    ims=[render(p["rows"],p["palette"],cell) for p in items]
    W=ims[0].width; H=ims[0].height+34
    rowsn=(len(ims)+cols-1)//cols
    out=Image.new("RGB",(cols*(W+16)+16,rowsn*(H+16)+16),(255,255,255))
    d=ImageDraw.Draw(out)
    for i,im in enumerate(ims):
        x=16+(i%cols)*(W+16); y=16+(i//cols)*(H+16)
        out.paste(im,(x,y)); d.text((x,y+im.height+6),f"L{i+1}",fill=(60,60,60))
    out.save(path)
if __name__=="__main__":
    from pictures import PICTURES
    sheet(PICTURES, sys.argv[1] if len(sys.argv)>1 else "sheet.png")
