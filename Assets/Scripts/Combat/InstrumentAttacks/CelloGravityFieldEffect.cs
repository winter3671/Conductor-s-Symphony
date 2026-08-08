using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 첼로: 홀드("11칸 베이스 롱노트", 2026-08-08부터 - InstrumentPatternDatabase.holdLengthSteps 참고)
    // 시작 시 그 시점의 가장 가까운 적 발밑에 고정된 중력장을 생성한다.
    // 필드는 캐스팅 위치에 고정되며 적을 추적하지 않는다(기획서 "고정된 중력장" 문구 그대로 반영).
    // 범위 내 적의 이동 속도를 감소시키고 주기적으로 타격. 기획서 7번(중력의 구속) 참고.
    // 레벨별 수치는 밸런스 doc(game_balance_design.docx) 5번 항목 반영: Lv1 이속감소 40%(기존 50%에서 정정) /
    // Lv2 범위+20% / Lv3 감소 40%→60% / Lv4 필드 잔류시간+30%(홀드 종료 후에도 잠시 유지) /
    // Lv5 중앙으로 지속 끌어당김 기믹 추가.
    public class CelloGravityFieldEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int level;
        private int damage;
        private float radius;
        private float slowFraction;
        private const float TickInterval = 0.4f;
        private float tickTimer;

        // Lv4: 홀드가 끝난 뒤에도 필드가 잠시 더 유지되는 "잔류시간". HoldEffectCoordinator는 릴리즈 즉시
        // 이 컴포넌트를 더 이상 추적하지 않으므로(OnHoldTick이 더 안 불림), 잔류 동안은 자체 Update()로
        // 계속 틱을 굴린다.
        private const float BaseLingerDuration = 1.0f;
        private bool isLingering;
        private float lingerTimer;

        // Lv5: 중앙으로 지속 끌어당김
        private const float PullStrength = 1.5f;

        private readonly HashSet<EnemyMonster> affectedEnemies = new HashSet<EnemyMonster>();

        // 2026-08-08: 손그림 11프레임 스월 애니메이션(Assets/Resources/Sprites/Effects/GravityField/
        // gravity_field1~11 - 사용자가 회전만으로는 밋밋할 것 같다며 프레임별로 형태가 변하는 애니메이션을
        // 직접 그림). 필드 아트는 보라-파랑 톤으로 이미 색이 입혀져 있고, 첼로 고유색(갈색, 링에 사용)과는
        // 의도적으로 다른 톤을 그대로 쓰기로 함(2026-08-08, 사용자 결정 - "중력장 이펙트 고유색" vs
        // "악기 식별 링 색"으로 구분).
        private static Sprite[] gravityFrames;
        private static bool triedLoadGravityFrames = false;
        private const float FrameInterval = 0.08f;
        private float frameTimer;
        private int frameIndex;
        private SpriteRenderer fieldSr;
        private Transform fieldArtTransform;

        // 손그림 아트(500x494 캔버스, 콘텐츠가 거의 꽉 참)는 기존 프로시저럴 원(28px 캔버스)과 bounds
        // 크기가 완전히 다르다. 기존엔 이 오브젝트의 루트 transform.localScale = radius*0.9를 아트 크기와
        // 링 보정(1/0.9 상쇄)에 동시에 재사용했는데, 그 매직넘버를 새 아트에 그대로 적용하면 다시
        // 이중 확대 버그가 난다(AreaImpactEffect/PiercingBeamProjectile에서 겪은 것과 동일 패턴이라
        // 사전 방지). 아트를 별도 자식 오브젝트로 분리해 콘텐츠 크기 기반으로 독립 계산하고, 루트는
        // identity로 두어 링도 "radius를 직접 곱하기"로 단순화했다(과거 0.9 상쇄 매직넘버 제거).
        private const float ReferenceContentSize = 0.28f; // 기존 CreateFilledCircle(28,13f,...) 풀캔버스 bounds(28px/100)
        private const float ArtVisualScale = 1f; // 순수 시각 배율 - 빔 사례처럼 실측 후 작아 보이면 이 값만 올리면 됨

        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 고정 위치 필드 판정이라 "낱개로 셀 수
        // 있는 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함).
        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.level = level;
            this.damage = damage;
            // Lv2+: 범위 +20% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 1.8f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            slowFraction = (level >= 3) ? 0.6f : 0.4f;         // Lv1: 40%, Lv3+: 60%

            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때도 필드가 보스 발밑에 생성되도록
            // GetNearestTargetPosition으로 교체(기존엔 origin=플레이어 위치에 생성되던 버그).
            transform.position = CombatTargetingUtility.GetNearestTargetPosition(origin, origin);

            EnsureGravityFrames();

            GameObject fieldArtObj = new GameObject("CelloFieldArt");
            fieldArtObj.transform.SetParent(transform, false);
            fieldArtTransform = fieldArtObj.transform;
            fieldSr = fieldArtObj.AddComponent<SpriteRenderer>();
            // 색상 틴트는 넣지 않고(위 코멘트 참고 - 아트 자체 톤 유지) 알파만 곱해 기존처럼 반투명하게
            // (장판 아래 적이 비쳐 보이도록) 유지한다.
            fieldSr.color = new Color(1f, 1f, 1f, 0.45f);
            fieldSr.sortingOrder = 3;
            Sprite initialFrame = (gravityFrames != null && gravityFrames.Length > 0)
                ? gravityFrames[0]
                : ProceduralSpriteFactory.CreateFilledCircle(28, 13f, new Color(color.r, color.g, color.b, 1f));
            ApplyFrame(initialFrame);

            // 실제 판정 반경(radius)을 정확히 표시하는 얇은 테두리 링. 드럼 오라 링과 동일하게 아주
            // 얇게(0.985~1.0) 설정(2026-08-07, 사용자 결정). 루트가 이제 identity라 radius를 직접 곱하면 됨.
            GameObject rangeRingObj = new GameObject("CelloRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * radius;
        }

        private static void EnsureGravityFrames()
        {
            if (triedLoadGravityFrames) return;
            triedLoadGravityFrames = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/GravityField");
            if (loaded != null && loaded.Length > 0)
            {
                // 프레임이 11장(2자리 번호 포함)이라 다른 이펙트들에 쓰던 string.CompareOrdinal 문자열
                // 정렬을 그대로 쓰면 "1,10,11,2,3..." 순으로 잘못 정렬된다(사전식 비교라 자릿수를 모름).
                // 파일명 끝 숫자를 뽑아 정수로 비교하는 자연 정렬로 교체(2026-08-08).
                System.Array.Sort(loaded, (a, b) => ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name)));
                gravityFrames = loaded;
            }
        }

        private static int ExtractTrailingNumber(string name)
        {
            // Sprite Mode가 Multiple로 임포트되면(이 프로젝트의 기본값) Unity가 서브스프라이트 이름
            // 끝에 "_인덱스"를 자동으로 붙인다(예: "gravity_field10" → "gravity_field10_0"). 그 끝자리
            // 인덱스 숫자를 실제 프레임 번호로 착각하면 전부 "_0"이라 모든 프레임이 0으로 동률 처리돼
            // 자연 정렬이 무력화된다 - 먼저 그 접미사를 한 번 제거한 뒤 진짜 프레임 번호를 추출한다.
            string s = name;
            int underscoreIndex = s.LastIndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < s.Length - 1)
            {
                bool suffixIsAllDigits = true;
                for (int j = underscoreIndex + 1; j < s.Length; j++)
                {
                    if (!char.IsDigit(s[j])) { suffixIsAllDigits = false; break; }
                }
                if (suffixIsAllDigits) s = s.Substring(0, underscoreIndex);
            }

            int i = s.Length;
            while (i > 0 && char.IsDigit(s[i - 1])) i--;
            return (i < s.Length) ? int.Parse(s.Substring(i)) : 0;
        }

        private void ApplyFrame(Sprite frame)
        {
            if (frame == null || fieldSr == null || fieldArtTransform == null) return;
            fieldSr.sprite = frame;
            float maxDim = Mathf.Max(frame.bounds.size.x, frame.bounds.size.y);
            float targetDiameter = radius * 0.9f * ReferenceContentSize * ArtVisualScale;
            if (maxDim > 0.0001f)
                fieldArtTransform.localScale = Vector3.one * (targetDiameter / maxDim);
        }

        public void OnHoldTick(float deltaTime)
        {
            TickFieldLogic(deltaTime);
        }

        // 홀드 중(OnHoldTick)과 릴리즈 후 잔류 기간(Update) 양쪽에서 공유하는 실제 필드 로직.
        private void TickFieldLogic(float deltaTime)
        {
            // 11프레임 스월 애니메이션 진행 - 홀드 중/잔류 중 양쪽 다 계속 돌아야 하므로 공유 로직에 둔다.
            if (gravityFrames != null && gravityFrames.Length > 1)
            {
                frameTimer += deltaTime;
                if (frameTimer >= FrameInterval)
                {
                    frameTimer = 0f;
                    frameIndex = (frameIndex + 1) % gravityFrames.Length;
                    ApplyFrame(gravityFrames[frameIndex]);
                }
            }

            HashSet<EnemyMonster> currentlyInRange = new HashSet<EnemyMonster>();
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= radius)
                {
                    currentlyInRange.Add(enemy);
                    if (!affectedEnemies.Contains(enemy))
                    {
                        enemy.SetSpeedMultiplier(1f - slowFraction);
                    }

                    // Lv5: 범위 안의 적을 중앙으로 서서히 끌어당김
                    if (level >= 5 && dist > 0.1f)
                    {
                        Vector3 toCenter = (transform.position - enemy.transform.position).normalized;
                        enemy.transform.position += toCenter * PullStrength * deltaTime;
                    }
                }
            }

            // 필드를 벗어난 적은 감속을 해제해야 원래 속도로 되돌아온다.
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null && !currentlyInRange.Contains(enemy))
                {
                    enemy.SetSpeedMultiplier(1f);
                }
            }

            affectedEnemies.Clear();
            foreach (var e in currentlyInRange) affectedEnemies.Add(e);

            // 알레그로(Allegro) 패시브 "쿨타임 감축" 반영 - 값이 작을수록(배율<1) 더 자주 틱.
            tickTimer += deltaTime;
            if (tickTimer < TickInterval * CombatTargetingUtility.GetCooldownMultiplier()) return;
            tickTimer = 0f;

            foreach (var enemy in currentlyInRange)
            {
                if (enemy != null) enemy.TakeDamage(damage);
            }

            // 2026-08-08 버그 수정: 위 로직 전체가 CombatTargetingUtility.GetActiveEnemies()(EnemyMonster)만
            // 다뤄서 보스는 중력장 범위 안에 있어도 감속/끌어당김/틱 피해를 전혀 못 받고 있었다. 감속
            // (SetSpeedMultiplier)·끌어당김은 EnemyMonster 전용 API라 그대로 두고(다른 곳의 보스 처리와
            // 동일한 관례 - 보스는 부가 효과 없이 피해만 적용), 틱 피해만 동일한 주기로 함께 적용한다.
            if (BossMonster.Instance != null)
            {
                float bossDist = Vector3.Distance(transform.position, BossMonster.Instance.transform.position);
                if (bossDist <= radius)
                {
                    BossMonster.Instance.TakeDamage(damage);
                }
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            // Lv4: 즉시 파괴하지 않고 잔류시간(+30%) 동안 필드를 유지한다. 자체 Update()가 이어받는다.
            // 페르마타(Fermata) 패시브 "지속시간 증가"도 함께 반영.
            isLingering = true;
            lingerTimer = BaseLingerDuration * (level >= 4 ? 1.3f : 1f) * CombatTargetingUtility.GetDurationMultiplier();
        }

        private void Update()
        {
            if (!isLingering) return;

            lingerTimer -= Time.deltaTime;
            if (lingerTimer <= 0f)
            {
                foreach (var enemy in affectedEnemies)
                {
                    if (enemy != null) enemy.SetSpeedMultiplier(1f);
                }
                affectedEnemies.Clear();
                Destroy(gameObject);
                return;
            }

            TickFieldLogic(Time.deltaTime);
        }
    }
}
