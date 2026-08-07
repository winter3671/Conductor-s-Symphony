using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 짧은 지연 후 특정 지점에 원형 범위 피해를 1회 입히는 공용 이펙트.
    // 글록켄슈필(별빛 낙하), 팀파니(캐논/융단폭격)가 사용한다 - "낙하 시간"을 표현하기 위해 작게
    // 시작했다가 착탄 시 커진다.
    public class AreaImpactEffect : MonoBehaviour
    {
        private float remainingDelay;
        private float initialDelay;
        private float radius;
        private int damage;
        private bool hasImpacted = false;
        private SpriteRenderer spriteRenderer;

        // 팀파니 Lv4(1초 기절) 등 - 착탄으로 맞은 개별 적에 대한 추가 처리를 호출자에게 위임.
        private System.Action<EnemyMonster> onHitEnemy;
        // 팀파니 Lv5(착탄 지점 3초 지진지대 잔류) 등 - 실제로 착탄이 발생한(딜레이가 끝난) 정확한 시점/위치를 알려준다.
        private System.Action<Vector3> onImpact;

        // 2026-08-08: 나노바나나+영상 프레임 추출로 제작한 9프레임 "임팩트 버스트" 애니메이션
        // (Assets/Resources/Sprites/Effects/ImpactBurst/spark1~9 - 작게 시작 → 5번(spark5)에서 최대로
        // 만개 → 다시 작아지며 소멸하는 무채색 흑백 클립. SpriteRenderer.color로 악기별 틴트를 곱해서
        // 쓴다). 예고(딜레이) 구간엔 앞쪽 절반(1→5)을, 착탄 후 플래시 구간엔 뒤쪽 절반(5→9)을 나눠 써서
        // "착탄 순간 = 가장 만개한 프레임"이 되도록 맞췄다. 프레임 로딩 실패 시(에셋 미존재 등) 기존처럼
        // Initialize()로 전달받은 단일 sprite로 그대로 폴백한다 - 호출부(TapAttackHelpers.SpawnImpact,
        // TimpaniBombardmentEffect.SpawnImpact)는 변경할 필요가 없다.
        private static Sprite[] burstFrames;
        private static bool triedLoadFrames = false;
        private const float FlashDuration = 0.2f;
        private float flashTimer;
        private bool flashing = false;

        // 2026-08-08 버그 수정(실측 리포트): 처음 출시했을 때 스파크가 실제 타격범위의 몇 배로 거대하게
        // 보이는 문제가 있었다. 원인은 이중 확대 - 기존 다이아몬드는 캔버스(20px) 대비 도형이 아주 작아
        // (반지름 9px) transform.localScale = radius*1.6을 곱해도 화면상 지름이 radius의 약 29%로만
        // 작게 보였는데, 새 스파크 프레임은 임포트 시 알파 기준으로 트림되어 프레임 대부분(특히 최대로
        // 만개한 spark5)이 캔버스를 거의 꽉 채운다 - 같은 radius*1.6 스케일을 그대로 곱하니 실제 지름이
        // radius의 7배 이상으로 뻥튀기됐다. 게다가 프레임마다 트림된 크기가 제각각이라(spark1은 작고
        // spark5는 큼) 프레임이 바뀔 때마다 크기가 들쭉날쭉하기도 했다.
        // InstrumentOrbit의 "최대 치수 정규화"와 동일한 방식으로 고친다: 어떤 프레임이 보이든 프레임
        // 콘텐츠의 실제 픽셀 크기와 무관하게 "화면에 그려지는 지름"을 fallbackSprite/aim 프레임의
        // 크기가 아니라 peakDiameter(및 아래 bloom 배율) 기준으로 매번 새로 계산해서 맞춘다.
        private Sprite fallbackSprite;
        private float peakDiameter;
        private const float MinVisualFraction = 0.15f; // 예고 시작/소멸 끝 시점 크기(피크 대비 비율)

        public void Initialize(Vector3 pos, float delaySeconds, float impactRadius, int damageAmount, Sprite sprite, Color color, System.Action<EnemyMonster> onHitEnemy = null, System.Action<Vector3> onImpact = null)
        {
            transform.position = pos;
            remainingDelay = Mathf.Max(0.01f, delaySeconds);
            initialDelay = remainingDelay;
            radius = impactRadius;
            damage = damageAmount;
            this.onHitEnemy = onHitEnemy;
            this.onImpact = onImpact;
            fallbackSprite = sprite;
            // 착탄 순간(가장 만개한 프레임)의 화면상 지름이 정확히 실제 판정 반경(radius)의 지름과
            // 같아지도록 - 예전 다이아몬드는 훨씬 작게(radius의 약 29%) 그려졌지만, 이제는 실제 히트
            // 범위를 그대로 보여주는 편이 (제대로 그린 이펙트가 생긴 김에) 더 낫다고 판단함.
            peakDiameter = impactRadius * 2f;

            EnsureBurstFrames();

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 14;
            ApplyFrame(0f); // 초기 프레임 + 크기 세팅 (예고 시작: 작게)
        }

        private static void EnsureBurstFrames()
        {
            if (triedLoadFrames) return;
            triedLoadFrames = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/ImpactBurst");
            if (loaded != null && loaded.Length > 0)
            {
                // Resources.LoadAll의 반환 순서는 보장되지 않으므로(spark1~9), 파일명 기준으로 직접 정렬한다.
                System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
                burstFrames = loaded;
            }
        }

        private void Update()
        {
            if (flashing)
            {
                flashTimer += Time.deltaTime;
                float ft = Mathf.Clamp01(flashTimer / FlashDuration);
                ApplyFrame(0.5f + ft * 0.5f); // 뒤쪽 절반: 만개 → 소멸
                return;
            }

            if (hasImpacted) return;

            remainingDelay -= Time.deltaTime;

            // 낙하 예고 표시가 착탄 시점까지 점점 커지도록 (실제 크기 계산은 ApplyFrame이 전담)
            float t = 1f - Mathf.Clamp01(remainingDelay / initialDelay);
            ApplyFrame(t * 0.5f); // 앞쪽 절반: 시작 → 만개

            if (remainingDelay <= 0f)
            {
                Impact();
            }
        }

        private void ApplyFrame(float progress01)
        {
            if (spriteRenderer == null) return;

            Sprite frame;
            if (burstFrames != null && burstFrames.Length > 0)
            {
                int index = Mathf.Clamp(Mathf.RoundToInt(progress01 * (burstFrames.Length - 1)), 0, burstFrames.Length - 1);
                frame = burstFrames[index];
                spriteRenderer.sprite = frame;
            }
            else
            {
                frame = fallbackSprite;
                if (frame != null) spriteRenderer.sprite = frame;
            }

            if (frame == null) return;

            // progress01은 전체 생애주기(0=예고 시작 → 0.5=착탄/만개 → 1=플래시 끝/소멸)를 나타내므로,
            // 0.5에서 최대(1.0)이고 양 끝에서 MinVisualFraction으로 줄어드는 삼각형 곡선을 그린다.
            float bloom = 1f - Mathf.Abs(progress01 - 0.5f) * 2f;
            float sizeFactor = Mathf.Lerp(MinVisualFraction, 1f, Mathf.Clamp01(bloom));

            // 프레임마다 임포트된 콘텐츠 크기(트림 여부 등)가 달라도, InstrumentOrbit과 동일한 "최대 치수
            // 정규화"로 화면에 실제로 그려지는 지름을 peakDiameter * sizeFactor로 고정한다.
            float maxDim = Mathf.Max(frame.bounds.size.x, frame.bounds.size.y);
            if (maxDim > 0.0001f)
            {
                transform.localScale = Vector3.one * (peakDiameter * sizeFactor / maxDim);
            }
        }

        private void Impact()
        {
            hasImpacted = true;

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.transform.position) <= radius)
                {
                    enemy.TakeDamage(damage);
                    onHitEnemy?.Invoke(enemy);
                }
            }

            if (BossMonster.Instance != null && Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= radius)
            {
                BossMonster.Instance.TakeDamage(damage);
            }

            onImpact?.Invoke(transform.position);

            // 착탄 플래시(만개→소멸 애니메이션)가 재생될 시간만큼 유지한 뒤 정리. 프레임 로딩 실패 시엔
            // 기존과 동일하게 착탄 시점 프레임이 잠깐 정지된 채로 보인다.
            flashing = true;
            flashTimer = 0f;
            Destroy(gameObject, FlashDuration);
        }
    }
}
