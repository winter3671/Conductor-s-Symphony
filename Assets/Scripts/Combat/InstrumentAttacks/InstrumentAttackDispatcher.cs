using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // "10종 악기별 공격 메커니즘 기획서" 전체 구현 완료: 탭+오토타겟 5종(피아노/벨/마림바/글록켄슈필/드럼)은
    // IsImplemented()/Execute()로, 홀드 기반 5종(바이올린/프렌치호른/첼로/팀파니/플루트)은
    // IsHoldImplemented()/CreateHoldEffect()로 각각 처리한다. 드럼의 "상시 비트 오라"만은 판정 성공과
    // 무관한 지속 효과라 여기가 아니라 RhythmAttackManager.UpdateDrumAura()가 별도로 담당한다.
    // RhythmAttackManager의 기존 범용 투사체 폴백 로직은 이제 어떤 악기에도 도달하지 않는다.
    public static class InstrumentAttackDispatcher
    {
        private static Sprite beamSprite;
        private static Sprite starSprite;

        public static bool IsImplemented(InstrumentType type)
        {
            return type == InstrumentType.Piano
                || type == InstrumentType.Bell
                || type == InstrumentType.Marimba
                || type == InstrumentType.Glockenspiel
                || type == InstrumentType.Drums;
        }

        // 2단계: 홀드 기반 4종(바이올린/프렌치호른/첼로/팀파니). 탭 4종과 달리 HoldEffectCoordinator를 통해
        // 지속 이펙트(IHoldAttackEffect)로 처리되며, 여기서는 "해당 타입이 홀드 이펙트를 갖는지"만 판별한다.
        // 3단계에서 플루트(숏 홀드 - 릴리즈 시 미니 소용돌이)도 이 방식으로 추가됐다.
        public static bool IsHoldImplemented(InstrumentType type)
        {
            return type == InstrumentType.Violin
                || type == InstrumentType.FrenchHorn
                || type == InstrumentType.Cello
                || type == InstrumentType.Timpani
                || type == InstrumentType.Flute;
        }

        // HoldEffectCoordinator가 홀드 시작 시 호출 - 악기 타입에 맞는 지속 이펙트 컴포넌트를 생성해 반환한다.
        public static IHoldAttackEffect CreateHoldEffect(InstrumentType type)
        {
            GameObject obj = new GameObject($"HoldEffect_{type}");
            switch (type)
            {
                case InstrumentType.Violin: return obj.AddComponent<ViolinOrbitEffect>();
                case InstrumentType.FrenchHorn: return obj.AddComponent<FrenchHornConeEffect>();
                case InstrumentType.Cello: return obj.AddComponent<CelloGravityFieldEffect>();
                case InstrumentType.Timpani: return obj.AddComponent<TimpaniBombardmentEffect>();
                case InstrumentType.Flute: return obj.AddComponent<FluteVortexHoldEffect>();
                default:
                    Object.Destroy(obj);
                    return null;
            }
        }

        public static void Execute(InstrumentType type, int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnsureSprites();

            switch (type)
            {
                case InstrumentType.Piano: ExecutePiano(level, damage, currentCombo, origin, color); break;
                case InstrumentType.Bell: ExecuteBell(level, damage, origin, color); break;
                case InstrumentType.Marimba: ExecuteMarimba(level, damage, origin, color); break;
                case InstrumentType.Glockenspiel: ExecuteGlockenspiel(level, damage, currentCombo, origin, color); break;
                case InstrumentType.Drums: ExecuteDrums(level, damage, origin, color); break;
            }
        }

        // ---- 5. 드럼: 정박(1,5,9,13) 타격 시 플레이어 중심 360도 비트 뱅(넉백 파동) ----
        // 밸런스 doc(game_balance_design.docx) 5번 항목: Lv2 충격파 피해량·범위 +20% / Lv3 넉백 거리 2배 +
        // 0.5초 둔화(둔화 배율 자체는 doc에 수치가 없어 50%로 가정) / Lv5 2연속 중첩 충격파.
        // (Lv4 "비트 오라 지속 피해량 +50%"는 여기가 아니라 RhythmAttackManager.UpdateDrumAura()가 담당)
        private static void ExecuteDrums(int level, int damage, Vector3 origin, Color color)
        {
            float radius = 2.0f * (level >= 2 ? 1.2f : 1f);                                       // Lv2+: 범위 +20%
            int shockwaveDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.2f : 1f))); // Lv2+: 피해량 +20%
            float knockbackImpulse = 0.6f * (level >= 3 ? 2f : 1f);                                // Lv3+: 넉백 거리 2배
            bool applySlow = level >= 3;                                                            // Lv3+: 0.5초 둔화
            int shockwaveCount = (level >= 5) ? 2 : 1;                                              // Lv5: 2연속 중첩 충격파

            for (int i = 0; i < shockwaveCount; i++)
            {
                FireBeatBangShockwave(origin, radius, shockwaveDamage, knockbackImpulse, applySlow, color);
            }
        }

        private static void FireBeatBangShockwave(Vector3 origin, float radius, int damage, float knockbackImpulse, bool applySlow, Color color)
        {
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > radius) continue;

                enemy.TakeDamage(damage);
                if (dist > 0.01f)
                {
                    enemy.transform.position += toEnemy.normalized * knockbackImpulse;
                }
                if (applySlow)
                {
                    enemy.ApplyTemporarySlow(0.5f, 0.5f); // 50% 감속, 0.5초 (감속 배율은 doc에 명시 없어 임의값)
                }
            }

            if (BossMonster.Instance != null && Vector3.Distance(origin, BossMonster.Instance.transform.position) <= radius)
            {
                BossMonster.Instance.TakeDamage(damage);
            }

            GameObject ringObj = new GameObject("DrumBeatBang");
            ShockwaveVisualEffect ring = ringObj.AddComponent<ShockwaveVisualEffect>();
            ring.Initialize(origin, radius, color);
        }

        private static void EnsureSprites()
        {
            if (beamSprite == null) beamSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 7f, Color.white);
            if (starSprite == null) starSprite = ProceduralSpriteFactory.CreateDiamond(20, 9f, Color.white);
        }

        // ---- 1. 피아노: 가장 가까운 적 방향 건반 관통 레이저 ----
        // 밸런스 doc 5번 항목: Lv2 피해량 +25% / Lv3 관통 +2 / Lv4 발사 수 +1 / Lv5 6연타 성공 시 폭포 추가 발사
        private static void ExecutePiano(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            Vector3 dir = nearest != null ? (nearest.transform.position - origin) : Vector3.up;

            int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.25f : 1f))); // Lv2+: 피해량 +25%
            int pierce = 2 + (level >= 3 ? 2 : 0); // Lv1~2: 2관통, Lv3+: 4관통
            int shots = 1 + (level >= 4 ? 1 : 0);  // Lv1~3: 1발, Lv4+: 2발

            for (int i = 0; i < shots; i++)
            {
                SpawnBeam(origin, dir, scaledDamage, pierce, maxRange: 9f, bounce: false, color);
            }

            // Lv5: 6연타 성공마다("건반 폭포") 부채꼴로 3발 추가 발사
            if (level >= 5 && currentCombo > 0 && currentCombo % 6 == 0)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 spreadDir = Quaternion.Euler(0f, 0f, i * 12f) * dir;
                    SpawnBeam(origin, spreadDir, scaledDamage, pierce, 9f, false, color);
                }
            }
        }

        // ---- 2. 벨: 가장 가까운 적 중심 8방향 성광(星光) ----
        // 밸런스 doc 5번 항목: Lv2 사거리 +30%(1회성 - 레벨마다 복리 아님) / Lv3 관통력 강화 / Lv4 8방향
        // 2연속 발사 / Lv5 "지나간 자리가 1.5초간 빛나며 경로상 적 지속 타격" → 중심점에 잔향 장판 추가.
        private static void ExecuteBell(int level, int damage, Vector3 origin, Color color)
        {
            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            Vector3 center = nearest != null ? nearest.transform.position : origin;

            float range = 2.5f * (level >= 2 ? 1.3f : 1f); // Lv2+: 사거리 +30% (1회성 적용)
            int pierce = 3 + (level >= 3 ? 2 : 0);
            int bursts = (level >= 4) ? 2 : 1; // Lv4+: 8방향 섬광 2연속 발사

            for (int b = 0; b < bursts; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, 0f, i * 45f) * Vector3.right;
                    SpawnBeam(center, dir, damage, pierce, range, false, color);
                }
            }

            if (level >= 5)
            {
                int tickDamage = Mathf.Max(1, damage / 2);
                GameObject glowObj = new GameObject("BellAfterglow");
                LingeringZoneEffect glow = glowObj.AddComponent<LingeringZoneEffect>();
                glow.Initialize(center, radius: 1.0f, tickDamage, tickInterval: 0.3f, duration: 1.5f, color);
            }
        }

        // ---- 3. 마림바: 이동 방향 일직선 목재 파동 ----
        // 밸런스 doc 5번 항목: Lv2 관통 +2 / Lv3 파동 크기 +30%(오프비트 여부 구분 없이 레벨 조건만으로 적용
        // - 이전 버전엔 이 분기가 아예 빠져있던 버그였음, 이번에 추가) / Lv4 화면 끝 1회 바운스 /
        // Lv5 피격 시 이속 30% 감소 + 밀쳐냄.
        private static void ExecuteMarimba(int level, int damage, Vector3 origin, Color color)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            int pierce = 3 + (level >= 2 ? 2 : 0);
            bool bounce = level >= 4;
            float sizeMultiplier = (level >= 3) ? 1.3f : 1f; // Lv3+: 파동 크기 +30% (시각 크기 + 히트 반경 모두)

            System.Action<EnemyMonster, Vector3> onHit = null;
            if (level >= 5)
            {
                onHit = (enemy, hitPos) =>
                {
                    enemy.ApplyTemporarySlow(0.7f, 1.0f); // 이속 30% 감소, 1초간 (지속시간은 doc에 수치 없어 임의값)
                    Vector3 push = enemy.transform.position - origin;
                    if (push.sqrMagnitude > 0.0001f)
                    {
                        enemy.transform.position += push.normalized * 0.5f; // 밀쳐냄
                    }
                };
            }

            SpawnBeam(origin, facing, damage, pierce, maxRange: 10f, bounce, color, sizeMultiplier, onHit);
        }

        // ---- 4. 글록켄슈필: 체력이 가장 높은 적 머리 위 별빛 낙하 ----
        // 밸런스 doc 5번 항목: Lv2 피해량 +30% / Lv3 스플래시 추가 / Lv4 버스트 성공 시 별빛 수 +2(항상) /
        // Lv5 2차 유도 파편 폭발 + 0.5초 기절.
        private static void ExecuteGlockenspiel(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster target = CombatTargetingUtility.GetHighestHpEnemy(origin);
            Vector3 pos = target != null ? target.transform.position : origin;

            int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.3f : 1f))); // Lv2+: 피해량 +30%
            float splashRadius = 0.6f + (level >= 3 ? 0.5f : 0f);
            SpawnImpact(pos, 0.15f, splashRadius, scaledDamage, color);

            bool burstReady = level >= 4 && currentCombo > 0 && currentCombo % 4 == 0;
            if (burstReady)
            {
                const int extraStars = 2; // Lv4+: 버스트 성공 시 별빛 수 +2 (doc 명시대로 항상 +2)
                for (int i = 0; i < extraStars; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), 0f);
                    SpawnImpact(pos + offset, 0.15f, splashRadius, scaledDamage, color);
                }
            }

            // Lv5: 2차 유도 파편 - 가장 가까운 "다른" 적을 향해 유도 투사체 발사, 명중 시 피해+0.5초 기절
            if (level >= 5)
            {
                EnemyMonster secondaryTarget = FindSecondaryShrapnelTarget(target, origin);
                if (secondaryTarget != null)
                {
                    GameObject shrapnelObj = new GameObject("GlockenspielShrapnel");
                    HomingShrapnelProjectile shrapnel = shrapnelObj.AddComponent<HomingShrapnelProjectile>();
                    shrapnel.Initialize(pos, secondaryTarget, scaledDamage, speed: 10f, stunDuration: 0.5f, starSprite, color);
                }
            }
        }

        private static EnemyMonster FindSecondaryShrapnelTarget(EnemyMonster primaryTarget, Vector3 origin)
        {
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            EnemyMonster best = null;
            float bestDist = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy == primaryTarget) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }
            return best;
        }

        private static void SpawnBeam(Vector3 start, Vector3 dir, int damage, int pierce, float maxRange, bool bounce, Color color, float sizeMultiplier = 1f, System.Action<EnemyMonster, Vector3> onHitEnemy = null)
        {
            GameObject obj = new GameObject("InstrumentBeam");
            PiercingBeamProjectile beam = obj.AddComponent<PiercingBeamProjectile>();
            beam.Initialize(start, dir, speed: 14f, damage, pierce, maxRange, bounce, beamSprite, color, visualLength: 1.1f, sizeMultiplier: sizeMultiplier, onHitEnemy: onHitEnemy);
        }

        private static void SpawnImpact(Vector3 pos, float delay, float radius, int damage, Color color)
        {
            GameObject obj = new GameObject("InstrumentImpact");
            AreaImpactEffect impact = obj.AddComponent<AreaImpactEffect>();
            impact.Initialize(pos, delay, radius, damage, starSprite, color);
        }
    }
}
