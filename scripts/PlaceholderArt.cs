using Godot;

#pragma warning disable CS0618 // SetAnimationLoop is deprecated but the replacement enum is not exposed in this binding

namespace ShallowSeaDream;

/// res://scripts/PlaceholderArt.cs
/// Procedural fallback visuals used when a supplied painting is unavailable. Produces simple
/// solid/round textures and 2-frame SpriteFrames so AnimatedSprite2D nodes animate
/// even with no PNG present. When the real sheet exists, callers slice it instead.
public static class PlaceholderArt
{
    /// A soft round blob texture of the given size and colour (alpha-feathered edge).
    public static ImageTexture RoundBlob(int size, Color color)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        float r = size * 0.5f;
        var center = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = new Vector2(x + 0.5f, y + 0.5f).DistanceTo(center) / r;
            float a = Mathf.Clamp(1.0f - Mathf.SmoothStep(0.8f, 1.0f, d), 0f, 1f);
            img.SetPixel(x, y, new Color(color.R, color.G, color.B, color.A * a));
        }
        return ImageTexture.CreateFromImage(img);
    }

    /// Build a 2-frame SpriteFrames ("Idle"+"Walk") from a tinted blob — a stand-in
    /// for a real sliced sheet.
    public static SpriteFrames BlobFrames(Color color, int size = 96)
    {
        var frames = new SpriteFrames();
        var a = RoundBlob(size, color);
        var b = RoundBlob(size, color.Lightened(0.15f));
        foreach (var anim in new[] { "Idle", "Walk" })
        {
            frames.AddAnimation(anim);
            frames.SetAnimationSpeed(anim, anim == "Walk" ? 9 : 6);
            frames.SetAnimationLoop(anim, true);
            frames.AddFrame(anim, a);
            frames.AddFrame(anim, b);
        }
        frames.RemoveAnimation("default");
        return frames;
    }

    /// Slice one of the existing painted sheets. Water is 3x2; the other forms are
    /// 2x3. The source sheets have an opaque dark-teal presentation background, so
    /// each cell is softly luminance-keyed in memory. This avoids destructive asset
    /// rewrites and restores the translucent tentacles that the old rembg pass lost.
    public static SpriteFrames SliceFormSheet(Texture2D sheet, int cols, int rows)
    {
        var frames = new SpriteFrames();
        var img = sheet.GetImage();
        int cw = img.GetWidth() / cols, ch = img.GetHeight() / rows;
        var cells = new System.Collections.Generic.List<Texture2D>();
        for (int i = 0; i < cols * rows; i++)
        {
            int cx = (i % cols) * cw, cy = (i / cols) * ch;
            var cell = img.GetRegion(new Rect2I(cx, cy, cw, ch));
            SoftKeyPresentationBackground(cell);
            cells.Add(ImageTexture.CreateFromImage(cell));
        }

        // Serpentine order follows the painted pose progression without jumping from
        // the bottom-right cell back across an entire row. Both states use all six
        // good frames; movement is simply a little more energetic.
        int[] order = cols == 3
            ? new[] { 0, 1, 2, 5, 4, 3 }
            : new[] { 0, 1, 3, 5, 4, 2 };
        foreach (var (anim, speed) in new[] { ("Idle", 4.0), ("Walk", 7.0) })
        {
            frames.AddAnimation(anim);
            frames.SetAnimationSpeed(anim, speed);
            frames.SetAnimationLoop(anim, true);
            foreach (int i in order)
                if (i < cells.Count) frames.AddFrame(anim, cells[i]);
        }
        frames.RemoveAnimation("default");
        return frames;
    }

    public static Texture2D KeyPresentationTexture(Texture2D source, float cropFraction = 0f)
    {
        var image = source.GetImage();
        if (cropFraction > 0f)
        {
            int mx = Mathf.RoundToInt(image.GetWidth() * cropFraction);
            int my = Mathf.RoundToInt(image.GetHeight() * cropFraction);
            image = image.GetRegion(new Rect2I(mx, my,
                image.GetWidth() - mx * 2, image.GetHeight() - my * 2));
        }
        if (image.GetFormat() != Image.Format.Rgba8) image.Convert(Image.Format.Rgba8);
        // Most hand-painted character files supplied by the project owner already
        // have a transparent border. Preserve that authored alpha instead of trying
        // to key dark ink out as if it were a presentation-sheet background.
        if (!HasTransparentCorners(image))
            SoftKeyPresentationBackground(image);
        return ImageTexture.CreateFromImage(image);
    }

    private static bool HasTransparentCorners(Image image)
    {
        int inset = Mathf.Min(8, Mathf.Min(image.GetWidth(), image.GetHeight()) / 10);
        if (inset < 0) return false;
        return image.GetPixel(inset, inset).A < 0.18f
            && image.GetPixel(image.GetWidth() - inset - 1, inset).A < 0.18f
            && image.GetPixel(inset, image.GetHeight() - inset - 1).A < 0.18f
            && image.GetPixel(image.GetWidth() - inset - 1, image.GetHeight() - inset - 1).A < 0.18f;
    }

    private static void SoftKeyPresentationBackground(Image cell)
    {
        if (cell.GetFormat() != Image.Format.Rgba8)
            cell.Convert(Image.Format.Rgba8);
        int w = cell.GetWidth();
        int h = cell.GetHeight();
        const int edge = 7;
        int sample = Mathf.Min(12, Mathf.Min(w, h) / 8);
        Color tl = cell.GetPixel(sample, sample);
        Color tr = cell.GetPixel(w - sample - 1, sample);
        Color bl = cell.GetPixel(sample, h - sample - 1);
        Color br = cell.GetPixel(w - sample - 1, h - sample - 1);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = cell.GetPixel(x, y);
            float tx = w <= 1 ? 0f : (float)x / (w - 1);
            float ty = h <= 1 ? 0f : (float)y / (h - 1);
            Color top = tl.Lerp(tr, tx);
            Color bottom = bl.Lerp(br, tx);
            Color bg = top.Lerp(bottom, ty);
            float dr = c.R - bg.R, dg = c.G - bg.G, db = c.B - bg.B;
            float distance = Mathf.Sqrt(dr * dr + dg * dg + db * db);
            float light = Mathf.Max(c.R, Mathf.Max(c.G, c.B));
            // Per-pixel distance from an interpolated presentation-paper estimate
            // removes gradients and keeps the translucent painted glow intact.
            float alpha = Mathf.SmoothStep(0.055f, 0.23f, distance)
                        * Mathf.SmoothStep(0.55f, 0.80f, light);
            if (x < edge || y < edge || x >= w - edge || y >= h - edge) alpha = 0f;
            cell.SetPixel(x, y, new Color(c.R, c.G, c.B, c.A * alpha));
        }
    }

    /// Form sheet from the selected supplied PNG if present, else a tinted fallback.
    public static SpriteFrames FormFrames(ElementForm form)
    {
        var tex = AssetLoader.Texture(AssetLoader.RawFormSheet(form));
        if (tex != null)
            return SliceFormSheet(tex, 3, 2);
        return BlobFrames(Palette.ForForm(form));
    }

    /// Slice a 3x2 hand-painted creature presentation sheet and key out its paper
    /// background in memory. Unlike the translucent player key, this preserves dark
    /// tar/factory silhouettes by measuring colour distance without a lightness gate.
    public static SpriteFrames SliceCreatureSheet(Texture2D sheet, int cols = 3, int rows = 2)
    {
        var result = new SpriteFrames();
        var source = sheet.GetImage();
        int cw = source.GetWidth() / cols, ch = source.GetHeight() / rows;
        result.AddAnimation("Idle");
        result.SetAnimationSpeed("Idle", 4.5);
        result.SetAnimationLoop("Idle", true);
        int[] order = { 0, 1, 2, 5, 4, 3 };
        foreach (int index in order)
        {
            var cell = source.GetRegion(new Rect2I((index % cols) * cw, (index / cols) * ch, cw, ch));
            KeyOpaquePresentationBackground(cell);
            result.AddFrame("Idle", ImageTexture.CreateFromImage(cell));
        }
        result.RemoveAnimation("default");
        return result;
    }

    private static void KeyOpaquePresentationBackground(Image cell)
    {
        if (cell.GetFormat() != Image.Format.Rgba8) cell.Convert(Image.Format.Rgba8);
        int w = cell.GetWidth(), h = cell.GetHeight();
        int sample = Mathf.Min(12, Mathf.Min(w, h) / 8);
        Color tl = cell.GetPixel(sample, sample), tr = cell.GetPixel(w - sample - 1, sample);
        Color bl = cell.GetPixel(sample, h - sample - 1), br = cell.GetPixel(w - sample - 1, h - sample - 1);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = cell.GetPixel(x, y);
            float tx = w <= 1 ? 0f : (float)x / (w - 1);
            float ty = h <= 1 ? 0f : (float)y / (h - 1);
            Color bg = tl.Lerp(tr, tx).Lerp(bl.Lerp(br, tx), ty);
            float dr = c.R - bg.R, dg = c.G - bg.G, db = c.B - bg.B;
            float distance = Mathf.Sqrt(dr * dr + dg * dg + db * db);
            float alpha = Mathf.SmoothStep(0.065f, 0.19f, distance);
            if (x < 5 || y < 5 || x >= w - 5 || y >= h - 5) alpha = 0f;
            cell.SetPixel(x, y, new Color(c.R, c.G, c.B, c.A * alpha));
        }
    }

    /// Scale a Sprite2D so its longest dimension equals targetLongestPx.
    /// Call AFTER assigning the texture. No-op if texture is null or zero-size.
    public static void FitSprite(Sprite2D s, float targetLongestPx)
    {
        if (s.Texture == null) return;
        float longest = Mathf.Max(s.Texture.GetWidth(), s.Texture.GetHeight());
        if (longest > 0f) s.Scale = Vector2.One * (targetLongestPx / longest);
    }
}
