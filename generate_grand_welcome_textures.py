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

    cx, cy = 345, 256
    # Speed trails trailing to the left
    trail_specs = [
        (-52, 240, 10, (0, 170, 255, 170)),
        (-30, 310, 18, (0, 210, 255, 210)),
        (-8, 360, 24, (0, 240, 255, 255)),
        (14, 330, 20, (0, 220, 255, 220)),
        (36, 270, 14, (0, 190, 255, 180)),
        (56, 210, 8, (0, 150, 255, 130))
    ]
    for dy, length, w, col in trail_specs:
        start_x = cx - 20
        end_x = start_x - length
        y = cy + dy
        pts = [(start_x, y - w // 2), (end_x, y), (start_x, y + w // 2)]
        draw_glow.polygon(pts, fill=col)
        # Inner bright white streak
        pts_core = [(start_x, y - w // 4), (end_x + 70, y), (start_x, y + w // 4)]
        draw.polygon(pts_core, fill=(215, 252, 255, 240))

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(14))
    img = Image.alpha_composite(glow_layer, img)
    draw = ImageDraw.Draw(img)

    # Main Glowing Sphere
    draw_sphere(
        draw, cx, cy, 102,
        base_col=(0, 60, 160, 255),
        mid_col=(0, 195, 255, 255),
        high_col=(245, 254, 255, 255)
    )

    # Outer Sphere Rim Glow
    for i in range(1, 24):
        alpha = int(180 * (1.0 - i / 24))
        draw.ellipse([cx - 102 - i, cy - 102 - i, cx + 102 + i, cy + 102 + i], outline=(0, 245, 255, alpha), width=2)

    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_force_icon(path):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    cx, cy = 180, 256
    s = 84

    # Isometric Cube
    p_top = (cx, cy - int(s * 1.15))
    p_top_r = (cx + int(s * 1.0), cy - int(s * 0.58))
    p_top_l = (cx - int(s * 1.0), cy - int(s * 0.58))
    p_center = (cx, cy)
    p_bot = (cx, cy + int(s * 1.15))
    p_bot_r = (cx + int(s * 1.0), cy + int(s * 0.58))
    p_bot_l = (cx - int(s * 1.0), cy + int(s * 0.58))

    draw.polygon([p_top, p_top_r, p_center, p_top_l], fill=(130, 235, 255, 255))
    draw.polygon([p_top_l, p_center, p_bot, p_bot_l], fill=(0, 180, 255, 255))
    draw.polygon([p_top_r, p_center, p_bot, p_bot_r], fill=(0, 120, 220, 255))

    edges = [
        (p_top, p_top_r), (p_top_r, p_center), (p_center, p_top_l), (p_top_l, p_top),
        (p_center, p_bot), (p_top_l, p_bot_l), (p_bot_l, p_bot),
        (p_top_r, p_bot_r), (p_bot_r, p_bot)
    ]
    for p1, p2 in edges:
        draw.line([p1, p2], fill=(235, 254, 255, 255), width=4)

    # Force Arrow
    ay = cy - 4
    arrow_pts = [
        (280, ay - 26),
        (375, ay - 26),
        (375, ay - 56),
        (475, ay),
        (375, ay + 56),
        (375, ay + 26),
        (280, ay + 26)
    ]
    draw_glow.polygon(arrow_pts, fill=(0, 240, 255, 255))
    draw.polygon(arrow_pts, fill=(0, 240, 255, 255))
    draw.line(arrow_pts + [arrow_pts[0]], fill=(245, 255, 255, 255), width=4)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(12))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_action_reaction_icon(path):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    r = 78
    cy = 256
    cx1, cx2 = 120, 392

    draw_sphere(draw, cx1, cy, r, (0, 65, 160, 255), (0, 190, 255, 255), (245, 254, 255, 255))
    draw_sphere(draw, cx2, cy, r, (0, 65, 160, 255), (0, 190, 255, 255), (245, 254, 255, 255))

    for i in range(1, 18):
        alpha = int(150 * (1.0 - i / 18))
        draw.ellipse([cx1 - r - i, cy - r - i, cx1 + r + i, cy + r + i], outline=(0, 240, 255, alpha), width=2)
        draw.ellipse([cx2 - r - i, cy - r - i, cx2 + r + i, cy + r + i], outline=(0, 240, 255, alpha), width=2)

    # Top arrow: points RIGHT --->
    ty = cy - 30
    top_arrow = [
        (205, ty - 15),
        (275, ty - 15),
        (275, ty - 32),
        (328, ty),
        (275, ty + 32),
        (275, ty + 15),
        (205, ty + 15)
    ]
    draw_glow.polygon(top_arrow, fill=(0, 240, 255, 255))
    draw.polygon(top_arrow, fill=(0, 240, 255, 255))
    draw.line(top_arrow + [top_arrow[0]], fill=(245, 255, 255, 255), width=3)

    # Bottom arrow: points LEFT <---
    by = cy + 30
    bot_arrow = [
        (307, by - 15),
        (237, by - 15),
        (237, by - 32),
        (184, by),
        (237, by + 32),
        (237, by + 15),
        (307, by + 15)
    ]
    draw_glow.polygon(bot_arrow, fill=(0, 240, 255, 255))
    draw.polygon(bot_arrow, fill=(0, 240, 255, 255))
    draw.line(bot_arrow + [bot_arrow[0]], fill=(245, 255, 255, 255), width=3)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(12))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=6)
    img.save(path)
    print(f"Saved: {path}")

def generate_grand_main_frame(path):
    w, h = 2048, 1280
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    grid_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw_grid = ImageDraw.Draw(grid_layer)
    draw = ImageDraw.Draw(img)

    pad = 32
    cut = 110
    top_tab_w = 420
    top_tab_h = 36

    # Perimeter polygon with Top Center Crown Tab and Chamfered Corners
    pts = [
        # Top-Left to Top-Center
        (pad + cut, pad),
        (w // 2 - top_tab_w // 2 - 28, pad),
        (w // 2 - top_tab_w // 2, pad - top_tab_h),
        (w // 2 + top_tab_w // 2, pad - top_tab_h),
        (w // 2 + top_tab_w // 2 + 28, pad),
        (w - pad - cut, pad),
        # Top-Right Chamfer
        (w - pad, pad + cut),
        # Right Side
        (w - pad, h - pad - cut),
        # Bottom-Right Chamfer
        (w - pad - cut, h - pad),
        # Bottom Edge
        (pad + cut, h - pad),
        # Bottom-Left Chamfer
        (pad, h - pad - cut),
        # Left Side
        (pad, pad + cut)
    ]

    # 1. Deep translucent navy glass background fill
    draw.polygon(pts, fill=(5, 14, 28, 242))

    # 2. Scientific Coordinate Grid Pattern inside the panel
    grid_spacing = 48
    for gx in range(pad + 40, w - pad - 40, grid_spacing):
        alpha = 32 if (gx % (grid_spacing * 4) != 0) else 55
        draw_grid.line([(gx, pad + 20), (gx, h - pad - 20)], fill=(0, 180, 240, alpha), width=1)

    for gy in range(pad + 20, h - pad - 20, grid_spacing):
        alpha = 32 if (gy % (grid_spacing * 4) != 0) else 55
        draw_grid.line([(pad + 40, gy), (w - pad - 40, gy)], fill=(0, 180, 240, alpha), width=1)

    # 3. Central Title Orbital Ring / Trajectory Ellipse with Nodes
    # Matches the glowing cyan ellipse around "NEWTONIAN VR LABORATORY" in the reference image!
    el_img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    el_glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_el_glow = ImageDraw.Draw(el_glow)
    draw_el = ImageDraw.Draw(el_img)

    el_cx, el_cy = w // 2, 215
    el_rx, el_ry = 600, 105
    el_rect = [el_cx - el_rx, el_cy - el_ry, el_cx + el_rx, el_cy + el_ry]
    for ew, ea in [(16, 40), (8, 90), (3, 220)]:
        draw_el_glow.ellipse(el_rect, outline=(0, 230, 255, ea), width=ew)
    draw_el.ellipse(el_rect, outline=(200, 250, 255, 255), width=2)

    # Orbital nodes / satellite dots on the ring
    for node_ang in [math.pi * 0.12, math.pi * 0.88, math.pi * 1.42]:
        nx = int(el_cx + el_rx * math.cos(node_ang))
        ny = int(el_cy + el_ry * math.sin(node_ang))
        draw_el_glow.ellipse([nx - 14, ny - 14, nx + 14, ny + 14], fill=(0, 245, 255, 220))
        draw_el.ellipse([nx - 7, ny - 7, nx + 7, ny + 7], fill=(240, 255, 255, 255))

    el_glow = el_glow.filter(ImageFilter.GaussianBlur(8))
    el_composite = Image.alpha_composite(el_glow, el_img)
    el_rotated = el_composite.rotate(-5, resample=Image.BICUBIC, center=(el_cx, el_cy))

    # Composite grid & orbital ring
    img = Image.alpha_composite(img, grid_layer)
    img = Image.alpha_composite(img, el_rotated)
    draw = ImageDraw.Draw(img)

    # 4. Multi-layered Radiant Neon Cyan Outer Frame
    for gw, ga in [(40, 20), (28, 45), (18, 90), (10, 160), (4, 255)]:
        draw_glow.polygon(pts, outline=(0, 230, 255, ga), width=gw)

    draw.polygon(pts, outline=(200, 250, 255, 255), width=5)

    # 5. Inner Secondary Cyan Accent Line
    pad_in = pad + 18
    cut_in = cut - 6
    pts_in = [
        (pad_in + cut_in, pad_in),
        (w // 2 - top_tab_w // 2 - 20, pad_in),
        (w // 2 - top_tab_w // 2 + 10, pad_in - top_tab_h + 16),
        (w // 2 + top_tab_w // 2 - 10, pad_in - top_tab_h + 16),
        (w // 2 + top_tab_w // 2 + 20, pad_in),
        (w - pad_in - cut_in, pad_in),
        (w - pad_in, pad_in + cut_in),
        (w - pad_in, h - pad_in - cut_in),
        (w - pad_in - cut_in, h - pad_in),
        (pad_in + cut_in, h - pad_in),
        (pad_in, h - pad_in - cut_in),
        (pad_in, pad_in + cut_in)
    ]
    draw.polygon(pts_in, outline=(0, 190, 245, 140), width=3)

    # 6. Prominent Glowing Cybernetic Corner Brackets (as seen in reference image)
    bracket_len = 160
    b_glow = (0, 245, 255, 255)
    b_col = (220, 252, 255, 255)

    def draw_corner_bracket(p1, p2, p3):
        draw_glow.line([p1, p2, p3], fill=b_glow, width=14)
        draw.line([p1, p2, p3], fill=b_col, width=6)

    # Top-Left Bracket
    draw_corner_bracket((pad - 6, pad + cut + bracket_len), (pad - 6, pad + cut), (pad + cut, pad - 6))
    draw_corner_bracket((pad - 6, pad + cut), (pad + cut, pad - 6), (pad + cut + bracket_len, pad - 6))

    # Top-Right Bracket
    draw_corner_bracket((w - pad + 6, pad + cut + bracket_len), (w - pad + 6, pad + cut), (w - pad - cut, pad - 6))
    draw_corner_bracket((w - pad + 6, pad + cut), (w - pad - cut, pad - 6), (w - pad - cut - bracket_len, pad - 6))

    # Bottom-Left Bracket
    draw_corner_bracket((pad - 6, h - pad - cut - bracket_len), (pad - 6, h - pad - cut), (pad + cut, h - pad + 6))
    draw_corner_bracket((pad - 6, h - pad - cut), (pad + cut, h - pad + 6), (pad + cut + bracket_len, h - pad + 6))

    # Bottom-Right Bracket
    draw_corner_bracket((w - pad + 6, h - pad - cut - bracket_len), (w - pad + 6, h - pad - cut), (w - pad - cut, h - pad + 6))
    draw_corner_bracket((w - pad + 6, h - pad - cut), (w - pad - cut, h - pad + 6), (w - pad - cut - bracket_len, h - pad + 6))

    # 7. Stepped Side Wings / Brackets (Left & Right)
    wing_h = 75
    draw_glow.polygon([(pad, h//2 - wing_h), (pad - 26, h//2 - wing_h//2), (pad - 26, h//2 + wing_h//2), (pad, h//2 + wing_h)], fill=(0, 230, 255, 140), outline=(0, 245, 255, 255))
    draw.line([(pad, h//2 - wing_h), (pad - 26, h//2 - wing_h//2), (pad - 26, h//2 + wing_h//2), (pad, h//2 + wing_h)], fill=(225, 252, 255, 255), width=5)

    draw_glow.polygon([(w - pad, h//2 - wing_h), (w - pad + 26, h//2 - wing_h//2), (w - pad + 26, h//2 + wing_h//2), (w - pad, h//2 + wing_h)], fill=(0, 230, 255, 140), outline=(0, 245, 255, 255))
    draw.line([(w - pad, h//2 - wing_h), (w - pad + 26, h//2 - wing_h//2), (w - pad + 26, h//2 + wing_h//2), (w - pad, h//2 + wing_h)], fill=(225, 252, 255, 255), width=5)

    # 8. Slanted Tech Hash Vents (Bottom Left & Bottom Right)
    # Exactly matching the illuminated cyan vent bars in the reference image!
    vent_y = h - pad - 60
    vent_h = 28
    vent_w = 12
    vent_spacing = 18

    # Left Vents: Slanted ///
    for k in range(6):
        vx = pad + cut + 25 + k * vent_spacing
        v_pts = [(vx, vent_y + vent_h), (vx + 10, vent_y), (vx + 10 + vent_w, vent_y), (vx + vent_w, vent_y + vent_h)]
        draw_glow.polygon(v_pts, fill=(0, 240, 255, 220))
        draw.polygon(v_pts, fill=(210, 250, 255, 255))

    # Right Vents: Slanted \\\
    for k in range(6):
        vx = w - pad - cut - 25 - (6 - k) * vent_spacing
        v_pts = [(vx + 10, vent_y + vent_h), (vx, vent_y), (vx + vent_w, vent_y), (vx + 10 + vent_w, vent_y + vent_h)]
        draw_glow.polygon(v_pts, fill=(0, 240, 255, 220))
        draw.polygon(v_pts, fill=(210, 250, 255, 255))

    # 9. Top Crown Horizontal Accent Bar
    tb_pts = [(w // 2 - top_tab_w // 2 + 30, pad - top_tab_h + 12), (w // 2 + top_tab_w // 2 - 30, pad - top_tab_h + 12)]
    draw_glow.line(tb_pts, fill=(0, 245, 255, 255), width=10)
    draw.line(tb_pts, fill=(240, 255, 255, 255), width=4)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(14))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=5)
    img.save(path)
    print(f"Saved Grand Main Frame: {path}")

def generate_grand_card_frame(path):
    w, h = 800, 560
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    grid_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw_grid = ImageDraw.Draw(grid_layer)
    draw = ImageDraw.Draw(img)

    pad = 18
    cut = 48
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

    # Translucent card background
    draw.polygon(pts, fill=(7, 20, 36, 240))

    # Perspective 3D Grid lines on the lower half of the card (where the icon sits)
    # Gives the holographic pedestal effect seen in the reference image!
    vp_y = h // 2 - 20
    for gx in range(pad + 40, w - pad - 40, 48):
        draw_grid.line([(gx, h - pad - 20), (w // 2 + (gx - w // 2) * 0.45, vp_y)], fill=(0, 190, 245, 45), width=1)
    for gy in range(vp_y, h - pad - 20, 32):
        draw_grid.line([(pad + 30, gy), (w - pad - 30, gy)], fill=(0, 190, 245, 40), width=1)

    img = Image.alpha_composite(img, grid_layer)
    draw = ImageDraw.Draw(img)

    for gw, ga in [(22, 35), (14, 75), (6, 160), (3, 255)]:
        draw_glow.polygon(pts, outline=(0, 230, 255, ga), width=gw)
    draw.polygon(pts, outline=(210, 250, 255, 255), width=4)

    # Inner fine border
    pad2 = pad + 10
    cut2 = cut - 5
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
    draw.polygon(pts2, outline=(0, 185, 240, 130), width=2)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(10))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=4)
    img.save(path)
    print(f"Saved Grand Card Frame: {path}")

def generate_grand_pill_frame(path):
    w, h = 800, 180
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow_layer)
    draw = ImageDraw.Draw(img)

    pad = 10
    r = (h - pad * 2) // 2

    draw.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, fill=(4, 28, 52, 245))
    for gw, ga in [(24, 40), (16, 90), (8, 170), (3, 255)]:
        draw_glow.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, outline=(0, 240, 255, ga), width=gw)

    draw.rounded_rectangle([pad, pad, w - pad, h - pad], radius=r, outline=(220, 252, 255, 255), width=5)
    draw.rounded_rectangle([pad + 6, pad + 6, w - pad - 6, h - pad - 6], radius=r - 6, outline=(0, 195, 245, 140), width=2)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(10))
    img = Image.alpha_composite(glow_layer, img)
    img = create_glow(img, blur_radius=4)
    img.save(path)
    print(f"Saved Grand Pill Frame: {path}")

if __name__ == "__main__":
    generate_inertia_icon("Assets/Textures/UI/Icon_FirstLaw_Inertia.png")
    generate_force_icon("Assets/Textures/UI/Icon_SecondLaw_Force.png")
    generate_action_reaction_icon("Assets/Textures/UI/Icon_ThirdLaw_ActionReaction.png")
    generate_grand_main_frame("Assets/Textures/UI/SciFi_Main_Frame.png")
    generate_grand_card_frame("Assets/Textures/UI/SciFi_Card_Frame.png")
    generate_grand_pill_frame("Assets/Textures/UI/SciFi_Pill_Frame.png")
