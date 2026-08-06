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

        // 홀드(롱노트) 꼬리 바(RhythmNote)처럼, transform.localScale만으로 원하는 길이/두께를 직접
        // 지정하고 싶은 막대형 스프라이트. pixelsPerUnit을 텍스처 크기와 동일하게 맞춰서 scale=1일 때
        // 정확히 1x1 월드 유닛이 되도록 한다 - 이후 localScale = new Vector3(원하는 길이, 원하는 두께, 1)
        // 로 바로 늘려 쓸 수 있다.
        public static Sprite CreateUnitSquare(Color color)
        {
            const int size = 4;
            return BuildSprite(size, (x, y) => color, size);
        }

        private static Sprite BuildSprite(int size, System.Func<int, int, Color> pixelFunc, float pixelsPerUnit = 100f)
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
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
