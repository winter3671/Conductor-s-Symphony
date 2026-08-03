using UnityEngine;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 드럼 "비트 뱅(Beat Bang)"의 확장하는 링 비주얼 전용 이펙트. 피해/넉백은 ExecuteDrums()에서
    // 스폰 즉시 처리하고, 이 컴포넌트는 순수 연출(링이 커지다 사라짐)만 담당한다.
    public class ShockwaveVisualEffect : MonoBehaviour
    {
        private const float Duration = 0.25f;
        private float elapsed;
        private float maxScale;

        public void Initialize(Vector3 pos, float radius, Color color)
        {
            transform.position = pos;
            maxScale = radius * 2f;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteFactory.CreateRingWithCore(32, 12f, 15f, color, Color.clear);
            sr.sortingOrder = 12;
            transform.localScale = Vector3.one * 0.2f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one * maxScale, t);

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
