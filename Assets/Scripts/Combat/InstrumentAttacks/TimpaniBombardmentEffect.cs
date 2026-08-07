using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 팀파니: 홀드 시작 즉시 가장 가까운 적 위치에 충격파 포탄이 낙하하고(단타 "캐논"),
    // 이후 홀드를 유지("롤ing", 2026-08-08부터 10스텝 - InstrumentPatternDatabase.holdLengthSteps 참고)
    // 하는 동안 같은 구역에 주기적으로 소형 융단폭격이 추가된다.
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

        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.damage = damage;
            this.color = color;
            EnsureSprite();

            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때도 캐논/융단폭격이 보스 위치를 노리도록
            // GetNearestTargetPosition으로 교체(기존엔 origin=플레이어 위치를 계속 폭격하던 버그 -
            // "최종보스 단독 페이즈에서 팀파니가 인식을 못 한다"는 실측 리포트로 발견).
            targetPos = CombatTargetingUtility.GetNearestTargetPosition(origin, origin);

            // Lv3+: 폭격 빈도 +50% × 알레그로(Allegro) 패시브 "쿨타임 감축"(값이 작을수록 더 자주 발동)
            bombardInterval = ((level >= 3) ? (0.65f / 1.5f) : 0.65f) * CombatTargetingUtility.GetCooldownMultiplier();
            applyStun = level >= 4;                                  // Lv4+: 착탄 시 1초 기절
            lingeringZone = level >= 5;                              // Lv5: 착탄 지점 3초 지진지대 잔류

            // Lv2+: 범위 +25% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            float initialRadius = 1.0f * (level >= 2 ? 1.25f : 1f) * CombatTargetingUtility.GetRangeMultiplier();

            // 즉발 "팀파니 캐논" - 홀드 시작 즉시 1회 착탄
            SpawnImpact(targetPos, 0.05f, initialRadius, damage, color);

            // 레가토(Legato) 패시브/악기 Lv4 Multi+1(extraProjectiles): 홀드 시작 시점에 캐논 포탄을
            // 추가로 더 발사한다 - 융단폭격과 같은 랜덤 오프셋 방식으로 착탄 지점을 흩뿌린다.
            for (int e = 0; e < extraProjectiles; e++)
            {
                Vector3 legatoOffset = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0f)
                    * CombatTargetingUtility.GetRangeMultiplier();
                SpawnImpact(targetPos + legatoOffset, 0.05f, initialRadius, damage, color);
            }

            // 융단폭격이 착탄할 수 있는 구역(오프셋 ±1.0×크레센도 배율의 정사각형 범위, OnHoldTick 참고)을
            // 표시하는 얇은 테두리 링 - 프렌치호른의 부채꼴 근사와 같은 방식으로, 실제 정사각형 범위를
            // 반지름 1.0×배율의 원으로 근사 표시한다(2026-08-07). targetPos는 홀드 내내 고정이므로 링도
            // 그 자리에 고정. 이 컴포넌트의 GameObject transform은 위치/스케일을 따로 쓰지 않으므로, 자식
            // 오브젝트의 월드 위치를 targetPos로 직접 지정하면 된다.
            GameObject zoneRingObj = new GameObject("TimpaniZoneRing");
            zoneRingObj.transform.SetParent(transform);
            zoneRingObj.transform.position = targetPos;
            SpriteRenderer zoneRingSr = zoneRingObj.AddComponent<SpriteRenderer>();
            zoneRingSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            zoneRingSr.sortingOrder = 4;
            zoneRingObj.transform.localScale = Vector3.one * (1.0f * CombatTargetingUtility.GetRangeMultiplier());
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
            // 알레그로(쿨타임 감축)는 tickInterval에, 페르마타(지속시간 증가)는 duration에 반영.
            zone.Initialize(pos, radius: 1.0f * CombatTargetingUtility.GetRangeMultiplier(), tickDamage: Mathf.Max(1, damage / 3),
                tickInterval: 0.5f * CombatTargetingUtility.GetCooldownMultiplier(), duration: 3f * CombatTargetingUtility.GetDurationMultiplier(), color);
        }

        private static void EnsureSprite()
        {
            if (impactSprite == null) impactSprite = ProceduralSpriteFactory.CreateDiamond(20, 9f, Color.white);
        }
    }
}
