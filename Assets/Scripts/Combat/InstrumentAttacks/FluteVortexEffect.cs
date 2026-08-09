using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;
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

        // 2026-08-08: 손그림 정지 이미지 1장(Assets/Resources/Sprites/Effects/Vortex/Vortex.png - 동심원
        // 형태라 회전시켜도 완전 대칭이라 티가 안 남. 영상/프레임 제작 없이 이 그림 한 장만 쓰기로 사용자
        // 결정). 대신 코드에서 살짝 커졌다 작아지는 "숨쉬듯" 펄스로 밋밋함만 보완한다.
        private static Sprite vortexSprite;
        private static bool triedLoadVortexSprite = false;
        private SpriteRenderer fieldSr;
        private Transform fieldArtTransform;
        private float baseArtScale;
        private const float PulseSpeed = 2.2f;   // 라디안/초
        private const float PulseAmount = 0.06f; // 기준 크기 대비 ±6%

        // 첼로와 동일한 이유(콘텐츠 크기가 다른 손그림 아트를 기존 매직넘버(radius*0.9)에 그대로 곱하면
        // 이중 확대)로, 아트를 별도 자식 오브젝트로 분리해 콘텐츠 크기 기반으로 독립 계산한다.
        private const float ReferenceContentSize = 0.28f; // 기존 CreateFilledCircle(28,13f,...) 풀캔버스 bounds(28px/100)
        private const float ArtVisualScale = 1f; // 순수 시각 배율 - 실측 후 작아 보이면 이 값만 올리면 됨

        public void Initialize(Vector3 pos, int level)
        {
            transform.position = pos;
            // 2026-08-09: 레벨별 배율/수치를 InstrumentLevelStats로 데이터화(순수 추출, 값 변경 없음).
            // Lv3+: 흡입 범위 +50% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 2.0f * InstrumentLevelStats.GetRangeMultiplier(InstrumentType.Flute, level) * CombatTargetingUtility.GetRangeMultiplier();
            pullStrength = 2.5f * InstrumentLevelStats.GetRangeMultiplier(InstrumentType.Flute, level); // Lv3+: 당기는 힘 +50%(범위와 동일 배율표 재사용)
            // Lv2+: 유지시간 +40% × 페르마타(Fermata) 패시브 "지속시간 증가"(2026-08-06)
            duration = 1.5f * InstrumentLevelStats.GetDurationMultiplier(InstrumentType.Flute, level) * CombatTargetingUtility.GetDurationMultiplier();
            explodeOnExpire = level >= 5;                       // Lv5: 소멸 시 바람 파편 폭발

            EnsureVortexSprite();

            GameObject fieldArtObj = new GameObject("FluteFieldArt");
            fieldArtObj.transform.SetParent(transform, false);
            fieldArtTransform = fieldArtObj.transform;
            fieldSr = fieldArtObj.AddComponent<SpriteRenderer>();
            // 색상 틴트 없이(아트 자체의 초록 톤 유지) 알파만 곱해 기존처럼 반투명하게 유지.
            fieldSr.color = new Color(1f, 1f, 1f, 0.4f);
            fieldSr.sortingOrder = 3;
            Color fallbackColor = new Color(0.2f, 0.9f, 0.5f, 1f); // 문서: 초록빛 바람 장판
            Sprite initialSprite = vortexSprite != null ? vortexSprite : ProceduralSpriteFactory.CreateFilledCircle(28, 13f, fallbackColor);
            ApplyArt(initialSprite);

            // 실제 흡입 반경(radius)을 정확히 표시하는 얇은 테두리 링. 드럼 오라 링과 동일하게 아주
            // 얇게(0.985~1.0) 설정(2026-08-07, 사용자 결정). 루트가 identity라 radius를 직접 곱하면 됨.
            GameObject rangeRingObj = new GameObject("FluteRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(0.2f, 0.9f, 0.5f, 0.85f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * radius;

            int maxConcurrent = InstrumentLevelStats.GetStepCount(InstrumentType.Flute, level); // Lv4+: 동시 2개까지 유지 가능
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

        private static void EnsureVortexSprite()
        {
            if (triedLoadVortexSprite) return;
            triedLoadVortexSprite = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/Vortex");
            if (loaded != null && loaded.Length > 0)
            {
                vortexSprite = loaded[0]; // 정지 이미지 1장만 사용
            }
        }

        private void ApplyArt(Sprite sprite)
        {
            if (sprite == null || fieldSr == null || fieldArtTransform == null) return;
            fieldSr.sprite = sprite;
            float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float targetDiameter = radius * 0.9f * ReferenceContentSize * ArtVisualScale;
            baseArtScale = (maxDim > 0.0001f) ? (targetDiameter / maxDim) : targetDiameter;
            fieldArtTransform.localScale = Vector3.one * baseArtScale;
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

            // 동심원이라 회전은 시각적으로 티가 안 나서(완전 대칭), 대신 살짝 커졌다 작아지는 펄스로
            // 정지 이미지 한 장만으로도 "숨쉬는" 느낌을 준다. 판정 반경(radius)에는 영향 없음(아트 크기만).
            if (fieldArtTransform != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * PulseSpeed) * PulseAmount;
                fieldArtTransform.localScale = Vector3.one * (baseArtScale * pulse);
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
