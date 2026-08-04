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

        public void Initialize(Vector3 pos, float radius, int tickDamage, float tickInterval, float duration, Color color)
        {
            transform.position = pos;
            this.radius = radius;
            this.tickDamage = tickDamage;
            this.tickInterval = tickInterval;
            remainingDuration = duration;

            Color faded = color;
            faded.a = 0.35f;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(20, 9f, faded);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);
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

            if (BossMonster.Instance != null && Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= radius)
            {
                BossMonster.Instance.TakeDamage(tickDamage);
            }
        }
    }
}
