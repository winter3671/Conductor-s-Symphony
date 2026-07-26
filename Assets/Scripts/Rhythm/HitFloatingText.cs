using UnityEngine;
using UnityEngine.UI;

namespace ConductorSymphony.Rhythm
{
    public class HitFloatingText : MonoBehaviour
    {
        private float floatDuration = 0.45f;
        private float floatSpeed = 1.8f;
        private float elapsedTime = 0f;

        private Text textComponent;
        private Canvas canvasComponent;
        private Vector3 startWorldPos;

        public void Initialize(Vector3 spawnPos, HitRating rating)
        {
            startWorldPos = spawnPos + new Vector3(0f, 0.9f, 0f);
            transform.position = startWorldPos;

            // Create Canvas for World-Space UI
            canvasComponent = gameObject.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.sortingOrder = 25; // Render high above other sprites

            RectTransform canvasRt = GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(300f, 100f);
            canvasRt.localScale = new Vector3(0.01f, 0.01f, 1.0f);

            // Create Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(transform, false);

            textComponent = textObj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 36;
            textComponent.fontStyle = FontStyle.BoldAndItalic;
            textComponent.alignment = TextAnchor.MiddleCenter;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Rating Text & Color
            switch (rating)
            {
                case HitRating.Perfect:
                    textComponent.text = "PERFECT!";
                    textComponent.color = new Color(1.0f, 0.84f, 0.0f); // Gold (#FFD700)
                    break;
                case HitRating.Great:
                    textComponent.text = "GREAT!";
                    textComponent.color = new Color(0.0f, 0.95f, 1.0f); // Neon Cyan (#00FFFF)
                    break;
                case HitRating.Miss:
                    textComponent.text = "MISS";
                    textComponent.color = new Color(1.0f, 0.25f, 0.25f); // Red (#FF4444)
                    break;
            }

            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / floatDuration);

            // Float upwards
            transform.position = startWorldPos + new Vector3(0f, progress * floatSpeed * 0.45f, 0f);

            // Elastic Bounce Scale (Pop up to 1.25, settle to 1.0)
            float scale = 1.0f;
            if (progress < 0.25f)
            {
                scale = Mathf.Lerp(0.3f, 1.25f, progress / 0.25f);
            }
            else if (progress < 0.4f)
            {
                scale = Mathf.Lerp(1.25f, 1.0f, (progress - 0.25f) / 0.15f);
            }
            transform.localScale = new Vector3(scale * 0.01f, scale * 0.01f, 1.0f);

            // Fade Out Alpha near end
            float alpha = 1.0f;
            if (progress > 0.6f)
            {
                alpha = Mathf.Lerp(1.0f, 0.0f, (progress - 0.6f) / 0.4f);
            }

            if (textComponent != null)
            {
                Color c = textComponent.color;
                textComponent.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (progress >= 1.0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
