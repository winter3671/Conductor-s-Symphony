using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 착탄/공격 지점에 남아 일정 시간 동안 주기적으로 피해를 주는 공용 잔류 장판.
    // 밸런스 doc(game_balance_design.docx) 5번 항목의 다음 Lv5 전용 효과들이 공유한다:
    // 팀파니 Lv5("낙하 지점에 3초간 지진 지대 잔류"), 벨 Lv5("지나간 자리가 1.5초간 빛나며 지속 타격"),
    // 바이올린 Lv5("참격이 지난 자리에 2초간 검기 잔향").
    public class LingeringZoneEffect : MonoBehaviour
    {
        private float radius;
        private int tickDamage;
        private float tickInterval;
        private float tickTimer;
        private float remainingDuration;

        // 2026-08-08: 손그림 정지 이미지 1장(Assets/Resources/Sprites/Effects/LingeringZone/
        // LingeringZone.png - 균열이 중심에서 퍼지는 룬 서클). 세 악기(팀파니/벨/바이올린)가 색이 다
        // 다른 color를 넘겨서 이 클래스를 공유하므로, 기존처럼 색을 텍스처에 구워 넣지 않고 무채색
        // (회색조)으로 그려서 SpriteRenderer.color로 런타임 틴트하도록 바꿨다(빔/버스트와 동일 패턴).
        private static Sprite zoneSprite;
        private static bool triedLoadZoneSprite = false;
        private const float ReferenceContentSize = 0.2f; // 기존 CreateFilledCircle(20,9f,...) 풀캔버스 bounds(20px/100)

        public void Initialize(Vector3 pos, float radius, int tickDamage, float tickInterval, float duration, Color color)
        {
            transform.position = pos;
            this.radius = radius;
            this.tickDamage = tickDamage;
            this.tickInterval = tickInterval;
            remainingDuration = duration;

            EnsureZoneSprite();

            Color faded = color;
            faded.a = 0.35f;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = faded;
            sr.sortingOrder = 3;
            Sprite sprite = zoneSprite != null ? zoneSprite : ProceduralSpriteFactory.CreateFilledCircle(20, 9f, Color.white);
            sr.sprite = sprite;
            float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float targetDiameter = radius * 0.9f * ReferenceContentSize;
            if (maxDim > 0.0001f)
                transform.localScale = Vector3.one * (targetDiameter / maxDim);
        }

        private static void EnsureZoneSprite()
        {
            if (triedLoadZoneSprite) return;
            triedLoadZoneSprite = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/LingeringZone");
            if (loaded != null && loaded.Length > 0)
            {
                zoneSprite = loaded[0]; // 정지 이미지 1장만 사용
            }
        }

        private void Update()
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            tickTimer += Time.deltaTime;
            if (tickTimer < tickInterval) return;
            tickTimer = 0f;

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.transform.position) <= radius)
                {
                    enemy.TakeDamage(tickDamage);
                }
            }

            if (BossMonster.Instance != null && Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= radius + BossMonster.Instance.HitboxRadius)
            {
                BossMonster.Instance.TakeDamage(tickDamage);
            }
        }
    }
}
