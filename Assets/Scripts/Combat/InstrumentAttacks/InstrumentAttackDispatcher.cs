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
        // 문서: 정박 타격 시 장판이 팽창하며 전방위로 강력한 넉백 파동을 분사.
        // "상시 비트 오라"(판정과 무관한 지속 소량 타격)는 이 디스패처(판정 성공 시에만 호출됨)가 아니라
        // RhythmAttackManager.UpdateDrumAura()가 별도로 담당한다 - 자세한 내용은 그쪽 주석 참고.
        private static void ExecuteDrums(int level, int damage, Vector3 origin, Color color)
        {
            float radius = 2.0f + 0.3f * Mathf.Max(0, level - 1);        // 레벨당 넉백 파동 범위 소폭 증가
            float knockbackImpulse = 0.6f + 0.1f * Mathf.Max(0, level - 1);

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
        // 문서: Lv1 1발 관통 / Lv3 관통 +2 / Lv4 발사 수 +1 / Lv5 6연타 성공 시 폭포 추가 발사
        private static void ExecutePiano(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            Vector3 dir = nearest != null ? (nearest.transform.position - origin) : Vector3.up;

            int pierce = 2 + (level >= 3 ? 2 : 0); // Lv1~2: 2관통, Lv3+: 4관통
            int shots = 1 + (level >= 4 ? 1 : 0);  // Lv1~3: 1발, Lv4+: 2발

            for (int i = 0; i < shots; i++)
            {
                SpawnBeam(origin, dir, damage, pierce, maxRange: 9f, bounce: false, color);
            }

            // Lv5: 6연타 성공마다("건반 폭포") 부채꼴로 3발 추가 발사
            if (level >= 5 && currentCombo > 0 && currentCombo % 6 == 0)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 spreadDir = Quaternion.Euler(0f, 0f, i * 12f) * dir;
                    SpawnBeam(origin, spreadDir, damage, pierce, 9f, false, color);
                }
            }
        }

        // ---- 2. 벨: 가장 가까운 적 중심 8방향 성광(星光) ----
        // 문서: Lv2 사거리 +30% / Lv3 관통력 강화 / Lv4 8방향 2연속 발사 / Lv5 잔향(지속 타격, 추후 연결)
        private static void ExecuteBell(int level, int damage, Vector3 origin, Color color)
        {
            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            Vector3 center = nearest != null ? nearest.transform.position : origin;

            float range = 2.5f * (1f + 0.3f * Mathf.Max(0, level - 1));
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
        }

        // ---- 3. 마림바: 이동 방향 일직선 목재 파동 ----
        // 문서: Lv2 관통 +2 / Lv3 파동 크기 +30%(단순화: 오프비트 여부 구분 없이 항상 적용) / Lv4 화면 끝 1회 바운스 / Lv5 감속+밀쳐냄(추후 연결)
        private static void ExecuteMarimba(int level, int damage, Vector3 origin, Color color)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            int pierce = 3 + (level >= 2 ? 2 : 0);
            bool bounce = level >= 4;

            SpawnBeam(origin, facing, damage, pierce, maxRange: 10f, bounce, color);
        }

        // ---- 4. 글록켄슈필: 체력이 가장 높은 적 머리 위 별빛 낙하 ----
        // 문서: Lv3 스플래시 추가 / Lv4 버스트(4/8콤보) 성공 시 별빛 수 +2 / Lv5 2차 유도 파편+기절(추후 연결)
        private static void ExecuteGlockenspiel(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster target = CombatTargetingUtility.GetHighestHpEnemy(origin);
            Vector3 pos = target != null ? target.transform.position : origin;

            float splashRadius = 0.6f + (level >= 3 ? 0.5f : 0f);
            SpawnImpact(pos, 0.15f, splashRadius, damage, color);

            bool burstReady = level >= 4 && currentCombo > 0 && currentCombo % 4 == 0;
            if (burstReady)
            {
                int extraStars = (currentCombo % 8 == 0) ? 2 : 1; // 8마디 8연타는 4마디 4연타보다 더 풍성하게
                for (int i = 0; i < extraStars; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), 0f);
                    SpawnImpact(pos + offset, 0.15f, splashRadius, damage, color);
                }
            }
        }

        private static void SpawnBeam(Vector3 start, Vector3 dir, int damage, int pierce, float maxRange, bool bounce, Color color)
        {
            GameObject obj = new GameObject("InstrumentBeam");
            PiercingBeamProjectile beam = obj.AddComponent<PiercingBeamProjectile>();
            beam.Initialize(start, dir, speed: 14f, damage, pierce, maxRange, bounce, beamSprite, color);
        }

        private static void SpawnImpact(Vector3 pos, float delay, float radius, int damage, Color color)
        {
            GameObject obj = new GameObject("InstrumentImpact");
            AreaImpactEffect impact = obj.AddComponent<AreaImpactEffect>();
            impact.Initialize(pos, delay, radius, damage, starSprite, color);
        }
    }
}
