using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 플루트의 실제 소용돌이 본체. FluteVortexHoldEffect가 릴리즈 시점에 스폰하며, 그 이후로는
    // 홀드 코디네이터와 무관하게 독립적으로 살아있다가 지속시간이 끝나면 스스로 파괴된다.
    // 기획서 4번(바람 와류): 순수 CC(군집)용 - 직접 피해는 주지 않고 범위 내 적을 중앙으로 끌어당기기만 한다.
    public class FluteVortexEffect : MonoBehaviour
    {
        private float radius;
        private float pullStrength;
        private float duration;
        private float elapsed;

        public void Initialize(Vector3 pos, int level)
        {
            transform.position = pos;
            radius = 2.0f + 0.2f * Mathf.Max(0, level - 1);       // 레벨당 범위 소폭 증가
            pullStrength = 2.5f;
            duration = 1.5f + 0.2f * Mathf.Max(0, level - 1);      // 레벨당 유지시간 소폭 증가

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color vortexColor = new Color(0.2f, 0.9f, 0.5f, 0.4f); // 문서: 초록빛 바람 장판
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(28, 13f, vortexColor);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector3 toCenter = transform.position - enemy.transform.position;
                float dist = toCenter.magnitude;
                if (dist <= radius && dist > 0.05f)
                {
                    enemy.transform.position += toCenter.normalized * pullStrength * Time.deltaTime;
                }
            }
        }
    }
}
