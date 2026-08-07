using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 플루트의 실제 소용돌이 본체. FluteVortexHoldEffect가 릴리즈 시점에 스폰하며, 그 이후로는
    // 홀드 코디네이터와 무관하게 독립적으로 살아있다가 지속시간이 끝나면 스스로 파괴된다.
    // 기획서 4번(바람 와류): 순수 CC(군집)용 - 직접 피해는 주지 않고 범위 내 적을 중앙으로 끌어당기기만 한다.
    // 레벨별 수치는 밸런스 doc(game_balance_design.docx) 5번 항목 반영: Lv2 유지시간+40% / Lv3 흡입범위·
    // 당기는힘+50% / Lv4 동시 유지 가능 소용돌이 +1개(총 2개) / Lv5 소멸 시 바람 파편(피해 없는 외곽 넉백) 폭발.
    public class FluteVortexEffect : MonoBehaviour
    {
        private float radius;
        private float pullStrength;
        private float duration;
        private float elapsed;
        private bool explodeOnExpire;

        // Lv4("동시 유지 가능 소용돌이 개수 +1") - 이 캡을 넘기면 가장 오래된 소용돌이를 강제 정리한다.
        private static readonly List<FluteVortexEffect> activeVortices = new List<FluteVortexEffect>();

        public void Initialize(Vector3 pos, int level)
        {
            transform.position = pos;
            // Lv3+: 흡입 범위 +50% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 2.0f * (level >= 3 ? 1.5f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            pullStrength = 2.5f * (level >= 3 ? 1.5f : 1f);    // Lv3+: 당기는 힘 +50%
            // Lv2+: 유지시간 +40% × 페르마타(Fermata) 패시브 "지속시간 증가"(2026-08-06)
            duration = 1.5f * (level >= 2 ? 1.4f : 1f) * CombatTargetingUtility.GetDurationMultiplier();
            explodeOnExpire = level >= 5;                       // Lv5: 소멸 시 바람 파편 폭발

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color vortexColor = new Color(0.2f, 0.9f, 0.5f, 0.4f); // 문서: 초록빛 바람 장판
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(28, 13f, vortexColor);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);

            // 실제 흡입 반경(radius)을 정확히 표시하는 얇은 테두리 링 - 첼로와 동일한 이유로 채워진
            // 원만으로는 실제 반경의 약 11.7%로만 보인다. 부모 스케일(radius*0.9)을 상쇄하는 로컬
            // 스케일(1/0.9)을 곱해 정확히 맞춘다. 드럼 오라 링과 동일하게 아주 얇게(0.985~1.0) 설정
            // (2026-08-07, 사용자 결정).
            GameObject rangeRingObj = new GameObject("FluteRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(0.2f, 0.9f, 0.5f, 0.85f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * (1f / 0.9f);

            int maxConcurrent = (level >= 4) ? 2 : 1; // Lv4+: 동시 2개까지 유지 가능
            activeVortices.Add(this);
            while (activeVortices.Count > maxConcurrent)
            {
                FluteVortexEffect oldest = activeVortices[0];
                activeVortices.RemoveAt(0);
                if (oldest != null && oldest != this)
                {
                    Destroy(oldest.gameObject);
                }
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                if (explodeOnExpire) ExplodeWindShard();
                Destroy(gameObject);
                return;
            }

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
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

        // Lv5: 소멸 순간 범위 내 적을 바깥으로 밀어내는 "바람 파편" 연출. 기존 플루트 설계(무피해 CC)를
        // 그대로 유지해 피해는 주지 않는다.
        private void ExplodeWindShard()
        {
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;

                Vector3 away = enemy.transform.position - transform.position;
                float dist = away.magnitude;
                if (dist <= radius * 1.2f && dist > 0.05f)
                {
                    enemy.transform.position += away.normalized * 0.8f;
                }
            }
        }

        private void OnDestroy()
        {
            activeVortices.Remove(this);
        }
    }
}
