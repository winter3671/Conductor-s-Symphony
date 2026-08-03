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
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent -= HandleRhythmHit;
            }
        }

        public void HandleRhythmHit(HitRating rating, RhythmLane lane)
        {
            if (player == null) player = PlayerController.Instance;
            if (spawner == null) spawner = EnemySpawner.Instance;

            Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;

            int slotIdx = RhythmManager.GetSlotForLane(lane);
            if (Instrument.InstrumentManager.Instance != null && slotIdx < Instrument.InstrumentManager.Instance.AcquiredInstruments.Count)
            {
                var inst = Instrument.InstrumentManager.Instance.AcquiredInstruments[slotIdx];
                if (Audio.AudioLayerManager.Instance != null)
                {
                    Audio.AudioLayerManager.Instance.PlayInstrumentKeySound(inst.type, rating == HitRating.Perfect);
                }
            }

            Sprite projSprite = projectileSprite;
            Color projColor = (rating == HitRating.Perfect) ? Color.yellow : Color.cyan;

            int extraDamage = Instrument.InstrumentManager.Instance != null ? Instrument.InstrumentManager.Instance.GetTotalExtraDamage() : 0;
            int extraProj = Instrument.InstrumentManager.Instance != null ? Instrument.InstrumentManager.Instance.GetTotalExtraProjectiles() : 0;

            // 최종 딜량 공식 (game_balance_design.docx section 1): 기본 DPS × M_rhythm × M_stat
            int baseDamage = ((rating == HitRating.Perfect) ? 2 : 1) + extraDamage;
            float mRhythm = RhythmManager.Instance != null ? RhythmManager.Instance.GetRhythmDamageMultiplier() : 1.0f;
            const float mStat = 1.0f; // TODO(balance): 패시브 스탯 강화 시스템(문서 4번) 구현 후 실제 강화도(0~100%) 값으로 교체
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat));
            int projCount = 1 + extraProj;

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
