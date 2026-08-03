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
        private EnemySpawner spawner;

        private Sprite projectileSprite;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            projectileSprite = ProceduralSpriteFactory.CreateFilledCircle(20, 8f, Color.yellow);
        }

        private void Start()
        {
            player = PlayerController.Instance;
            spawner = EnemySpawner.Instance;

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
            if (spawner == null) spawner = EnemySpawner.Instance;

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

            // 최종 딜량 공식 (game_balance_design.docx section 1): 기본 DPS × M_rhythm × M_stat
            int baseDamage = ((rating == HitRating.Perfect) ? 2 : 1) + extraDamage;
            float mRhythm = RhythmManager.Instance != null ? RhythmManager.Instance.GetRhythmDamageMultiplier() : 1.0f;
            // M_stat = 시포르찬도(Sforzando) 패시브 배율(1.0~1.5). 나머지 7종 패시브는 서로 다른 종류의
            // 스탯(공속/범위/이속/투사체/지속시간/자석범위/방어)이라 하나의 M_stat 숫자로 합쳐지지 않는다.
            float mStat = Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetDamageMultiplier() : 1.0f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat));
            int projCount = 1 + extraProj;

            // 10종 악기별 공격 메커니즘 기획서: 1단계(피아노/벨/마림바/글록켄슈필)는 각자 고유 로직으로 처리하고,
            // 아직 이관되지 않은 나머지 악기는 아래의 기존 범용 투사체 로직으로 계속 폴백한다.
            if (hitInstrument != null && InstrumentAttacks.InstrumentAttackDispatcher.IsImplemented(hitInstrument.type))
            {
                int comboCount = RhythmManager.Instance != null ? RhythmManager.Instance.CurrentCombo : 0;
                InstrumentAttacks.InstrumentAttackDispatcher.Execute(hitInstrument.type, hitInstrument.level, damage, comboCount, spawnPos, projColor);
                return;
            }

            // 2단계: 홀드 기반 4종(바이올린/프렌치호른/첼로/팀파니)은 이 최초 판정 성공 시점(=홀드 시작)에
            // HoldEffectCoordinator로 지속 이펙트를 등록한다. 이후 유지/해제는 OnHoldTickEvent/OnHoldReleasedEvent
            // 구독(HandleHoldTick/HandleHoldReleased)에서 계속 처리한다.
            if (hitInstrument != null && InstrumentAttacks.InstrumentAttackDispatcher.IsHoldImplemented(hitInstrument.type))
            {
                InstrumentAttacks.HoldEffectCoordinator.BeginHold(lane, hitInstrument.type, hitInstrument.level, damage, spawnPos, projColor);
                return;
            }

            // Collect all potential target components (regular trash mobs + boss)
            List<Component> potentialTargets = new List<Component>();
            if (BossMonster.Instance != null) potentialTargets.Add(BossMonster.Instance);

            EnemyMonster[] enemies = FindObjectsByType<EnemyMonster>();
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy != null) potentialTargets.Add(enemy);
                }
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

        private EnemyMonster FindNearestEnemy(Vector3 originPos)
        {
            EnemyMonster[] enemies = FindObjectsByType<EnemyMonster>();
            EnemyMonster nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(originPos, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
