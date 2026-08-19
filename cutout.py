#!/usr/bin/env python3
"""Flat-background cutout for Shallow Sea Dream illustrated sprites.

These Gemini illustrations sit on a near-uniform teal/dark background. A
flood-fill-from-edges color-key gives cleaner edges than salient-object rembg
for this flat-bg case, so that is the primary method. rembg is available as a
fallback for any image where flood-fill leaves too much background.

Usage:
    .venv/bin/python cutout.py
Originals are backed up to assets/sprites/_raw and assets/img/_raw first.
"""
import os
import shutil
from collections import deque
from PIL import Image, ImageFilter
import numpy as np

ROOT = os.path.dirname(os.path.abspath(__file__))


def backup(path):
    raw = os.path.join(os.path.dirname(path), "_raw")
    os.makedirs(raw, exist_ok=True)
    dst = os.path.join(raw, os.path.basename(path))
    if not os.path.exists(dst):
        shutil.copy2(path, dst)


def flood_key(path, tol=42, feather=1.2, edge_erode=True):
    """Make near-uniform edge-connected background transparent via flood fill."""
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im).astype(np.int16)
    h, w, _ = arr.shape
    rgb = arr[:, :, :3]

    frame = np.concatenate([
        rgb[:3, :, :].reshape(-1, 3),
        rgb[-3:, :, :].reshape(-1, 3),
        rgb[:, :3, :].reshape(-1, 3),
        rgb[:, -3:, :].reshape(-1, 3),
    ])
    bg = np.median(frame, axis=0)
    dist = np.sqrt(((rgb - bg) ** 2).sum(axis=2))
    near = dist <= tol

    visited = np.zeros((h, w), dtype=bool)
    dq = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near[y, x] and not visited[y, x]:
                visited[y, x] = True
                dq.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if near[y, x] and not visited[y, x]:
                visited[y, x] = True
                dq.append((y, x))
    while dq:
        y, x = dq.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and near[ny, nx]:
                visited[ny, nx] = True
                dq.append((ny, nx))

    alpha = np.where(visited, 0, 255).astype(np.uint8)
    a_img = Image.fromarray(alpha, mode="L")
    if edge_erode:
        a_img = a_img.filter(ImageFilter.MinFilter(3))
    if feather:
        a_img = a_img.filter(ImageFilter.GaussianBlur(feather))

    out = im.copy()
    out.putalpha(a_img)
    return out, float((alpha == 0).mean())


def flood_key_gradient(path, seed_tol=70, step_tol=22, feather=1.2):
    """Gradient-tolerant flood: grow the background region by *local* similarity so a
    smoothly-shaded teal bg (where subject and bg share a hue) is followed without
    eating the subject. A pixel joins the bg if it is within `step_tol` of an already-
    accepted neighbour AND within `seed_tol` of the global border colour (a guard so
    growth can't wander onto the subject)."""
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im).astype(np.int16)
    h, w, _ = arr.shape
    rgb = arr[:, :, :3]
    frame = np.concatenate([
        rgb[:3, :].reshape(-1, 3), rgb[-3:, :].reshape(-1, 3),
        rgb[:, :3].reshape(-1, 3), rgb[:, -3:].reshape(-1, 3),
    ])
    bg = np.median(frame, axis=0)
    guard = np.sqrt(((rgb - bg) ** 2).sum(axis=2)) <= seed_tol * 1.8  # generous global guard

    visited = np.zeros((h, w), dtype=bool)
    dq = deque()
    for x in range(w):
        for y in (0, h - 1):
            if guard[y, x] and not visited[y, x]:
                visited[y, x] = True; dq.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if guard[y, x] and not visited[y, x]:
                visited[y, x] = True; dq.append((y, x))
    while dq:
        y, x = dq.popleft()
        cur = rgb[y, x]
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and guard[ny, nx]:
                if np.sqrt(((rgb[ny, nx] - cur) ** 2).sum()) <= step_tol:
                    visited[ny, nx] = True; dq.append((ny, nx))

    alpha = np.where(visited, 0, 255).astype(np.uint8)
    a_img = Image.fromarray(alpha, mode="L").filter(ImageFilter.MinFilter(3))
    if feather:
        a_img = a_img.filter(ImageFilter.GaussianBlur(feather))
    out = im.copy(); out.putalpha(a_img)
    return out, float((alpha == 0).mean())


def luma_key_dark(path, lum_cut=105, feather=1.2):
    """Global luminance key for sheets whose subjects are uniformly bright and whose
    background is dark (and not edge-connected because the bright subject spans every
    border). Any pixel darker than `lum_cut` becomes transparent. No flood needed."""
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im).astype(np.int16)
    lum = arr[:, :, :3].mean(axis=2)
    alpha = np.where(lum < lum_cut, 0, 255).astype(np.uint8)
    a_img = Image.fromarray(alpha, mode="L").filter(ImageFilter.MaxFilter(3))  # close pinholes
    if feather:
        a_img = a_img.filter(ImageFilter.GaussianBlur(feather))
    out = im.copy(); out.putalpha(a_img)
    return out, float((alpha == 0).mean())


def autocrop(im, pad=8):
    a = np.asarray(im)[:, :, 3]
    ys, xs = np.where(a > 16)
    if len(xs) == 0:
        return im
    x0, x1 = max(0, xs.min() - pad), min(im.width, xs.max() + pad)
    y0, y1 = max(0, ys.min() - pad), min(im.height, ys.max() + pad)
    return im.crop((x0, y0, x1, y1))


def rembg_cut(path):
    from rembg import remove
    return remove(Image.open(path).convert("RGBA"))


# (relative path, tolerance, crop?)  crop=False for grid sheets (keep cell geometry)
# (path, tol/cut, crop, method) — method "flood" = edge-connected colour key (default);
# "luma" = global dark-luminance key for sheets whose bright subject spans every border
# and isolates the dark bg from the edge (ice, water_attack).
SHEETS = [
    ("assets/sprites/jellyfish_base.png", 95, False, "flood"),  # dark bg, bright subject
    ("assets/sprites/jellyfish_water.png", 50, False, "flood"),
    ("assets/sprites/jellyfish_ice.png", 110, False, "luma"),
    ("assets/sprites/jellyfish_electric.png", 60, False, "flood"),
    ("assets/sprites/water_attack.png", 122, False, "luma"),
]
# (path, tol, crop, force_rembg) — force_rembg for cream/white-bg portraits where the
# subject and corners are too far apart in colour for a single flood seed to read well.
SINGLES = [
    ("assets/sprites/brood_mother.png", 44, True, True),  # teal mass on teal bg → rembg
    ("assets/sprites/invader_1.png", 90, True, False),
    ("assets/sprites/invader_2.png", 50, True, False),
    ("assets/sprites/invader_3.png", 50, True, False),
    ("assets/img/water_shard.png", 50, True, False),
    ("assets/img/icon_memory.png", 50, True, False),
    ("assets/img/icon_note.png", 44, True, False),
    ("assets/img/npc_npc1giving.png", 90, True, False),
    ("assets/img/npc_starfish.png", 60, True, False),
    ("assets/img/npc_seaweed.png", 72, True, False),
    ("assets/img/npc_hermit.png", 50, True, False),
    ("assets/img/npc_lantern.png", 40, True, True),   # cream bg
    ("assets/img/npc_shoal.png", 44, True, False),
    ("assets/img/npc_barrel.png", 44, True, False),
    ("assets/img/npc_boss.png", 40, True, True),       # cream bg
]


def process(rel, tol, crop, allow_rembg=True, force_rembg=False, mode="flood"):
    path = os.path.join(ROOT, rel)
    if not os.path.exists(path):
        print(f"  skip (missing) {rel}")
        return
    backup(path)
    src = os.path.join(os.path.dirname(path), "_raw", os.path.basename(path))
    method = mode
    if force_rembg:
        out, removed = rembg_cut(src), -1.0
        method = "rembg!"
    elif mode == "luma":
        out, removed = luma_key_dark(src, lum_cut=tol)
    else:
        out, removed = flood_key(src, tol=tol)
        # rembg fallback ONLY for single illustrations — never for grid sheets, where
        # it would collapse the 6-cell layout into one salient object.
        if removed < 0.04 and allow_rembg:
            try:
                out = rembg_cut(src)
                method = "rembg"
            except Exception as e:
                print(f"  rembg failed {rel}: {e}")
    if crop:
        out = autocrop(out)
    out.save(path)
    rstr = "  n/a" if removed < 0 else f"{removed:5.1%}"
    print(f"  {method:5s} removed={rstr} -> {rel} {out.size}")


def main():
    print("== Sheets (no crop, keep grid, flood-only) ==")
    for rel, tol, crop, mode in SHEETS:
        process(rel, tol, crop, allow_rembg=False, mode=mode)
    print("== Singles (autocrop) ==")
    for rel, tol, crop, force in SINGLES:
        process(rel, tol, crop, force_rembg=force)
    print("done")


if __name__ == "__main__":
    main()
