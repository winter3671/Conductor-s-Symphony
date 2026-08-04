using UnityEngine;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 탭 기반 악기(피아노/벨/마림바/글록켄슈필/드럼)의 즉발 공격 이펙트 공용 인터페이스.
    // 홀드 기반 IHoldAttackEffect와 대칭되는 구조지만, 탭 공격은 상태를 유지하지 않는 1회성 실행이라
    // Init/Tick/Release 생명주기 없이 Execute() 한 번만 호출된다.
    public interface ITapAttackEffect
    {
        void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color);
    }
}
