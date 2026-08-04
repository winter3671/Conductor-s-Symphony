using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // "10종 악기별 공격 메커니즘 기획서" 전체 구현 완료: 탭+오토타겟 5종(피아노/벨/마림바/글록켄슈필/드럼)은
    // ITapAttackEffect 딕셔너리 조회로, 홀드 기반 5종(바이올린/프렌치호른/첼로/팀파니/플루트)은
    // IsHoldImplemented()/CreateHoldEffect()로 각각 처리한다. 드럼의 "상시 비트 오라"만은 판정 성공과
    // 무관한 지속 효과라 여기가 아니라 RhythmAttackManager.UpdateDrumAura()가 별도로 담당한다.
    // RhythmAttackManager의 기존 범용 투사체 폴백 로직은 이제 어떤 악기에도 도달하지 않는다.
    //
    // 탭 5종의 개별 구현(피아노/벨/마림바/글록켄슈필/드럼)은 각자의 ITapAttackEffect 클래스 파일로
    // 분리되어 있다 (예: PianoBeamEffect.cs). 이 디스패처는 더 이상 switch문으로 분기하지 않고,
    // 홀드 5종과 동일하게 "타입 → 이펙트" 딕셔너리 조회 패턴을 사용한다.
    public static class InstrumentAttackDispatcher
    {
        private static readonly Dictionary<InstrumentType, ITapAttackEffect> tapEffects = new Dictionary<InstrumentType, ITapAttackEffect>
        {
            { InstrumentType.Piano, new PianoBeamEffect() },
            { InstrumentType.Bell, new BellStarburstEffect() },
            { InstrumentType.Marimba, new MarimbaWaveEffect() },
            { InstrumentType.Glockenspiel, new GlockenspielStarfallEffect() },
            { InstrumentType.Drums, new DrumBeatBangEffect() },
        };

        public static bool IsImplemented(InstrumentType type)
        {
            return tapEffects.ContainsKey(type);
        }

        // 2단계: 홀드 기반 5종(바이올린/프렌치호른/첼로/팀파니/플루트). 탭 5종과 달리 HoldEffectCoordinator를
        // 통해 지속 이펙트(IHoldAttackEffect)로 처리되며, 여기서는 "해당 타입이 홀드 이펙트를 갖는지"만 판별한다.
        public static bool IsHoldImplemented(InstrumentType type)
        {
            return type == InstrumentType.Violin
                || type == InstrumentType.FrenchHorn
                || type == InstrumentType.Cello
                || type == InstrumentType.Timpani
                || type == InstrumentType.Flute;
        }

        // HoldEffectCoordinator가 홀드 시작 시 호출 - 악기 타입에 맞는 지속 이펙트 컴포넌트를 생성해 반환한다.
        public static IHoldAttackEffect CreateHoldEffect(InstrumentType type)
        {
            GameObject obj = new GameObject($"HoldEffect_{type}");
            switch (type)
            {
                case InstrumentType.Violin: return obj.AddComponent<ViolinOrbitEffect>();
                case InstrumentType.FrenchHorn: return obj.AddComponent<FrenchHornConeEffect>();
                case InstrumentType.Cello: return obj.AddComponent<CelloGravityFieldEffect>();
                case InstrumentType.Timpani: return obj.AddComponent<TimpaniBombardmentEffect>();
                case InstrumentType.Flute: return obj.AddComponent<FluteVortexHoldEffect>();
                default:
                    Object.Destroy(obj);
                    return null;
            }
        }

        public static void Execute(InstrumentType type, int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            TapAttackHelpers.EnsureSprites();

            if (tapEffects.TryGetValue(type, out var effect))
            {
                effect.Execute(level, damage, currentCombo, origin, color);
            }
        }
    }
}
