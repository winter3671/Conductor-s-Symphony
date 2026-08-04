using UnityEngine;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.Combat
{
    // 최종 딜량 공식(game_balance_design.docx section 1: 기본 DPS × M_rhythm × M_stat)과 악기별 DPS
    // 보정 배율(Docs/dps_balance_gap_analysis.md)을 순수 정적 함수로 분리했다.
    //
    // (리팩토링 배경) 원래 RhythmAttackManager.HandleRhythmHit() 안에 다른 책임(장착 악기 조회, 사운드
    // 재생, 탭/홀드 분기 등)과 뒤섞여 있어서, 공식만 검증하려 해도 싱글톤(RhythmAttackManager,
    // PlayerController, InstrumentManager 등)을 전부 띄운 Play Mode가 필요했다 - 이번 대화에서 진행한
    // DPS 밸런스 테스트 라운드마다 private 필드를 리플렉션으로 주입해야 했던 이유 중 하나였다. 이
    // 클래스는 MonoBehaviour도 싱글톤도 아니므로, Edit Mode에서 인자만 넣어 바로 호출해 검증할 수 있다.
    //
    // RhythmAttackManager.HandleRhythmHit()(판정 성공 데미지)와 DrumAuraController(드럼 상시 오라
    // 데미지)가 이 공식을 공유한다.
    public static class DamageFormula
    {
        // 판정 등급(Perfect=2/그 외=1) + 공유 extraDamage로 "보정 전 기본 데미지"를 계산한다.
        public static int ComputeBaseDamage(HitRating rating, int extraDamage)
        {
            return ((rating == HitRating.Perfect) ? 2 : 1) + extraDamage;
        }

        // 최종 딜량 = 기본 데미지 × M_rhythm × M_stat × 악기별 DPS 보정 배율, 최소 1 보장.
        // 드럼 상시 오라처럼 M_rhythm을 의도적으로 제외하는 경우 mRhythm=1f를 넘기면 된다.
        public static int ComputeFinalDamage(float baseDamage, float mRhythm, float mStat, float instrumentDpsMultiplier)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat * instrumentDpsMultiplier));
        }
    }
}
