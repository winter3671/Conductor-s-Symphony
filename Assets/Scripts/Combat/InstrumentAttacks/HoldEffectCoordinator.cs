using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // RhythmManager의 레인(lane) 단위 홀드 이벤트(OnHoldTickEvent/OnHoldReleasedEvent)를
    // 실제 악기별 지속 이펙트 객체(IHoldAttackEffect)에 연결하는 다리 역할.
    // 레인당 이펙트는 최대 1개만 존재한다 - 같은 레인에 새 홀드가 시작되면 이전 것을 먼저 안전 종료한다.
    public static class HoldEffectCoordinator
    {
        private static readonly Dictionary<RhythmLane, IHoldAttackEffect> activeEffects = new Dictionary<RhythmLane, IHoldAttackEffect>();

        public static void BeginHold(RhythmLane lane, InstrumentType type, int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            if (!InstrumentAttackDispatcher.IsHoldImplemented(type)) return;

            // 안전장치: RhythmManager가 레인당 홀드를 1개만 유지하므로 이론상 발생하면 안 되지만,
            // 혹시 이전 이펙트가 정리되지 않고 남아있다면 새로 시작하기 전에 강제 종료한다.
            if (activeEffects.TryGetValue(lane, out var stale) && stale != null)
            {
                stale.OnHoldReleased(false);
            }

            IHoldAttackEffect effect = InstrumentAttackDispatcher.CreateHoldEffect(type);
            if (effect == null) return;

            effect.Init(level, damage, origin, color, extraProjectiles);
            activeEffects[lane] = effect;
        }

        public static void Tick(RhythmLane lane, float deltaTime)
        {
            if (activeEffects.TryGetValue(lane, out var effect) && effect != null)
            {
                effect.OnHoldTick(deltaTime);
            }
        }

        public static void Release(RhythmLane lane, bool completedFully)
        {
            if (activeEffects.TryGetValue(lane, out var effect) && effect != null)
            {
                effect.OnHoldReleased(completedFully);
            }
            activeEffects.Remove(lane);
        }

        // static이라 씬을 벗어나도 안 비워짐 - 홀드 도중 Gameplay 씬이 언로드되면(플레이어 사망/보스 시간초과 후
        // "메인으로" 복귀 등) Release()가 못 불려서 죽은 이펙트 참조가 여기 남는다. IHoldAttackEffect는 인터페이스라
        // effect != null 검사가 유니티의 "파괴된 오브젝트 = null" 처리를 못 받으므로, 다음 Gameplay 세션에서 같은
        // 레인에 새 홀드가 시작되면 BeginHold()가 그 죽은 참조에 대고 메서드를 호출해 MissingReferenceException으로
        // 죽을 수 있었다. RhythmManager.Awake()가 새 세션 시작 시점에 이걸 호출해 안전하게 비운다.
        public static void ResetForNewSession()
        {
            activeEffects.Clear();
        }
    }
}
