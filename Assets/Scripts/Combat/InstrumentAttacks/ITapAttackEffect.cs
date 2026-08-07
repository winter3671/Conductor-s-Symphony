using UnityEngine;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 탭 기반 악기(피아노/벨/마림바/글록켄슈필/드럼)의 즉발 공격 이펙트 공용 인터페이스.
    // 홀드 기반 IHoldAttackEffect와 대칭되는 구조지만, 탭 공격은 상태를 유지하지 않는 1회성 실행이라
    // Init/Tick/Release 생명주기 없이 Execute() 한 번만 호출된다.
    public interface ITapAttackEffect
    {
        // extraProjectiles: 레가토(Legato) 패시브 + 악기 Lv4 "Multi+1" 스탯의 합산치(2026-08-07 연동).
        // "낱개로 셀 수 있는 발사체/낙하체"가 있는 악기(피아노/벨/마림바/글록켄슈필)만 실제로 소비하고,
        // 드럼은 광역 판정이라 개념이 안 맞아 무시한다(DrumBeatBangEffect 주석 참고).
        void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles);
    }
}
