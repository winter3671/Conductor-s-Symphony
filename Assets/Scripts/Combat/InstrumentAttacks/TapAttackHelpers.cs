using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 탭 기반 악기 이펙트(ITapAttackEffect 구현체)들이 공유하는 스프라이트 캐시 + 공용 투사체/임팩트 생성 헬퍼.
    // 기존에는 InstrumentAttackDispatcher 안에 이 로직이 전부 섞여 있었으나, 탭 5종을 각자의 클래스로
    // 분리하면서(#44) 여러 이펙트가 공유해야 하는 부분만 이곳으로 뽑아냈다.
    internal static class TapAttackHelpers
    {
        private static Sprite beamSprite;
        private static Sprite starSprite;

        internal static Sprite StarSprite
        {
            get
            {
                EnsureSprites();
                return starSprite;
            }
        }

        internal static void EnsureSprites()
        {
            if (beamSprite == null) beamSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 7f, Color.white);
            if (starSprite == null) starSprite = ProceduralSpriteFactory.CreateDiamond(20, 9f, Color.white);
        }

        internal static void SpawnBeam(Vector3 start, Vector3 dir, int damage, int pierce, float maxRange, bool bounce, Color color, float sizeMultiplier = 1f, System.Action<EnemyMonster, Vector3> onHitEnemy = null)
        {
            EnsureSprites();
            GameObject obj = new GameObject("InstrumentBeam");
            PiercingBeamProjectile beam = obj.AddComponent<PiercingBeamProjectile>();
            beam.Initialize(start, dir, speed: 14f, damage, pierce, maxRange, bounce, beamSprite, color, visualLength: 1.1f, sizeMultiplier: sizeMultiplier, onHitEnemy: onHitEnemy);
        }

        internal static void SpawnImpact(Vector3 pos, float delay, float radius, int damage, Color color)
        {
            EnsureSprites();
            GameObject obj = new GameObject("InstrumentImpact");
            AreaImpactEffect impact = obj.AddComponent<AreaImpactEffect>();
            impact.Initialize(pos, delay, radius, damage, starSprite, color);
        }
    }
}
