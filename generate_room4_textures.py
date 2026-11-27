import os
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont

os.makedirs("Assets/Textures/UI", exist_ok=True)

def make_chamfer_box(draw, xy, chamfer, fill=None, outline=None):
    x0, y0, x1, y1 = xy
    c = chamfer
    pts = [
        (x0 + c, y0), (x1 - c, y0),
        (x1, y0 + c), (x1, y1 - c),
        (x1 - c, y1), (x0 + c, y1),
        (x0, y1 - c), (x0, y0 + c)
    ]
    if fill:
        draw.polygon(pts, fill=fill)
    if outline:
        draw.polygon(pts, outline=outline)

def add_glow_effect(img, blur=6):
    glow = img.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(glow, img)

def generate_header_banner():
    w, h = 1024, 256
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_g = ImageDraw.Draw(glow)
    draw = ImageDraw.Draw(img)

    # Outer Chamfered Glow Frame
    pad = 12
    make_chamfer_box(draw_g, (pad, pad, w - pad, h - pad), 36, outline=(0, 210, 255, 230))
    glow = glow.filter(ImageFilter.GaussianBlur(8))
    img = Image.alpha_composite(glow, img)
    draw = ImageDraw.Draw(img)

    # Dark Navy Glass Background
    make_chamfer_box(draw, (pad, pad, w - pad, h - pad), 36, fill=(4, 14, 30, 220), outline=(0, 240, 255, 255))
    make_chamfer_box(draw, (pad + 4, pad + 4, w - pad - 4, h - pad - 4), 32, outline=(0, 160, 240, 120))

    # Corner Accents
    for cx, cy in [(pad + 36, pad), (w - pad - 36, pad), (pad, pad + 36), (w - pad, pad + 36),
                   (pad + 36, h - pad), (w - pad - 36, h - pad), (pad, h - pad - 36), (w - pad, h - pad - 36)]:
        draw.line([(cx - 8, cy), (cx + 8, cy)], fill=(200, 250, 255, 255), width=2)
        draw.line([(cx, cy - 8), (cx, cy + 8)], fill=(200, 250, 255, 255), width=2)

    img.save("Assets/Textures/UI/Room4_Header_Banner.png")
    print("Saved Assets/Textures/UI/Room4_Header_Banner.png")

def generate_station1_inertia_diagram():
    w, h = 800, 360
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Subtle Background panel
    make_chamfer_box(draw, (8, 8, w - 8, h - 8), 20, fill=(6, 18, 38, 200), outline=(0, 180, 240, 180))

    # Divider
    draw.line([(w // 2, 24), (w // 2, h - 24)], fill=(0, 120, 180, 80), width=2)

    # --- LEFT SIDE: OBSTACLE RAISED (STOPS) ---
    # Track
    track_y = 220
    draw.rectangle([40, track_y, 360, track_y + 8], fill=(0, 190, 255, 240))
    draw.line([40, track_y + 12, 360, track_y + 12], fill=(0, 120, 180, 160), width=2)

    # Raised Obstacle (Gray/Blue Block in front of cart)
    draw.polygon([(260, track_y), (300, track_y), (300, 120), (270, 120), (260, 150)], fill=(80, 105, 135, 240), outline=(130, 175, 220, 255))
    draw.line([(265, 130), (295, 130)], fill=(0, 220, 255, 200), width=2)

    # Cart Chassis (Stopped right at obstacle)
    cart_x = 120
    draw.rectangle([cart_x, 155, cart_x + 130, 195], fill=(0, 95, 175, 230), outline=(0, 230, 255, 255), width=2)
    # Wheels
    draw.ellipse([cart_x + 15, 190, cart_x + 45, 220], fill=(20, 30, 45, 255), outline=(0, 220, 255, 255), width=2)
    draw.ellipse([cart_x + 85, 190, cart_x + 115, 220], fill=(20, 30, 45, 255), outline=(0, 220, 255, 255), width=2)
    # Impact / Stop indicator
    draw.line([(cart_x + 130, 175), (cart_x + 145, 175)], fill=(255, 80, 80, 255), width=3)
    draw.line([(cart_x + 140, 165), (cart_x + 140, 185)], fill=(255, 80, 80, 255), width=3)

    # --- RIGHT SIDE: OBSTACLE LOWERED (CONTINUES) ---
    track2_x = w // 2 + 40
    draw.rectangle([track2_x, track_y, track2_x + 320, track_y + 8], fill=(0, 190, 255, 240))
    draw.line([track2_x, track_y + 12, track2_x + 320, track_y + 12], fill=(0, 120, 180, 160), width=2)

    # Lowered Obstacle (Flat level with track)
    draw.rectangle([track2_x + 220, track_y - 4, track2_x + 260, track_y + 8], fill=(50, 75, 100, 200), outline=(0, 200, 255, 200))

    # Cart moving forward smoothly past obstacle
    cart2_x = track2_x + 110
    draw.rectangle([cart2_x, 155, cart2_x + 130, 195], fill=(0, 95, 175, 230), outline=(0, 230, 255, 255), width=2)
    draw.ellipse([cart2_x + 15, 190, cart2_x + 45, 220], fill=(20, 30, 45, 255), outline=(0, 220, 255, 255), width=2)
    draw.ellipse([cart2_x + 85, 190, cart2_x + 115, 220], fill=(20, 30, 45, 255), outline=(0, 220, 255, 255), width=2)

    # Velocity Vector Arrow (Cyan arrow pointing forward)
    arrow_y = 135
    draw.line([(cart2_x + 30, arrow_y), (cart2_x + 120, arrow_y)], fill=(0, 240, 255, 255), width=3)
    draw.polygon([(cart2_x + 120, arrow_y - 6), (cart2_x + 135, arrow_y), (cart2_x + 120, arrow_y + 6)], fill=(0, 240, 255, 255))
    # Label "v = const"
    draw.line([(cart2_x + 15, 175), (cart2_x - 15, 175)], fill=(0, 220, 255, 180), width=2)

    img.save("Assets/Textures/UI/Room4_Station1_Inertia_Diagram.png")
    print("Saved Assets/Textures/UI/Room4_Station1_Inertia_Diagram.png")

def generate_station3_collision_diagram():
    w, h = 800, 320
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Subtle Background panel
    make_chamfer_box(draw, (8, 8, w - 8, h - 8), 20, fill=(6, 18, 38, 200), outline=(0, 180, 240, 180))

    # Track
    track_y = 220
    draw.rectangle([60, track_y, w - 60, track_y + 8], fill=(0, 190, 255, 240))
    draw.line([60, track_y + 12, w - 60, track_y + 12], fill=(0, 120, 180, 160), width=2)

    # Blue Cart (Left)
    c1_x = 180
    draw.rectangle([c1_x, 150, c1_x + 140, 195], fill=(0, 105, 195, 230), outline=(0, 240, 255, 255), width=2)
    draw.ellipse([c1_x + 20, 190, c1_x + 50, 220], fill=(20, 35, 55, 255), outline=(0, 220, 255, 255), width=2)
    draw.ellipse([c1_x + 90, 190, c1_x + 120, 220], fill=(20, 35, 55, 255), outline=(0, 220, 255, 255), width=2)

    # Red Cart (Right)
    c2_x = 480
    draw.rectangle([c2_x, 150, c2_x + 140, 195], fill=(195, 45, 65, 230), outline=(255, 110, 130, 255), width=2)
    draw.ellipse([c2_x + 20, 190, c2_x + 50, 220], fill=(55, 20, 25, 255), outline=(255, 110, 130, 255), width=2)
    draw.ellipse([c2_x + 90, 190, c2_x + 120, 220], fill=(55, 20, 25, 255), outline=(255, 110, 130, 255), width=2)

    # Equal & Opposite Force Vectors meeting in center
    f_y = 172
    # F1 vector pointing Right (Cyan)
    draw.line([(c1_x + 140, f_y), (385, f_y)], fill=(0, 240, 255, 255), width=4)
    draw.polygon([(385, f_y - 7), (400, f_y), (385, f_y + 7)], fill=(0, 240, 255, 255))

    # F2 vector pointing Left (Red/Coral)
    draw.line([(c2_x, f_y), (415, f_y)], fill=(255, 100, 120, 255), width=4)
    draw.polygon([(415, f_y - 7), (400, f_y), (415, f_y + 7)], fill=(255, 100, 120, 255))

    img.save("Assets/Textures/UI/Room4_Station3_Collision_Diagram.png")
    print("Saved Assets/Textures/UI/Room4_Station3_Collision_Diagram.png")

def generate_room4_thumbnail():
    w, h = 160, 114
    img = Image.new("RGBA", (w, h), (10, 14, 22, 255))
    draw = ImageDraw.Draw(img)

    # Chamber curvature lines
    draw.arc([10, -30, w - 10, h + 50], 0, 180, fill=(0, 180, 255, 180), width=2)
    draw.arc([30, -10, w - 30, h + 30], 0, 180, fill=(0, 140, 220, 120), width=1)

    # Central Hologlobe
    cx, cy = w // 2, h // 2 + 5
    draw.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], outline=(0, 240, 255, 255), width=2)
    draw.ellipse([cx - 24, cy - 8, cx + 24, cy + 8], outline=(0, 180, 255, 180), width=1)
    draw.ellipse([cx - 12, cy - 12, cx + 12, cy + 12], fill=(0, 200, 255, 90))

    # Side Data Screens
    draw.rectangle([12, 25, 42, 65], fill=(5, 25, 55, 220), outline=(0, 210, 255, 240), width=1)
    draw.rectangle([w - 42, 25, w - 12, 65], fill=(5, 25, 55, 220), outline=(0, 210, 255, 240), width=1)

    img.save("Assets/Textures/UI/Room4_Thumbnail.png")
    print("Saved Assets/Textures/UI/Room4_Thumbnail.png")

generate_header_banner()
generate_station1_inertia_diagram()
generate_station3_collision_diagram()
generate_room4_thumbnail()
print("All Room 4 textures generated successfully!")

