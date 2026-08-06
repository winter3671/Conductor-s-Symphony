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
        private float minScale;
        private float maxScale;

        // radius = 실제 판정 반경(월드 유닛). CreateUnitRing은 scale=1일 때 링 바깥쪽 끝이 정확히
        // 반지름 1 유닛이 되도록 만들어져 있어서, localScale에 그대로 radius를 곱하면 실제 공격
        // 판정 범위와 시각적으로 정확히 일치한다(기존엔 radius*2f라는 근사값을 썼는데, 스프라이트
        // 내부 픽셀 비율까지 감안하지 않아 실제 판정 범위보다 눈에 띄게 작게 그려지고 있었다).
        public void Initialize(Vector3 pos, float radius, Color color)
        {
            transform.position = pos;
            minScale = radius * 0.15f;
            maxScale = radius;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.95f, 1f, color);
            sr.sortingOrder = 12;
            transform.localScale = Vector3.one * minScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            transform.localScale = Vector3.Lerp(Vector3.one * minScale, Vector3.one * maxScale, t);

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
