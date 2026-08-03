using UnityEngine;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 홀드(롱노트) 기반 악기(바이올린/프렌치호른/첼로/팀파니)의 지속 공격 이펙트 공용 인터페이스.
    // HoldEffectCoordinator가 RhythmManager의 레인(lane) 단위 홀드 이벤트를 이 인터페이스로 연결한다.
    public interface IHoldAttackEffect
    {
        // 홀드 시작(=최초 판정 성공) 시점에 1회 호출된다. origin은 시전 시점의 플레이어 위치.
        void Init(int level, int damage, Vector3 origin, Color color);

        // 홀드가 유지되는 동안 매 프레임 호출된다 (RhythmManager.OnHoldTickEvent와 동일 주기).
        void OnHoldTick(float deltaTime);

        // 홀드가 끝났을 때(조기 이탈 또는 정상 완료) 1회 호출된다. 이펙트 정리(Destroy 등)는 여기서 수행한다.
        void OnHoldReleased(bool completedFully);
    }
}
