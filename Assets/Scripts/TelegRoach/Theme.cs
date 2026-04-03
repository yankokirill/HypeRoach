using UnityEngine;

public static class Theme
{
    public static readonly Color BG_CHAT = HexColor("E8EDF2");
    public static readonly Color BG_HEADER = HexColor("FFFFFF");
    public static readonly Color BG_BOT_BUBBLE = HexColor("FFFFFF");
    public static readonly Color BG_PLY_BUBBLE = HexColor("EEFFDE");
    public static readonly Color TEXT_MAIN = HexColor("000000");
    public static readonly Color TEXT_MUTED = HexColor("8598A8");
    public static readonly Color CHECK_READ = HexColor("4FC3F7");
    public static readonly Color CHECK_UNREAD = HexColor("9BB8D0");

    public const int AVATAR_SIZE = 34;
    public const int PAD_H = 14;
    public const int PAD_V = 10;
    public const int MSG_SPACING = 6;

    private static Sprite _roundedBubbleSprite;

    public static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    public static Sprite GetRoundedBubbleSprite()
    {
        return _roundedBubbleSprite;
    }

    static public void Initialize()
    {
        int size = 128;
        float radius = 20f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float distToTopLeft = Mathf.Sqrt(Mathf.Pow(px - radius, 2) + Mathf.Pow(py - radius, 2));
                float distToTopRight = Mathf.Sqrt(Mathf.Pow((size - radius) - px, 2) + Mathf.Pow(py - radius, 2));
                float distToBottomLeft = Mathf.Sqrt(Mathf.Pow(px - radius, 2) + Mathf.Pow((size - radius) - py, 2));
                float distToBottomRight = Mathf.Sqrt(Mathf.Pow((size - radius) - px, 2) + Mathf.Pow((size - radius) - py, 2));

                bool inside = true;
                if (px < radius && py < radius && distToTopLeft > radius) inside = false;
                else if (px > size - radius && py < radius && distToTopRight > radius) inside = false;
                else if (px < radius && py > size - radius && distToBottomLeft > radius) inside = false;
                else if (px > size - radius && py > size - radius && distToBottomRight > radius) inside = false;

                pixels[y * size + x] = inside ? white : transparent;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _roundedBubbleSprite = Sprite.Create(
            tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius)
        );
    }
}
