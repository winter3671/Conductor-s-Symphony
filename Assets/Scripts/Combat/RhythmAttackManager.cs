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

        // 드럼 "상시 비트 오라(Beat Aura)" - 판정 성공 여부와 무관하게 드럼이 장착&언락된 슬롯에 있는 동안
        // 항상 켜져 있는 지속 효과. 정박 타격 시의 "비트 뱅"(넉백 파동)은 다른 악기들과 동일하게
        // InstrumentAttackDispatcher.ExecuteDrums()가 담당하고, 이 오라는 그와 별개로 여기서 처리한다.
        private GameObject drumAuraVisual;
        private bool drumAuraActive;
        private float drumAuraTickTimer;
        private const float DrumAuraTickInterval = 0.5f;
        private const float DrumAuraBaseRadius = 1.6f;

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

            if (drumAuraVisual != null) Destroy(drumAuraVisual);
        }

        private void Update()
        {
            UpdateDrumAura();
        }

        // 판정 성공 이벤트와 완전히 별개로 매 프레임 호출된다 - "드럼이 언락된 슬롯에 장착되어 있는가"만
        // 확인하고, 그렇다면 최근 판정 성공 여부와 무관하게 플레이어 주변에 소량의 지속 타격을 가한다.
        private void UpdateDrumAura()
        {
            bool shouldBeActive = IsDrumsActive();
            if (shouldBeActive != drumAuraActive)
            {
                drumAuraActive = shouldBeActive;
                SetDrumAuraVisualActive(shouldBeActive);
            }

            if (!drumAuraActive) return;
            if (player == null) player = PlayerController.Instance;
            if (player == null) return;

            if (drumAuraVisual != null)
            {
                drumAuraVisual.transform.position = player.transform.position;
            }

            drumAuraTickTimer += Time.deltaTime;
            if (drumAuraTickTimer < DrumAuraTickInterval) return;
            drumAuraTickTimer = 0f;

            int drumLevel = Instrument.InstrumentManager.Instance != null
                ? Instrument.InstrumentManager.Instance.GetInstrumentLevel(Instrument.InstrumentType.Drums)
                : 1;

            // 오라는 판정 성공 여부와 무관한 baseline 효과라 M_rhythm(리듬 정확도 배율)은 의도적으로
            // 적용하지 않는다 - 시포르찬도(M_stat) 패시브만 반영한다. (판정 성공 시의 "비트 뱅" 폭발딜은
            // ExecuteDrums()에서 기존 공식(baseDamage*mRhythm*mStat) 그대로 적용됨 - 이 오라와는 별개)
            // 밸런스 doc 5번 항목 Lv4 "패시브 비트 오라 지속 피해량 +50%" 반영.
            float mStat = Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetDamageMultiplier() : 1.0f;
            float auraLevelMultiplier = (drumLevel >= 4) ? 1.5f : 1f;
            // 드럼 Target DPS 보정 배율(Docs/dps_balance_gap_analysis.md 참고)도 오라에 함께 적용한다 -
            // 이 배율은 "비트 뱅 + 오라" 합산치를 목표 DPS로 역산한 값이라, 비트 뱅에만 적용하면 오라 몫만큼
            // 여전히 목표에 못 미치게 된다.
            float instrumentDpsMultiplier = Instrument.InstrumentDamageTable.GetDamageMultiplier(Instrument.InstrumentType.Drums, drumLevel);
            int auraDamage = Mathf.Max(1, Mathf.RoundToInt(1 * mStat * auraLevelMultiplier * instrumentDpsMultiplier));
            float radius = DrumAuraBaseRadius + 0.1f * Mathf.Max(0, drumLevel - 1);

            EnemyMonster[] enemies = FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(player.transform.position, enemy.transform.position) <= radius)
                {
                    enemy.TakeDamage(auraDamage);
                }
            }
        }

        private bool IsDrumsActive()
        {
            var mgr = Instrument.InstrumentManager.Instance;
            if (mgr == null) return false;

            var equipped = mgr.AcquiredInstruments;
            int maxUnlocked = mgr.GetUnlockedSlotsCount();
            for (int slot = 0; slot < equipped.Count && slot < maxUnlocked; slot++)
            {
                if (equipped[slot].type == Instrument.InstrumentType.Drums) return true;
            }
            return false;
        }

        private void SetDrumAuraVisualActive(bool active)
        {
            if (active)
            {
                if (drumAuraVisual == null)
                {
                    drumAuraVisual = new GameObject("DrumBeatAura");
                    SpriteRenderer sr = drumAuraVisual.AddComponent<SpriteRenderer>();
                    sr.sprite = ProceduralSpriteFactory.CreateRingWithCore(28, 11f, 13f, new Color(0.9f, 0.3f, 0.3f, 0.35f), Color.clear);
                    sr.sortingOrder = 2;
                    drumAuraVisual.transform.localScale = Vector3.one * (DrumAuraBaseRadius * 0.95f);
                }
                drumAuraVisual.SetActive(true);
            }
            else if (drumAuraVisual != null)
            {
                drumAuraVisual.SetActive(false);
            }
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

            // 밸런스 doc 5번 항목의 악기별 Target DPS 보정 (Docs/dps_balance_gap_analysis.md 참고): 위
            // baseDamage(레벨 무관 2~4)만으로는 악기별 실제 타격 빈도를 감안했을 때 목표 DPS의 1~20%에
            // 불과했다. 각 악기 디스패처 내부의 기존 레벨별 비율(관통/범위/발사수 등)은 그대로 두고,
            // 여기서 전체 스케일만 악기·레벨별 배율로 보정한다.
            float instrumentDpsMultiplier = hitInstrument != null
                ? Instrument.InstrumentDamageTable.GetDamageMultiplier(hitInstrument.type, hitInstrument.level)
                : 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat * instrumentDpsMultiplier));
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
            // (설계자 확인/문서화 예정, 2026-08 정리 - 각 악기별 "추가 투사체"의 구체적 의미 정의가 필요함)

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
