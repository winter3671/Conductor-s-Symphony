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

        // 반지름이 "월드 유닛"과 1:1로 대응하는 링 스프라이트. 기존 CreateRingWithCore는 링 반지름을
        // 픽셀 좌표계로 정의해서, 실제로 원하는 월드 반지름(예: 공격 판정 반경)에 맞추려면 텍스처
        // 크기·픽셀 반지름 비율까지 감안한 별도 환산 상수가 필요했다 - 드럼 비트 뱅 이펙트가 실제
        // 판정 반경과 다른 크기로 그려지던 버그의 원인이 이것이었다. 이 메서드는 innerRadius01/
        // outerRadius01을 캔버스 반지름 대비 0~1 비율로 받고, pixelsPerUnit을 캔버스 반지름과 동일하게
        // 맞춰서 transform.localScale = Vector3.one * desiredWorldRadius만 하면 바로 정확한 월드
        // 반지름이 되도록 한다 - 공격 범위를 표시하는 링(지속 오라, 순간 충격파 등)에 사용할 것.
        public static Sprite CreateUnitRing(float innerRadius01, float outerRadius01, Color ringColor)
        {
            // 얇은 선처럼 보이는 링(outerRadius01-innerRadius01 차이가 작음)도 계단현상 없이 매끄럽게
            // 보이도록 256px 해상도를 쓴다. 드럼 오라 링처럼 두께 비율이 1.5%까지 얇아지는 경우도
            // 감안한 값(128px에서는 그 정도 두께가 1px 미만이라 뭉개지거나 아예 안 보일 위험이 있었음).
            const int size = 256;
            float halfSize = size / 2f;
            Vector2 center = new Vector2(halfSize, halfSize);
            return BuildSprite(size, (x, y) =>
            {
                float d01 = Vector2.Distance(new Vector2(x, y), center) / halfSize;
                return (d01 <= outerRadius01 && d01 >= innerRadius01) ? ringColor : Color.clear;
            }, halfSize);
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
