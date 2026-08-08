using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        [SerializeField] private Text defeatText;
        [SerializeField] private Button returnToMenuButton;

        private float ratingTimer = 0f;
        private bool hasEnded = false; // 승리/패배가 이미 한 번 표시되면 이후 이벤트(예: 화면 표시 직후 겹친 콜리전)는 무시

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureBossUIElements();
            EnsureEndScreenElements();
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
        }

        // 승리/패배 공용 종료 화면 요소. 씬에 EventSystem이 없으면(LevelUpUI가 먼저 만들어주는 게 보통이지만
        // 실행 순서를 가정하지 않기 위해) 버튼 클릭이 씹히지 않도록 여기서도 방어적으로 보장한다.
        private void EnsureEndScreenElements()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
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

            // Create DefeatText if null
            if (defeatText == null)
            {
                GameObject dObj = new GameObject("DefeatText");
                dObj.transform.SetParent(transform, false);
                defeatText = dObj.AddComponent<Text>();
                defeatText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                defeatText.fontSize = 42;
                defeatText.color = new Color(0.85f, 0.25f, 0.25f);
                defeatText.alignment = TextAnchor.MiddleCenter;
                defeatText.text = "DEFEAT";

                RectTransform dr = dObj.GetComponent<RectTransform>();
                dr.anchorMin = new Vector2(0.5f, 0.5f);
                dr.anchorMax = new Vector2(0.5f, 0.5f);
                dr.anchoredPosition = Vector2.zero;
                dr.sizeDelta = new Vector2(800, 160);
                dObj.SetActive(false);
            }

            // Create ReturnToMenuButton if null - shared by both Victory and Defeat (only one is ever shown per run).
            if (returnToMenuButton == null)
            {
                GameObject btnObj = new GameObject("ReturnToMenuButton");
                btnObj.transform.SetParent(transform, false);

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

                returnToMenuButton = btnObj.AddComponent<Button>();
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

                RectTransform brt = btnObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0.5f);
                brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, -140f);
                brt.sizeDelta = new Vector2(280f, 64f);

                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                Text label = labelObj.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 22;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
                label.text = "메인으로";

                RectTransform lrt = labelObj.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                btnObj.SetActive(false);
            }
        }

        private void OnEnable()
        {
            PlayerController.OnHealthChangedEvent += UpdateHealthUI;
            PlayerController.OnPlayerDeathEvent += HandlePlayerDied;
            PlayerExperience.OnExpChangedEvent += UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent += UpdateInstrumentUI;
            RhythmManager.OnScoreUpdatedEvent += HandleScoreUpdated;
            BossMonster.OnBossSpawnedEvent += HandleBossSpawned;
            BossMonster.OnBossHpChangedEvent += UpdateBossHp;
            BossMonster.OnBossDefeatedEvent += HandleBossDefeated;
            BossMonster.OnFinalBossClearedEvent += HandleFinalBossCleared;
            BossMonster.OnFinalBossTimeUpEvent += HandleFinalBossTimeUp;
        }

        private void OnDisable()
        {
            PlayerController.OnHealthChangedEvent -= UpdateHealthUI;
            PlayerController.OnPlayerDeathEvent -= HandlePlayerDied;
            PlayerExperience.OnExpChangedEvent -= UpdateExpUI;
            InstrumentManager.OnInstrumentsChangedEvent -= UpdateInstrumentUI;
            RhythmManager.OnScoreUpdatedEvent -= HandleScoreUpdated;
            BossMonster.OnBossSpawnedEvent -= HandleBossSpawned;
            BossMonster.OnBossHpChangedEvent -= UpdateBossHp;
            BossMonster.OnBossDefeatedEvent -= HandleBossDefeated;
            BossMonster.OnFinalBossClearedEvent -= HandleFinalBossCleared;
            BossMonster.OnFinalBossTimeUpEvent -= HandleFinalBossTimeUp;
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

        private void HandleFinalBossCleared()
        {
            ShowVictoryScreen();
        }

        private void HandleFinalBossTimeUp()
        {
            ShowDefeatScreen("TIME OVER\nDEFEAT");
        }

        private void HandlePlayerDied()
        {
            ShowDefeatScreen("YOU DIED\nDEFEAT");
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
            if (hasEnded) return;
            hasEnded = true;

            if (victoryText != null)
            {
                victoryText.gameObject.SetActive(true);
            }
            if (returnToMenuButton != null)
            {
                returnToMenuButton.gameObject.SetActive(true);
            }
            Time.timeScale = 0f; // Pause game on victory
        }

        public void ShowDefeatScreen(string message)
        {
            if (hasEnded) return;
            hasEnded = true;

            if (defeatText != null)
            {
                defeatText.text = message;
                defeatText.gameObject.SetActive(true);
            }
            if (returnToMenuButton != null)
            {
                returnToMenuButton.gameObject.SetActive(true);
            }
            Time.timeScale = 0f; // Pause game on defeat
        }

        private void OnReturnToMenuClicked()
        {
            Time.timeScale = 1f; // MainMenu(및 이후 재진입할 Gameplay)가 멈춰있지 않도록 반드시 복원
            SceneManager.LoadScene("MainMenu");
        }
    }
}
