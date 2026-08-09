using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 직선으로 날아가며 지나가는 모든 적을 관통(pierce) 타격하는 공용 투사체.
    // 피아노(건반 레이저), 벨(8방향 성광), 마림바(직선 파동)가 전부 이 클래스를 공유한다.
    public class PiercingBeamProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private int damage;
        private int remainingPierce;
        private float maxRange;
        private float traveledDistance;
        private bool bounceOnMaxRange;
        private bool hasBounced;
        private const float BaseHitRadius = 0.5f;
        private float hitRadius = BaseHitRadius;

        // 마림바 Lv3("파동 크기 +30%") 등 - 관통 파동/레이저의 히트 반경과 시각적 크기를 함께 키운다.
        private float sizeMultiplier = 1f;

        // 마림바 Lv5(피격 시 감속+밀쳐냄), 바이올린 Lv5(참격 자리 잔향) 등 - 명중한 적/위치별로 악기 고유의
        // 추가 처리가 필요할 때 이 콜백으로 위임한다. 공용 투사체 클래스 자체는 특정 악기를 모른다.
        private System.Action<EnemyMonster, Vector3> onHitEnemy;

        private readonly HashSet<EnemyMonster> hitEnemies = new HashSet<EnemyMonster>();
        private bool hasHitBoss = false;

        // 2026-08-08: 나노바나나로 그린 4프레임 "에너지 볼트" 반짝임 애니메이션(Assets/Resources/Sprites/
        // Effects/Beam/Beam1~4 - 미세한 반짝임 정도만 있고 형태 변화는 거의 없음). 피아노/벨/마림바가
        // 이 클래스를 공유하므로 여기 한 곳에서만 로딩하면 세 악기 모두 자동 적용된다. 프레임 로딩
        // 실패 시(에셋 미존재 등) 기존처럼 Initialize()로 전달받은 단일 sprite(프로시저럴 원)로 폴백한다.
        private static Sprite[] beamFrames;
        private static bool triedLoadBeamFrames = false;
        private const float FrameInterval = 0.06f;
        private float frameTimer;
        private int frameIndex;
        private SpriteRenderer sr;
        private float visualLength;

        // 기존 폴백 원(ProceduralSpriteFactory.CreateFilledCircle(16, 7f, ...))의 콘텐츠 크기
        // (PPU 100 기준 16px/100 = 0.16 월드유닛, 정사각형이라 가로세로 동일). 아래 ApplyBeamFrame()이
        // "어떤 스프라이트를 쓰든 화면상 길이/두께는 항상 이 기준 크기로 튜닝된 visualLength×0.28
        // 비율과 정확히 같아지도록" 역산하는 기준값이다 - 새 아트가 이미 가로로 길쭉한 모양(약 7~8:1)
        // 이라, 예전처럼 콘텐츠 크기를 무시하고 고정 배율만 곱하면 여기에 또 곱해져서(이중 확대)
        // 지나치게 가늘고 긴 모양이 됐을 것(AreaImpactEffect가 겪었던 것과 같은 종류의 버그를 사전에
        // 방지). 콘텐츠 폭/높이로 먼저 나눠서 "기준 원과 같은 크기"로 정규화한 뒤 기존 배율을 곱한다.
        private const float ReferenceContentSize = 0.16f;

        // 2026-08-08: 위 ReferenceContentSize 정규화가 "예전 프로시저럴 원과 완전히 동일한 화면 크기"를
        // 만드는 데는 성공했지만, 그 예전 크기 자체가 (0.176 x 0.045 월드유닛) 너무 작아서 새로 그린
        // 디테일한 아트가 잘 안 보인다는 피드백(2026-08-08, 실제 플레이 확인). 판정 반경(hitRadius)과는
        // 완전히 무관한 순수 시각 배율이라 여기서만 키우면 밸런스에 영향 없음. 최초 시도값 3배로는
        // 아직도 "하나도 안 보인다"는 재피드백(바이올린 릴리즈 참격 기준, 2026-08-08)을 받아 6배로
        // 재조정 - 최종 크기는 대략 1.06 x 0.27 월드유닛(hitRadius 지름 1.0과 비슷한 정도). 그래도
        // 작으면 이 상수만 더 올리면 됨.
        private const float ArtVisualScale = 6f;

        public void Initialize(Vector3 startPos, Vector3 dir, float speed, int damage, int pierceCount, float maxRange, bool bounceOnMaxRange, Sprite sprite, Color color, float visualLength = 1.1f, float sizeMultiplier = 1f, System.Action<EnemyMonster, Vector3> onHitEnemy = null)
        {
            transform.position = startPos;
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            this.speed = speed;
            this.damage = damage;
            remainingPierce = Mathf.Max(1, pierceCount);
            this.maxRange = maxRange;
            this.bounceOnMaxRange = bounceOnMaxRange;
            this.sizeMultiplier = sizeMultiplier;
            this.hitRadius = BaseHitRadius * sizeMultiplier;
            this.onHitEnemy = onHitEnemy;
            this.visualLength = visualLength;

            EnsureBeamFrames();

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = color;
            sr.sortingOrder = 15;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            Sprite initialFrame = (beamFrames != null && beamFrames.Length > 0) ? beamFrames[0] : sprite;
            ApplyBeamFrame(initialFrame);
        }

        private static void EnsureBeamFrames()
        {
            if (triedLoadBeamFrames) return;
            triedLoadBeamFrames = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/Beam");
            if (loaded != null && loaded.Length > 0)
            {
                // Resources.LoadAll의 반환 순서는 보장되지 않으므로(Beam1~4), 파일명 기준으로 직접 정렬한다.
                System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
                beamFrames = loaded;
            }
        }

        private void ApplyBeamFrame(Sprite frame)
        {
            if (frame == null || sr == null) return;
            sr.sprite = frame;

            float contentW = frame.bounds.size.x;
            float contentH = frame.bounds.size.y;
            float scaleX = (contentW > 0.0001f)
                ? visualLength * sizeMultiplier * (ReferenceContentSize / contentW)
                : visualLength * sizeMultiplier;
            float scaleY = (contentH > 0.0001f)
                ? 0.28f * sizeMultiplier * (ReferenceContentSize / contentH)
                : 0.28f * sizeMultiplier;
            transform.localScale = new Vector3(scaleX * ArtVisualScale, scaleY * ArtVisualScale, 1f);
        }

        private void Update()
        {
            Vector3 step = direction * speed * Time.deltaTime;
            transform.position += step;
            traveledDistance += step.magnitude;

            if (beamFrames != null && beamFrames.Length > 1)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= FrameInterval)
                {
                    frameTimer = 0f;
                    frameIndex = (frameIndex + 1) % beamFrames.Length;
                    ApplyBeamFrame(beamFrames[frameIndex]);
                }
            }

            CheckHits();
            if (remainingPierce <= 0)
            {
                Destroy(gameObject);
                return;
            }

            if (traveledDistance >= maxRange)
            {
                if (bounceOnMaxRange && !hasBounced)
                {
                    hasBounced = true;
                    traveledDistance = 0f;
                    direction = -direction;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void CheckHits()
        {
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null || hitEnemies.Contains(enemy)) continue;

                if (Vector3.Distance(transform.position, enemy.transform.position) <= hitRadius)
                {
                    enemy.TakeDamage(damage);
                    onHitEnemy?.Invoke(enemy, transform.position);
                    hitEnemies.Add(enemy);
                    remainingPierce--;
                    if (remainingPierce <= 0) return;
                }
            }

            if (!hasHitBoss && BossMonster.Instance != null)
            {
                if (Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= hitRadius + BossMonster.Instance.HitboxRadius)
                {
                    BossMonster.Instance.TakeDamage(damage);
                    hasHitBoss = true;
                    remainingPierce--;
                }
            }
        }
    }
}
