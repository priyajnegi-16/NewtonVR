import math
from PIL import Image, ImageDraw, ImageFilter

def create_glow(image, blur_radius=8):
    glow = image.filter(ImageFilter.GaussianBlur(blur_radius))
    return Image.alpha_composite(glow, image)

def draw_sphere(draw, cx, cy, r, base_col, mid_col, high_col):
    for i in range(r, 0, -1):
        ratio = i / r
        if ratio > 0.45:
            t = (ratio - 0.45) / 0.55
            col = tuple(int(mid_col[k] * (1-t) + base_col[k] * t) for k in range(4))
        else:
            t = ratio / 0.45
            col = tuple(int(high_col[k] * (1-t) + mid_col[k] * t) for k in range(4))
        ox = cx + int((1.0 - ratio) * (r * 0.38))
        oy = cy - int((1.0 - ratio) * (r * 0.38))
        draw.ellipse([ox - i, oy - i, ox + i, oy + i], fill=col)

def generate_inertia_icon(path):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    cx, cy = 340, 256
    # Speed trails trailing to the left
    trail_specs = [
        (-48, 220, 10, (0, 170, 255, 160)),
        (-26, 280, 16, (0, 210, 255, 200)),
        (-6, 320, 22, (0, 240, 255, 240)),
        (14, 300, 18, (0, 220, 255, 210)),
        (34, 250, 12, (0, 190, 255, 170)),
        (52, 190, 8, (0, 150, 255, 120))
    ]
    for dy, length, w, col in trail_specs:
        start_x = cx - 20
        end_x = start_x - length
        y = cy + dy
        pts = [(start_x, y - w // 2), (end_x, y), (start_x, y + w // 2)]
        draw_glow.polygon(pts, fill=col)
        # Inner bright white streak
        pts_core = [(start_x, y - w // 4), (end_x + 60, y), (start_x, y + w // 4)]
        draw.polygon(pts_core, fill=(210, 250, 255, 230))

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(12))
    img = Image.alpha_composite(glow_layer, img)
    draw = ImageDraw.Draw(img)

    # Main Glowing Sphere
    draw_sphere(
        draw, cx, cy, 96,
        base_col=(0, 65, 160, 255),
        mid_col=(0, 190, 255, 255),
        high_col=(240, 253, 255, 255)
    )

    # Outer Sphere Rim Glow
    for i in range(1, 20):
        alpha = int(160 * (1.0 - i / 20))
        draw.ellipse([cx - 96 - i, cy - 96 - i, cx + 96 + i, cy + 96 + i], outline=(0, 245, 255, alpha), width=2)

    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_force_icon(path):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    cx, cy = 185, 256
    s = 78

    # Isometric Cube
    p_top = (cx, cy - int(s * 1.15))
    p_top_r = (cx + int(s * 1.0), cy - int(s * 0.58))
    p_top_l = (cx - int(s * 1.0), cy - int(s * 0.58))
    p_center = (cx, cy)
    p_bot = (cx, cy + int(s * 1.15))
    p_bot_r = (cx + int(s * 1.0), cy + int(s * 0.58))
    p_bot_l = (cx - int(s * 1.0), cy + int(s * 0.58))

    draw.polygon([p_top, p_top_r, p_center, p_top_l], fill=(120, 230, 255, 250))
    draw.polygon([p_top_l, p_center, p_bot, p_bot_l], fill=(0, 170, 255, 250))
    draw.polygon([p_top_r, p_center, p_bot, p_bot_r], fill=(0, 110, 210, 250))

    edges = [
        (p_top, p_top_r), (p_top_r, p_center), (p_center, p_top_l), (p_top_l, p_top),
        (p_center, p_bot), (p_top_l, p_bot_l), (p_bot_l, p_bot),
        (p_top_r, p_bot_r), (p_bot_r, p_bot)
    ]
    for p1, p2 in edges:
        draw.line([p1, p2], fill=(225, 252, 255, 255), width=4)

    # Force Arrow
    ay = cy - 6
    arrow_pts = [
        (280, ay - 24),
        (370, ay - 24),
        (370, ay - 52),
        (465, ay),
        (370, ay + 52),
        (370, ay + 24),
        (280, ay + 24)
    ]
    draw_glow.polygon(arrow_pts, fill=(0, 235, 255, 255))
    draw.polygon(arrow_pts, fill=(0, 235, 255, 250))
    draw.line(arrow_pts + [arrow_pts[0]], fill=(240, 255, 255, 255), width=4)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(10))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_action_reaction_icon(path):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    r = 75
    cy = 256
    cx1, cx2 = 125, 387

    draw_sphere(draw, cx1, cy, r, (0, 70, 165, 255), (0, 185, 255, 255), (240, 253, 255, 255))
    draw_sphere(draw, cx2, cy, r, (0, 70, 165, 255), (0, 185, 255, 255), (240, 253, 255, 255))

    for i in range(1, 16):
        alpha = int(140 * (1.0 - i / 16))
        draw.ellipse([cx1 - r - i, cy - r - i, cx1 + r + i, cy + r + i], outline=(0, 240, 255, alpha), width=2)
        draw.ellipse([cx2 - r - i, cy - r - i, cx2 + r + i, cy + r + i], outline=(0, 240, 255, alpha), width=2)

    # Top arrow: points RIGHT --->
    ty = cy - 28
    top_arrow = [
        (210, ty - 14),
        (270, ty - 14),
        (270, ty - 30),
        (320, ty),
        (270, ty + 30),
        (270, ty + 14),
        (210, ty + 14)
    ]
    draw_glow.polygon(top_arrow, fill=(0, 235, 255, 255))
    draw.polygon(top_arrow, fill=(0, 235, 255, 250))
    draw.line(top_arrow + [top_arrow[0]], fill=(240, 255, 255, 255), width=3)

    # Bottom arrow: points LEFT <---
    by = cy + 28
    bot_arrow = [
        (302, by - 14),
        (242, by - 14),
        (242, by - 30),
        (192, by),
        (242, by + 30),
        (242, by + 14),
        (302, by + 14)
    ]
    draw_glow.polygon(bot_arrow, fill=(0, 235, 255, 255))
    draw.polygon(bot_arrow, fill=(0, 235, 255, 250))
    draw.line(bot_arrow + [bot_arrow[0]], fill=(240, 255, 255, 255), width=3)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(10))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_scifi_main_frame(path):
    w, h = 1024, 720
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    pad = 20
    cut = 56

    pts = [
        (pad + cut, pad),
        (w - pad - cut, pad),
        (w - pad, pad + cut),
        (w - pad, h - pad - cut),
        (w - pad - cut, h - pad),
        (pad + cut, h - pad),
        (pad, h - pad - cut),
        (pad, pad + cut)
    ]

    # Translucent deep navy interior fill
    draw.polygon(pts, fill=(6, 14, 26, 238))

    # Multi-layered glowing cyan outline
    for glow_w, glow_alpha in [(24, 25), (18, 50), (12, 100), (6, 180), (3, 255)]:
        draw_glow.polygon(pts, outline=(0, 229, 255, glow_alpha), width=glow_w)

    draw.polygon(pts, outline=(0, 240, 255, 255), width=4)

    # Inner secondary fine accent border
    pad_in = pad + 10
    cut_in = cut - 4
    pts_in = [
        (pad_in + cut_in, pad_in),
        (w - pad_in - cut_in, pad_in),
        (w - pad_in, pad_in + cut_in),
        (w - pad_in, h - pad_in - cut_in),
        (w - pad_in - cut_in, h - pad_in),
        (pad_in + cut_in, h - pad_in),
        (pad_in, h - pad_in - cut_in),
        (pad_in, pad_in + cut_in)
    ]
    draw.polygon(pts_in, outline=(0, 180, 240, 130), width=2)

    # Prominent Futuristic Corner Brackets
    bracket_len = 100
    b_col = (180, 245, 255, 255)
    b_glow = (0, 240, 255, 255)

    def draw_bracket(p_corner_start, p_corner, p_corner_end):
        draw_glow.line([p_corner_start, p_corner, p_corner_end], fill=b_glow, width=10)
        draw.line([p_corner_start, p_corner, p_corner_end], fill=b_col, width=5)

    # Top-Left Bracket
    draw_bracket((pad - 4, pad + cut + bracket_len), (pad - 4, pad + cut), (pad + cut, pad - 4))
    draw_bracket((pad - 4, pad + cut), (pad + cut, pad - 4), (pad + cut + bracket_len, pad - 4))

    # Top-Right Bracket
    draw_bracket((w - pad + 4, pad + cut + bracket_len), (w - pad + 4, pad + cut), (w - pad - cut, pad - 4))
    draw_bracket((w - pad + 4, pad + cut), (w - pad - cut, pad - 4), (w - pad - cut - bracket_len, pad - 4))

    # Bottom-Left Bracket
    draw_bracket((pad - 4, h - pad - cut - bracket_len), (pad - 4, h - pad - cut), (pad + cut, h - pad + 4))
    draw_bracket((pad - 4, h - pad - cut), (pad + cut, h - pad + 4), (pad + cut + bracket_len, h - pad + 4))

    # Bottom-Right Bracket
    draw_bracket((w - pad + 4, h - pad - cut - bracket_len), (w - pad + 4, h - pad - cut), (w - pad - cut, h - pad + 4))
    draw_bracket((w - pad + 4, h - pad - cut), (w - pad - cut, h - pad + 4), (w - pad - cut - bracket_len, h - pad + 4))

    # Cybernetic Side Notches (Left & Right)
    notch_h = 44
    draw_glow.polygon([(pad, h//2 - notch_h), (pad - 16, h//2 - notch_h//2), (pad - 16, h//2 + notch_h//2), (pad, h//2 + notch_h)], fill=(0, 220, 255, 120), outline=(0, 240, 255, 255))
    draw.line([(pad, h//2 - notch_h), (pad - 16, h//2 - notch_h//2), (pad - 16, h//2 + notch_h//2), (pad, h//2 + notch_h)], fill=(200, 250, 255, 255), width=4)

    draw_glow.polygon([(w - pad, h//2 - notch_h), (w - pad + 16, h//2 - notch_h//2), (w - pad + 16, h//2 + notch_h//2), (w - pad, h//2 + notch_h)], fill=(0, 220, 255, 120), outline=(0, 240, 255, 255))
    draw.line([(w - pad, h//2 - notch_h), (w - pad + 16, h//2 - notch_h//2), (w - pad + 16, h//2 + notch_h//2), (w - pad, h//2 + notch_h)], fill=(200, 250, 255, 255), width=4)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(10))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=4)
    img.save(path)
    print(f"Saved: {path}")

def generate_card_frame(path):
    w, h = 600, 440
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    pad = 14
    cut = 38
    pts = [
        (pad + cut, pad),
        (w - pad - cut, pad),
        (w - pad, pad + cut),
        (w - pad, h - pad - cut),
        (w - pad - cut, h - pad),
        (pad + cut, h - pad),
        (pad, h - pad - cut),
        (pad, pad + cut)
    ]

    draw.polygon(pts, fill=(8, 22, 38, 235))
    for gw, ga in [(16, 30), (10, 70), (5, 140), (2, 255)]:
        draw_glow.polygon(pts, outline=(0, 225, 255, ga), width=gw)
    draw.polygon(pts, outline=(0, 235, 255, 255), width=3)

    pad2 = pad + 7
    cut2 = cut - 3
    pts2 = [
        (pad2 + cut2, pad2),
        (w - pad2 - cut2, pad2),
        (w - pad2, pad2 + cut2),
        (w - pad2, h - pad2 - cut2),
        (w - pad2 - cut2, h - pad2),
        (pad2 + cut2, h - pad2),
        (pad2, h - pad2 - cut2),
        (pad2, pad2 + cut2)
    ]
    draw.polygon(pts2, outline=(0, 180, 240, 130), width=2)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(8))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=3)
    img.save(path)
    print(f"Saved: {path}")

def generate_pill_frame(path):
    w, h = 600, 140
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    pad = 8
    r = (h - pad * 2) // 2

    draw.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, fill=(4, 28, 48, 240))
    for gw, ga in [(16, 35), (10, 80), (5, 160), (2, 255)]:
        draw_glow.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, outline=(0, 240, 255, ga), width=gw)

    draw.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, outline=(0, 245, 255, 255), width=4)
    draw.rounded_rectangle([pad + 4, pad + 4, w - pad - 4, h - pad - 4], radius=r - 4, outline=(0, 190, 240, 130), width=2)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(8))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=3)
    img.save(path)
    print(f"Saved: {path}")

generate_inertia_icon("Assets/Textures/UI/Icon_FirstLaw_Inertia.png")
generate_force_icon("Assets/Textures/UI/Icon_SecondLaw_Force.png")
generate_action_reaction_icon("Assets/Textures/UI/Icon_ThirdLaw_ActionReaction.png")
generate_scifi_main_frame("Assets/Textures/UI/SciFi_Main_Frame.png")
generate_card_frame("Assets/Textures/UI/SciFi_Card_Frame.png")
generate_pill_frame("Assets/Textures/UI/SciFi_Pill_Frame.png")
