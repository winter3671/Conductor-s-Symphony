using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConductorSymphony.Player;
using ConductorSymphony.Instrument;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Rhythm
{
    public class RhythmUI : MonoSingleton<RhythmUI>
    {
        [Header("UI References")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text ratingText;
        [SerializeField] private Text hpText;
        [SerializeField] private Text expText;
        [SerializeField] private Text instrumentSlotText;
        [SerializeField] private Text bossHpText;
        [SerializeField] private Text victoryText;

        private float ratingTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureBossUIElements();
        }

        private void EnsureBossUIElements()
        {
            // Create BossHpText if null
            if (bossHpText == null)
            {
                GameObject bObj = new GameObject("BossHpText");
                bObj.transform.SetParent(transform, false);
                bossHpText = bObj.AddComponent<Text>();
                bossHpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bossHpText.fontSize = 28;
                bossHpText.color = new Color(1.0f, 0.2f, 0.2f);
                bossHpText.alignment = TextAnchor.UpperCenter;
                bossHpText.text = "";

                RectTransform r = bObj.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0, -60);
                r.sizeDelta = new Vector2(600, 40);
                bObj.SetActive(false);
            }

            // Create VictoryText if null
            if (victoryText == null)
            {
                GameObject vObj = new GameObject("VictoryText");
                vObj.transform.SetParent(transform, false);
                victoryText = vObj.AddComponent<Text>();
                victoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                victoryText.fontSize = 42;
                victoryText.color = Color.gold;
                victoryText.alignment = TextAnchor.MiddleCenter;
                victoryText.text = "CONCERTO COMPLETE!\nVICTORY!";

                RectTransform vr = vObj.GetComponent<RectTransform>();
                vr.anchorMin = new Vector2(0.5f, 0.5f);
                vr.anchorMax = new Vector2(0.5f, 0.5f);
                vr.anchoredPosition = Vector2.zero;
                vr.sizeDelta = new Vector2(800, 160);
                vObj.SetActive(false);
            }
        }

        private void OnEnable()
        {
            PlayerController.OnHealthChangedEvent += UpdateHealthUI;
            PlayerExperience.OnExpChangedEvent += UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent += UpdateInstrumentUI;
            RhythmManager.OnScoreUpdatedEvent += HandleScoreUpdated;
            BossMonster.OnBossSpawnedEvent += HandleBossSpawned;
            BossMonster.OnBossHpChangedEvent += UpdateBossHp;
            BossMonster.OnBossDefeatedEvent += HandleBossDefeated;
        }

        private void OnDisable()
        {
            PlayerController.OnHealthChangedEvent -= UpdateHealthUI;
            PlayerExperience.OnExpChangedEvent -= UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent -= UpdateInstrumentUI;
            RhythmManager.OnScoreUpdatedEvent -= HandleScoreUpdated;
            BossMonster.OnBossSpawnedEvent -= HandleBossSpawned;
            BossMonster.OnBossHpChangedEvent -= UpdateBossHp;
            BossMonster.OnBossDefeatedEvent -= HandleBossDefeated;
        }

        private void HandleScoreUpdated(int score, int combo, HitRating rating)
        {
            UpdateScoreAndCombo(score, combo);
            ShowHitRating(rating);
        }

        private void HandleBossSpawned(int maxHp)
        {
            ShowBossHpBar(true, maxHp);
        }

        private void HandleBossDefeated()
        {
            ShowBossHpBar(false, 0);
        }

        private void Update()
        {
            if (ratingTimer > 0f)
            {
                ratingTimer -= Time.deltaTime;
                if (ratingTimer <= 0f && ratingText != null)
                {
                    ratingText.text = "";
                }
            }
        }

        public void UpdateHealthUI(int currentHp, int maxHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {currentHp} / {maxHp}";
            }
        }

        public void UpdateExpUI(int level, int currentExp, int maxExp)
        {
            if (expText != null)
            {
                expText.text = $"LV.{level}  EXP: {currentExp} / {maxExp}";
            }
        }

        public void UpdateInstrumentUI(List<InstrumentInfo> instruments)
        {
            if (instrumentSlotText == null) return;

            int unlocked = InstrumentManager.Instance != null ? InstrumentManager.Instance.GetUnlockedSlotsCount() : 2;
            string[] keyLabels = new string[] { "Q", "R", "W", "E" };
            string[] lockReqs = new string[] { "", "", "Lv.5", "Lv.8" };

            string text = "SLOTS: ";
            for (int i = 0; i < 4; i++)
            {
                if (i < instruments.Count)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(instruments[i].themeColor);
                    text += $"[<color=#FFFF00>{keyLabels[i]}:</color> <color=#{colorHex}>{instruments[i].instrumentName} Lv.{instruments[i].level}</color>] ";
                }
                else if (i < unlocked)
                {
                    text += $"[<color=#FFFF00>{keyLabels[i]}:</color> EMPTY] ";
                }
                else
                {
                    text += $"[<color=#888888>LOCKED ({lockReqs[i]})</color>] ";
                }
            }
            instrumentSlotText.text = text;
        }

        public void UpdateScoreAndCombo(int score, int combo)
        {
            if (scoreText != null) scoreText.text = $"SCORE: {score:N0}";
            if (comboText != null) comboText.text = $"COMBO: {combo}";
        }

        public void ShowHitRating(HitRating rating)
        {
            // Replaced by dynamic 3D world floating text popups (HitFloatingText) above player's head
            if (ratingText != null) ratingText.text = "";
        }

        public void ShowBossHpBar(bool show, int maxHp)
        {
            if (bossHpText != null)
            {
                bossHpText.gameObject.SetActive(show);
                if (show) bossHpText.text = $"★ BOSS HP: {maxHp} / {maxHp} ★";
            }
        }

        public void UpdateBossHp(int currentHp, int maxHp)
        {
            if (bossHpText != null)
            {
                bossHpText.text = $"★ BOSS HP: {currentHp} / {maxHp} ★";
            }
        }

        public void ShowVictoryScreen()
        {
            if (victoryText != null)
            {
                victoryText.gameObject.SetActive(true);
            }
            Time.timeScale = 0f; // Pause game on victory
        }
    }
}
