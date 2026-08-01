using UnityEngine;

namespace ConductorSymphony.Rhythm
{
    // Fixed-radius ring around the character marking the Perfect judgment line.
    // Unlike ShrinkingRhythmRing (removed), this ring does not resize or fade with the beat —
    // it stays at a constant radius so players have a stable visual target to hit notes against.
    public class JudgmentRing : MonoBehaviour
    {
        private Transform targetTransform;
        private float radius;
        private Color ringColor;
        private float alpha;
        private LineRenderer lineRenderer;
        private static int segments = 64;

        public void Initialize(Transform target, float radius, Color color, float alpha)
        {
            targetTransform = target;
            this.radius = radius;
            ringColor = color;
            this.alpha = alpha;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.025f;

            Material whiteMat = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = whiteMat;
            Color c = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            lineRenderer.sortingOrder = 9; // Render underneath notes (order 10)

            UpdateRingPositions();
        }

        private void Update()
        {
            if (targetTransform == null) return;
            UpdateRingPositions();
        }

        private void UpdateRingPositions()
        {
            Vector3 center = targetTransform != null ? targetTransform.position : Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
