using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 팀파니: 홀드 시작 즉시 가장 가까운 적 위치에 충격파 포탄이 낙하하고(단타 "캐논"),
    // 이후 홀드를 유지("16마디 롤ing")하는 동안 같은 구역에 주기적으로 소형 융단폭격이 추가된다.
    // 기획서 8번(팀파니 캐논 + 지진 융단폭격) 참고. 탭(단타)과 홀드(롤ing) 두 모드가 문서에 함께 설명되어 있으나,
    // 0단계 홀드 인프라는 악기당 1가지 모드만 지원하므로 "홀드 시작 = 즉발 캐논, 유지 중 = 융단폭격"으로 단순화했다.
    // 레벨별 수치는 밸런스 doc(game_balance_design.docx) 5번 항목 반영: Lv2 낙하 범위+25%(이전엔 Lv3에서
    // 적용되던 버그성 어긋남을 정정) / Lv3 폭격 빈도+50%(이전엔 Lv4) / Lv4 착탄 시 1초 기절 /
    // Lv5 착탄 지점에 3초간 지진 지대 잔류.
    public class TimpaniBombardmentEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int damage;
        private Color color;
        private Vector3 targetPos;
        private float bombardInterval;
        private float tickTimer;
        private bool applyStun;
        private bool lingeringZone;

        private static Sprite impactSprite;

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.damage = damage;
            this.color = color;
            EnsureSprite();

            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            targetPos = nearest != null ? nearest.transform.position : origin;

            bombardInterval = (level >= 3) ? (0.65f / 1.5f) : 0.65f; // Lv3+: 폭격 빈도 +50%
            applyStun = level >= 4;                                  // Lv4+: 착탄 시 1초 기절
            lingeringZone = level >= 5;                              // Lv5: 착탄 지점 3초 지진지대 잔류

            // Lv2+: 범위 +25% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            float initialRadius = 1.0f * (level >= 2 ? 1.25f : 1f) * CombatTargetingUtility.GetRangeMultiplier();

            // 즉발 "팀파니 캐논" - 홀드 시작 즉시 1회 착탄
            SpawnImpact(targetPos, 0.05f, initialRadius, damage, color);
        }

        public void OnHoldTick(float deltaTime)
        {
            tickTimer += deltaTime;
            if (tickTimer < bombardInterval) return;
            tickTimer = 0f;

            // "지진 융단폭격": 지정 구역 주변에 소형 착탄을 랜덤 오프셋으로 추가. 오프셋 산포 범위와
            // 착탄 스플래시 반경에 크레센도(Crescendo) 범위 배율을 "동일하게" 곱한다 - 표적 하나 기준
            // 명중 확률(원 넓이/사각형 넓이 비율)이 배율과 무관하게 그대로 유지되도록 하는 결정임
            // (2026-08-06, 스플래시만 늘리면 명중률이 크게 오르고 오프셋만 늘리면 크게 떨어짐 - 이미
            // 튜닝된 DPS/명중률 밸런스를 건드리지 않기 위해 "같은 배율"로 결정).
            float rangeMultiplier = CombatTargetingUtility.GetRangeMultiplier();
            Vector3 offset = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0f) * rangeMultiplier;
            SpawnImpact(targetPos + offset, 0.1f, 0.7f * rangeMultiplier, Mathf.Max(1, damage / 2), color);
        }

        public void OnHoldReleased(bool completedFully)
        {
            Destroy(gameObject);
        }

        private void SpawnImpact(Vector3 pos, float delay, float radius, int dmg, Color impactColor)
        {
            GameObject obj = new GameObject("TimpaniImpact");
            AreaImpactEffect impact = obj.AddComponent<AreaImpactEffect>();

            System.Action<EnemyMonster> onHitEnemy = applyStun ? (enemy => enemy.ApplyStun(1.0f)) : (System.Action<EnemyMonster>)null;
            System.Action<Vector3> onImpact = lingeringZone ? (impactPos => SpawnSeismicZone(impactPos)) : (System.Action<Vector3>)null;

            impact.Initialize(pos, delay, radius, dmg, impactSprite, impactColor, onHitEnemy, onImpact);
        }

        // Lv5: 착탄 지점에 3초간 지진 지대가 남아 주기적으로 추가 피해를 준다.
        private void SpawnSeismicZone(Vector3 pos)
        {
            GameObject zoneObj = new GameObject("TimpaniSeismicZone");
            LingeringZoneEffect zone = zoneObj.AddComponent<LingeringZoneEffect>();
            // 바이올린 Lv5 잔향과 같은 이유로 범위 패시브 적용 대상에 포함(2026-08-06 결정).
            zone.Initialize(pos, radius: 1.0f * CombatTargetingUtility.GetRangeMultiplier(), tickDamage: Mathf.Max(1, damage / 3), tickInterval: 0.5f, duration: 3f, color);
        }

        private static void EnsureSprite()
        {
            if (impactSprite == null) impactSprite = ProceduralSpriteFactory.CreateDiamond(20, 9f, Color.white);
        }
    }
}
