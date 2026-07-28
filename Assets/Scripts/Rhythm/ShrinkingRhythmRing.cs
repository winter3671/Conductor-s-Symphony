using UnityEngine;

namespace ConductorSymphony.Rhythm
{
    public class ShrinkingRhythmRing : MonoBehaviour
    {
        private Transform targetTransform;
        private float initialDistance;
        private float spawnTime;
        private float travelDuration;
        private float maxAlpha = 0.8f;
        private Color baseColor = Color.white;
        private LineRenderer lineRenderer;
        private static int segments = 64;

        public void Initialize(Transform target, float initialDist, float timeSpawned, float duration, Color ringColor, float alphaAmount = 0.8f)
        {
            targetTransform = target;
            initialDistance = initialDist;
            spawnTime = timeSpawned;
            travelDuration = duration;
            baseColor = ringColor;
            maxAlpha = alphaAmount;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth = 0.005f; // Razor-thin ultra-fine ring line
            lineRenderer.endWidth = 0.005f;

            Material whiteMat = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = whiteMat;
            lineRenderer.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, maxAlpha);
            lineRenderer.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, maxAlpha);
            lineRenderer.sortingOrder = 9; // Render underneath notes (order 10)
        }

        private void Update()
        {
            float currentTime = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongTime : 0f;
            float elapsed = currentTime - spawnTime;

            // Audio loop wrap-around guard: same issue as RhythmNote — if SongTime jumps back
            // to ~0 mid-flight because the master track looped, unwrap it so this ring still
            // reaches its Destroy condition instead of freezing on screen forever.
            if (elapsed < -travelDuration)
            {
                float loopLength = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongLoopLength : 0f;
                if (loopLength > 0f) elapsed += loopLength;
            }

            float progress = elapsed / travelDuration;

            if (progress > 1.05f || targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentRadius = Mathf.Lerp(initialDistance, 0f, progress);

            // Fade ring smoothly near player center or end of travel
            float alpha = maxAlpha;
            if (progress > 0.8f)
            {
                alpha = Mathf.Lerp(maxAlpha, 0.0f, (progress - 0.8f) / 0.2f);
            }

            Color c = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            Vector3 center = targetTransform != null ? targetTransform.position : Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                float x = center.x + Mathf.Cos(angle) * currentRadius;
                float y = center.y + Mathf.Sin(angle) * currentRadius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
