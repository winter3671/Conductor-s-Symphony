using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // "10종 악기별 공격 메커니즘 기획서" 1단계: 탭+오토타겟 4종(피아노/벨/마림바/글록켄슈필).
    // 나머지 6종(홀드 기반 4종 + 드럼/플루트)은 아직 여기 연결되지 않았고, RhythmAttackManager의
    // 기존 범용 투사체 로직으로 계속 폴백한다 - IsImplemented(type)가 false를 반환하는 동안은 안전.
    public static class InstrumentAttackDispatcher
    {
        private static Sprite beamSprite;
        private static Sprite starSprite;

        public static bool IsImplemented(InstrumentType type)
        {
            return type == InstrumentType.Piano
                || type == InstrumentType.Bell
                || type == InstrumentType.Marimba
                || type == InstrumentType.Glockenspiel;
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
            }
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
