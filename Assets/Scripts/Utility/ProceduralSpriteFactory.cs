using UnityEngine;

namespace ConductorSymphony.Utility
{
    public static class ProceduralSpriteFactory
    {
        public static Sprite CreateFilledCircle(int size, float radius, Color color)
        {
            Vector2 center = new Vector2(size / 2f, size / 2f);
            return BuildSprite(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                return (d <= radius) ? color : Color.clear;
            });
        }

        public static Sprite CreateRingWithCore(int size, float ringInnerRadius, float ringOuterRadius, Color ringColor, Color coreColor)
        {
            Vector2 center = new Vector2(size / 2f, size / 2f);
            return BuildSprite(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d <= ringOuterRadius && d >= ringInnerRadius) return ringColor;
                if (d < ringInnerRadius) return coreColor;
                return Color.clear;
            });
        }

        public static Sprite CreateDiamond(int size, float radius, Color color)
        {
            Vector2 center = new Vector2(size / 2f, size / 2f);
            return BuildSprite(size, (x, y) =>
            {
                float dist = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                return (dist <= radius) ? color : Color.clear;
            });
        }

        private static Sprite BuildSprite(int size, System.Func<int, int, Color> pixelFunc)
        {
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = pixelFunc(x, y);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
