using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat
{
    // 드럼 "상시 비트 오라(Beat Aura)" 전담 컴포넌트 - 판정 성공 여부와 완전히 무관하게, 드럼이 언락된
    // 슬롯에 장착되어 있는 동안 항상 켜져 있는 지속 효과. 정박 타격 시의 "비트 뱅"(넉백 파동)은 다른
    // 악기들과 동일하게 DrumBeatBangEffect(InstrumentAttackDispatcher가 위임)가 담당하고, 이 오라는 별개다.
    //
    // (리팩토링 배경) 원래 RhythmAttackManager 안에 판정 디스패치 로직과 뒤섞여 있었으나, 서로 무관한
    // 두 책임을 분리하기 위해 별도 컴포넌트로 뽑았다. MonoSingleton으로 만들지 않고
    // RhythmAttackManager.Start()가 자식 GameObject로 직접 생성한다 - 씬 파일에 수동으로 배치해야 하는
    // 매니저를 새로 늘리지 않기 위한 선택이다(과거 PassiveStatManager가 씬 배치 자체를 빠뜨려서 겪었던
    // "코드는 맞는데 씬에 존재하지 않아 아무 효과도 없던" 버그를 반복하지 않기 위함).
    public class DrumAuraController : MonoBehaviour
    {
        private PlayerController player;
        private GameObject auraVisual;
        private bool auraActive;
        private float tickTimer;

        private const float TickInterval = 0.5f;
        private const float BaseRadius = 1.6f;

        private void OnDestroy()
        {
            if (auraVisual != null) Destroy(auraVisual);
        }

        // 판정 성공 이벤트와 완전히 별개로 매 프레임 호출된다 - "드럼이 언락된 슬롯에 장착되어 있는가"만
        // 확인하고, 그렇다면 최근 판정 성공 여부와 무관하게 플레이어 주변에 소량의 지속 타격을 가한다.
        private void Update()
        {
            bool shouldBeActive = IsDrumsActive();
            if (shouldBeActive != auraActive)
            {
                auraActive = shouldBeActive;
                SetVisualActive(shouldBeActive);
            }

            if (!auraActive) return;
            if (player == null) player = PlayerController.Instance;
            if (player == null) return;

            int drumLevel = Instrument.InstrumentManager.Instance != null
                ? Instrument.InstrumentManager.Instance.GetInstrumentLevel(Instrument.InstrumentType.Drums)
                : 1;
            float radius = (BaseRadius + 0.1f * Mathf.Max(0, drumLevel - 1)) * CombatTargetingUtility.GetRangeMultiplier();

            if (auraVisual != null)
            {
                auraVisual.transform.position = player.transform.position;
                // 실제 판정 반경(레벨업/범위 패시브 반영)에 맞춰 매 프레임 갱신 - 이전엔 최초 생성 시
                // BaseRadius 기준 고정 크기로만 그려서, 레벨이 오르거나 범위 패시브를 먹어도 오라가
                // 실제로 얼마나 넓어졌는지 화면으로는 전혀 알 수 없었다.
                auraVisual.transform.localScale = Vector3.one * radius;
            }

            // 알레그로(Allegro) 패시브 "쿨타임 감축" 반영 - 값이 작을수록(배율<1) 더 자주 틱.
            float tickInterval = TickInterval * CombatTargetingUtility.GetCooldownMultiplier();
            tickTimer += Time.deltaTime;
            if (tickTimer < tickInterval) return;
            tickTimer = 0f;

            // 오라는 판정 성공 여부와 무관한 baseline 효과라 M_rhythm(리듬 정확도 배율)은 의도적으로
            // 적용하지 않는다(DamageFormula에 mRhythm=1f 고정으로 전달) - 시포르찬도(M_stat) 패시브만
            // 반영한다. 밸런스 doc 5번 항목 Lv4 "패시브 비트 오라 지속 피해량 +50%"와
            // Docs/dps_balance_gap_analysis.md의 악기별 DPS 보정 배율을 함께 반영한다(비트 뱅에만
            // 배율을 적용하면 오라 몫만큼 계속 목표 DPS에 못 미치게 된다).
            float mStat = Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetDamageMultiplier() : 1.0f;
            float auraLevelMultiplier = (drumLevel >= 4) ? 1.5f : 1f;
            float instrumentDpsMultiplier = Instrument.InstrumentDamageTable.GetDamageMultiplier(Instrument.InstrumentType.Drums, drumLevel);
            int auraDamage = DamageFormula.ComputeFinalDamage(1, 1f, mStat * auraLevelMultiplier, instrumentDpsMultiplier);

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;
                if (Vector3.Distance(player.transform.position, enemy.transform.position) <= radius)
                {
                    enemy.TakeDamage(auraDamage);
                }
            }
        }

        private bool IsDrumsActive()
        {
            var mgr = Instrument.InstrumentManager.Instance;
            if (mgr == null) return false;

            var equipped = mgr.AcquiredInstruments;
            int maxUnlocked = mgr.GetUnlockedSlotsCount();
            for (int slot = 0; slot < equipped.Count && slot < maxUnlocked; slot++)
            {
                if (equipped[slot].type == Instrument.InstrumentType.Drums) return true;
            }
            return false;
        }

        private void SetVisualActive(bool active)
        {
            if (active)
            {
                if (auraVisual == null)
                {
                    auraVisual = new GameObject("DrumBeatAura");
                    SpriteRenderer sr = auraVisual.AddComponent<SpriteRenderer>();
                    // CreateUnitRing: scale=1일 때 링 바깥쪽 끝이 정확히 반지름 1 유닛이 되므로,
                    // localScale = Vector3.one * 실제판정반경만 하면 항상 정확히 일치한다(매 프레임
                    // Update()에서 갱신 - 최초 생성 시점의 크기는 다음 프레임에 바로 덮어써진다).
                    sr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(0.9f, 0.3f, 0.3f, 0.6f));
                    sr.sortingOrder = 2;
                }
                auraVisual.SetActive(true);
            }
            else if (auraVisual != null)
            {
                auraVisual.SetActive(false);
            }
        }
    }
}
