using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConductorSymphony.Player;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.Rhythm
{
    public class RhythmUI : MonoBehaviour
    {
        public static RhythmUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text ratingText;
        [SerializeField] private Text hpText;
        [SerializeField] private Text expText;
        [SerializeField] private Text instrumentSlotText;

        private float ratingTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            PlayerController.OnHealthChangedEvent += UpdateHealthUI;
            PlayerExperience.OnExpChangedEvent += UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent += UpdateInstrumentUI;
        }

        private void OnDisable()
        {
            PlayerController.OnHealthChangedEvent -= UpdateHealthUI;
            PlayerExperience.OnExpChangedEvent -= UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent -= UpdateInstrumentUI;
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

            string text = "INSTRUMENTS: ";
            for (int i = 0; i < 4; i++)
            {
                if (i < instruments.Count)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(instruments[i].themeColor);
                    text += $"[<color=#{colorHex}>{instruments[i].instrumentName} Lv.{instruments[i].level}</color>] ";
                }
                else
                {
                    text += "[ EMPTY ] ";
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
            if (ratingText == null) return;

            switch (rating)
            {
                case HitRating.Perfect:
                    ratingText.text = "<color=#FFD700>PERFECT!</color>";
                    break;
                case HitRating.Great:
                    ratingText.text = "<color=#00FF7F>GREAT!</color>";
                    break;
                case HitRating.Miss:
                    ratingText.text = "<color=#FF4500>MISS</color>";
                    break;
            }

            ratingTimer = 0.8f;
        }
    }
}
