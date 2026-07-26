using UnityEngine;

namespace ConductorSymphony.Rhythm
{
    public class SoundwaveRingEffect : MonoBehaviour
    {
        private float duration = 0.28f;
        private float startRadius = 0.3f;
        private float endRadius = 1.6f;
        private float elapsedTime = 0f;

        private LineRenderer lineRenderer;
        private Vector3 centerPos;
        private Color ringColor;
        private static int segments = 48;

        public void Initialize(Vector3 spawnCenter, Color color)
        {
            centerPos = spawnCenter;
            ringColor = color;
            transform.position = centerPos;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.02f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = mat;
            lineRenderer.startColor = ringColor;
            lineRenderer.endColor = ringColor;
            lineRenderer.sortingOrder = 11;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            float currentRadius = Mathf.Lerp(startRadius, endRadius, progress);
            float alpha = Mathf.Lerp(1.0f, 0.0f, progress);

            Color c = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                float x = centerPos.x + Mathf.Cos(angle) * currentRadius;
                float y = centerPos.y + Mathf.Sin(angle) * currentRadius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }

            if (progress >= 1.0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
