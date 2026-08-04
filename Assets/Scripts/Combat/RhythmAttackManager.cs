using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Rhythm;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat
{
    public class RhythmAttackManager : MonoSingleton<RhythmAttackManager>
    {
        private PlayerController player;

        private Sprite projectileSprite;

        // 드럼 "상시 비트 오라"는 판정 디스패치와 무관한 별도 책임이라 DrumAuraController로 분리했다
        // (리팩토링 배경은 DrumAuraController.cs 상단 주석 참고). MonoSingleton으로 만들지 않고 여기서
        // 자식 GameObject로 직접 생성한다 - 씬 파일에 수동 배치가 필요 없다.
        private DrumAuraController drumAuraController;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            projectileSprite = ProceduralSpriteFactory.CreateFilledCircle(20, 8f, Color.yellow);
        }

        private void Start()
        {
            player = PlayerController.Instance;

            GameObject auraObj = new GameObject("DrumAuraController");
            auraObj.transform.SetParent(transform);
            drumAuraController = auraObj.AddComponent<DrumAuraController>();

            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent += HandleRhythmHit;
            }

            // 2단계(홀드 기반 4종: 바이올린/프렌치호른/첼로/팀파니)의 유지/해제 처리.
            // 홀드 "시작"은 OnHitSuccessEvent(HandleRhythmHit)에서 이미 처리하므로 여기선 유지/해제만 구독한다.
            RhythmManager.OnHoldTickEvent += HandleHoldTick;
            RhythmManager.OnHoldReleasedEvent += HandleHoldReleased;
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent -= HandleRhythmHit;
            }

            RhythmManager.OnHoldTickEvent -= HandleHoldTick;
            RhythmManager.OnHoldReleasedEvent -= HandleHoldReleased;
        }

        private void HandleHoldTick(RhythmLane lane)
        {
            InstrumentAttacks.HoldEffectCoordinator.Tick(lane, Time.deltaTime);
        }

        private void HandleHoldReleased(RhythmLane lane, float progress01, bool completedFully)
        {
            InstrumentAttacks.HoldEffectCoordinator.Release(lane, completedFully);
        }

        public void HandleRhythmHit(HitRating rating, RhythmLane lane)
        {
            if (player == null) player = PlayerController.Instance;

            Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;

            int slotIdx = RhythmManager.GetSlotForLane(lane);
            Instrument.InstrumentInfo hitInstrument = null;
            if (Instrument.InstrumentManager.Instance != null && slotIdx < Instrument.InstrumentManager.Instance.AcquiredInstruments.Count)
            {
                hitInstrument = Instrument.InstrumentManager.Instance.AcquiredInstruments[slotIdx];
                if (Audio.AudioLayerManager.Instance != null)
                {
                    Audio.AudioLayerManager.Instance.PlayInstrumentKeySound(hitInstrument.type, rating == HitRating.Perfect);
                }
            }

            Sprite projSprite = projectileSprite;
            Color projColor = (rating == HitRating.Perfect) ? Color.yellow : Color.cyan;

            int extraDamage = Instrument.InstrumentManager.Instance != null ? Instrument.InstrumentManager.Instance.GetTotalExtraDamage() : 0;
            int extraProj = Instrument.InstrumentManager.Instance != null ? Instrument.InstrumentManager.Instance.GetTotalExtraProjectiles() : 0;
            // 레가토(Legato) 패시브: 투사체 수 +1(Lv3), +1(Lv5) 추가 지급
            extraProj += Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetExtraProjectiles() : 0;

            // 최종 딜량 공식 (game_balance_design.docx section 1: 기본 DPS × M_rhythm × M_stat) +
            // 악기별 DPS 보정 배율(Docs/dps_balance_gap_analysis.md) - 계산 자체는 DamageFormula로 분리됨.
            int baseDamage = DamageFormula.ComputeBaseDamage(rating, extraDamage);
            float mRhythm = RhythmManager.Instance != null ? RhythmManager.Instance.GetRhythmDamageMultiplier() : 1.0f;
            // M_stat = 시포르찬도(Sforzando) 패시브 배율(1.0~1.5). 나머지 7종 패시브는 서로 다른 종류의
            // 스탯(공속/범위/이속/투사체/지속시간/자석범위/방어)이라 하나의 M_stat 숫자로 합쳐지지 않는다.
            float mStat = Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetDamageMultiplier() : 1.0f;
            float instrumentDpsMultiplier = hitInstrument != null
                ? Instrument.InstrumentDamageTable.GetDamageMultiplier(hitInstrument.type, hitInstrument.level)
                : 1f;
            int damage = DamageFormula.ComputeFinalDamage(baseDamage, mRhythm, mStat, instrumentDpsMultiplier);
            int projCount = 1 + extraProj;

            // 10종 악기별 공격 메커니즘 기획서: 탭+오토타겟 5종(피아노/벨/마림바/글록켄슈필/드럼)은 여기서,
            if (hitInstrument != null && InstrumentAttacks.InstrumentAttackDispatcher.IsImplemented(hitInstrument.type))
            {
                int comboCount = RhythmManager.Instance != null ? RhythmManager.Instance.CurrentCombo : 0;
                InstrumentAttacks.InstrumentAttackDispatcher.Execute(hitInstrument.type, hitInstrument.level, damage, comboCount, spawnPos, projColor);
                return;
            }

            // 홀드 기반 5종(바이올린/프렌치호른/첼로/팀파니/플루트)은 이 최초 판정 성공 시점(=홀드 시작)에
            // HoldEffectCoordinator로 지속 이펙트를 등록한다. 이후 유지/해제는 OnHoldTickEvent/OnHoldReleasedEvent
            // 구독(HandleHoldTick/HandleHoldReleased)에서 계속 처리한다.
            if (hitInstrument != null && InstrumentAttacks.InstrumentAttackDispatcher.IsHoldImplemented(hitInstrument.type))
            {
                InstrumentAttacks.HoldEffectCoordinator.BeginHold(lane, hitInstrument.type, hitInstrument.level, damage, spawnPos, projColor);
                return;
            }

            // 아래는 10종 악기별 전용 디스패처가 생기기 전부터 있던 범용 투사체 폴백 로직이다.
            // 현재는 위 두 분기(IsImplemented/IsHoldImplemented)가 10종 전체를 커버하므로 실질적으로
            // 도달하지 않지만(신규 악기 추가 시를 대비한 안전장치), 레가토(Legato) 패시브의 "투사체 수 +1/+2"와
            // 악기 Lv4의 extraProjectiles("Multi +1")를 실제로 소비하는 유일한 코드이기도 하다 - 즉 지금
            // 이 두 스탯은 사실상 여기서만 의미를 가지며, 개별 악기 디스패처들은 이 값을 받지 않는다.
            // (설계자 확인/문서화 예정 - Docs/team_review_needed.md 1-1 참고. 이 로직을 삭제할지는 그
            // 항목이 정리된 뒤에 다시 판단하기로 함 - 리팩토링 대상에서 의도적으로 제외)

            // Collect all potential target components (regular trash mobs + boss)
            List<Component> potentialTargets = new List<Component>();
            if (BossMonster.Instance != null) potentialTargets.Add(BossMonster.Instance);

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy != null) potentialTargets.Add(enemy);
            }

            if (potentialTargets.Count == 0)
            {
                // Fire default single projectile forward if no targets
                GameObject projObj = new GameObject($"Proj_{Time.frameCount}");
                AttackProjectile proj = projObj.AddComponent<AttackProjectile>();
                proj.Initialize(null, spawnPos, projSprite, projColor, damage);
                return;
            }

            // Sort targets by distance to player
            potentialTargets.Sort((a, b) => Vector3.Distance(spawnPos, a.transform.position).CompareTo(Vector3.Distance(spawnPos, b.transform.position)));

            for (int i = 0; i < Mathf.Min(projCount, potentialTargets.Count); i++)
            {
                GameObject projObj = new GameObject($"Proj_{i}_{Time.frameCount}");
                AttackProjectile proj = projObj.AddComponent<AttackProjectile>();
                proj.Initialize(potentialTargets[i], spawnPos, projSprite, projColor, damage);
            }
        }
    }
}
