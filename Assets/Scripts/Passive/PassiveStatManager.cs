using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Passive
{
    // game_balance_design.docx section 4: 인게임 패시브 스탯(장신구) 8종 관리.
    // InstrumentManager와 달리 슬롯 제한이 없다 - 8종 전부 원하는 만큼(최대 Lv5씩) 모을 수 있다.
    public class PassiveStatManager : MonoSingleton<PassiveStatManager>
    {
        private readonly List<PassiveStatInfo> acquired = new List<PassiveStatInfo>();

        public IReadOnlyList<PassiveStatInfo> AcquiredPassives => acquired;

        public static event System.Action<List<PassiveStatInfo>> OnPassivesChangedEvent;

        public bool HasPassive(PassiveStatType type)
        {
            return acquired.Exists(p => p.type == type);
        }

        public bool IsMaxLevel(PassiveStatType type)
        {
            return GetLevel(type) >= PassiveStatDatabase.MaxLevel;
        }

        public int GetLevel(PassiveStatType type)
        {
            PassiveStatInfo info = acquired.Find(p => p.type == type);
            return info != null ? info.level : 0;
        }

        public void AcquireOrUpgrade(PassiveStatType type)
        {
            PassiveStatInfo existing = acquired.Find(p => p.type == type);
            if (existing != null)
            {
                existing.UpgradeLevel();
            }
            else
            {
                acquired.Add(new PassiveStatInfo(type, 1));
            }

            ApplyOnAcquireSideEffects(type);
            OnPassivesChangedEvent?.Invoke(new List<PassiveStatInfo>(acquired));
        }

        // 획득/레벨업 그 순간에 다른 시스템에 즉시 반영해야 하는 부수효과 (예: 최대 HP 증가분 즉시 채우기).
        // 매 프레임 재계산되는 나머지 효과(GetXxxMultiplier 등)와 달리, Tuning의 최대 HP만은
        // PlayerController가 내부적으로 "베이스 HP 대비 보너스"를 스냅샷으로 들고 있어야 해서 이벤트로 통지한다.
        private void ApplyOnAcquireSideEffects(PassiveStatType type)
        {
            if (type == PassiveStatType.Tuning && PlayerController.Instance != null)
            {
                PlayerController.Instance.RefreshMaxHealthBonus(GetMaxHealthBonusFraction());
            }
        }

        // ---- 문서 1번 M_stat 자리에 그대로 사용: 시포르찬도 = 유일하게 딜량에 직접 영향을 주는 패시브 ----
        public float GetDamageMultiplier()
        {
            int lv = GetLevel(PassiveStatType.Sforzando);
            return 1.0f + 0.10f * lv; // Lv0=1.0x, Lv5(MAX)=1.5x
        }

        public float GetMoveSpeedMultiplier()
        {
            int lv = GetLevel(PassiveStatType.Vivace);
            return 1.0f + 0.08f * lv; // Lv5=1.4x
        }

        // Legato: Lv3에 +1, Lv5에 +1 더 (최대 +2). 문서 그대로 계단식 지급, 선형 아님에 주의.
        public int GetExtraProjectiles()
        {
            int lv = GetLevel(PassiveStatType.Legato);
            int bonus = 0;
            if (lv >= 3) bonus += 1;
            if (lv >= 5) bonus += 1;
            return bonus;
        }

        public float GetPickupRangeMultiplier()
        {
            int lv = GetLevel(PassiveStatType.Resonance);
            return 1.0f + 0.25f * lv; // Lv5=2.25x
        }

        public float GetDamageReductionFraction()
        {
            int lv = GetLevel(PassiveStatType.Tuning);
            return Mathf.Min(0.25f, 0.05f * lv); // Lv5=0.25 (최대 -25%)
        }

        public float GetMaxHealthBonusFraction()
        {
            int lv = GetLevel(PassiveStatType.Tuning);
            return Mathf.Min(0.50f, 0.10f * lv); // Lv5=0.50 (최대 +50%)
        }

        // ---- 아래 3종은 대상 시스템이 아직 없음 (10종 악기별 고유 메커니즘 구현 전) ----
        // 값 계산 자체는 정상 동작하므로, 나중에 악기 고유 공격 로직(쿨타임/범위/장판 지속시간)을
        // 만들 때 이 getter들을 그대로 가져다 곱해서 쓰면 된다. 지금은 소비하는 쪽이 없을 뿐이다.

        // Allegro: "공격 속도/쿨타임 감축" → 쿨타임에 곱하는 감축 비율(0~0.30)로 정의.
        // 실제 쿨타임 필드가 생기면 finalCooldown = baseCooldown * (1f - GetCooldownReductionFraction()) 형태로 사용.
        public float GetCooldownReductionFraction()
        {
            int lv = GetLevel(PassiveStatType.Allegro);
            return Mathf.Min(0.30f, 0.06f * lv); // Lv5=0.30 (최대 -30%)
        }

        public float GetRangeMultiplier()
        {
            int lv = GetLevel(PassiveStatType.Crescendo);
            return 1.0f + 0.10f * lv; // Lv5=1.5x
        }

        public float GetDurationMultiplier()
        {
            int lv = GetLevel(PassiveStatType.Fermata);
            return 1.0f + 0.15f * lv; // Lv5=1.75x
        }
    }
}
