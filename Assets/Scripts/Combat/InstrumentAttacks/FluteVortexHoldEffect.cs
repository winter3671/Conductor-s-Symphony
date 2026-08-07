using UnityEngine;
using ConductorSymphony.Player;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 플루트: 숏 홀드("2~4칸") 자체는 아무 효과가 없고, 떼는 순간 플레이어가 "지나간 자리"(근사: 바라보는
    // 방향의 반대쪽)에 미니 소용돌이(FluteVortexEffect)를 생성한다. 기획서 4번(바람 와류) 참고.
    // 다른 3종(바이올린/프렌치호른/첼로)과 달리 홀드 "유지 중"에는 아무 것도 하지 않는 게 의도된 설계다 -
    // 플루트는 딜러가 아니라 다른 광역 악기와 연계하는 순수 CC(군집) 담당 악기이기 때문.
    public class FluteVortexHoldEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int level;

        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 소용돌이는 무피해 CC라 "낱개로 셀 수
        // 있는 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함. 동시 유지 개수는
        // 이미 Lv4 스탯이 별도로 담당).
        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.level = level;
            // 홀드 유지 중에는 시각적으로도 아무 것도 표시하지 않는다(단순화) - 실제 이펙트는 릴리즈 시점에만 발생.
        }

        public void OnHoldTick(float deltaTime)
        {
            // 의도적으로 비워둠 - 플루트는 홀드 유지 중 효과가 없다(위 클래스 주석 참고).
        }

        public void OnHoldReleased(bool completedFully)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            Vector3 playerPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : transform.position;

            // "지나간 자리"를 정확히 추적하는 대신, 바라보는 방향의 반대쪽(뒤쪽)에 스폰하는 것으로 근사한다.
            Vector3 spawnPos = playerPos - (Vector3)facing * 0.8f;

            GameObject vortexObj = new GameObject("FluteVortex");
            FluteVortexEffect vortex = vortexObj.AddComponent<FluteVortexEffect>();
            vortex.Initialize(spawnPos, level);

            Destroy(gameObject);
        }
    }
}
