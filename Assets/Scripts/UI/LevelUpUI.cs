using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        public static LevelUpUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Text[] cardTitleTexts;
        [SerializeField] private Text[] cardDescTexts;

        private List<InstrumentInfo> currentChoices = new List<InstrumentInfo>();
        private bool isGameStartSelection = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureEventSystemExists();

            if (cardPanel != null) cardPanel.SetActive(false);
        }

        private void EnsureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void Start()
        {
            // Trigger Starting Instrument selection at game start
            Invoke(nameof(TriggerGameStartSelection), 0.1f);
        }

        private void TriggerGameStartSelection()
        {
            ShowLevelUpSelection(isGameStart: true);
        }

        private void Update()
        {
            // Allow keyboard shortcuts (1, 2, 3) to select cards even when paused
            if (cardPanel != null && cardPanel.activeSelf)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) OnCardSelected(0);
                    else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) OnCardSelected(1);
                    else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) OnCardSelected(2);
                }
            }
        }

        public void ShowLevelUpSelection(bool isGameStart)
        {
            isGameStartSelection = isGameStart;
            Time.timeScale = 0f; // Pause game

            if (titleText != null)
            {
                titleText.text = isGameStart ? "CHOOSING STARTING INSTRUMENT (Press 1, 2, 3 or Click)" : "LEVEL UP! CHOOSE AN UPGRADE (Press 1, 2, 3 or Click)";
            }

            GenerateChoices(isGameStart);

            if (cardPanel != null) cardPanel.SetActive(true);
        }

        public void ShowEliteRewardSelection()
        {
            isGameStartSelection = false;
            Time.timeScale = 0f; // Pause game

            if (titleText != null)
            {
                titleText.text = "★ ELITE CHEST REWARD! CHOOSE AN UPGRADE ★";
            }

            GenerateChoices(isGameStart: false);

            if (cardPanel != null) cardPanel.SetActive(true);
        }

        private void GenerateChoices(bool isGameStart)
        {
            currentChoices.Clear();
            List<InstrumentType> availableTypes = new List<InstrumentType>();

            if (isGameStart)
            {
                // All 10 instruments available for starting choice
                foreach (InstrumentType t in System.Enum.GetValues(typeof(InstrumentType)))
                {
                    availableTypes.Add(t);
                }
            }
            else
            {
                // If slots < 4, allow unequipped types AND equipped instrument level ups (< 5)
                List<InstrumentInfo> equipped = InstrumentManager.Instance != null ? InstrumentManager.Instance.AcquiredInstruments : new List<InstrumentInfo>();
                bool slotsFull = equipped.Count >= 4;

                if (!slotsFull)
                {
                    foreach (InstrumentType t in System.Enum.GetValues(typeof(InstrumentType)))
                    {
                        if (!InstrumentManager.Instance.HasInstrument(t))
                        {
                            availableTypes.Add(t);
                        }
                    }
                }

                // Add equipped instruments that are not max level (< 5)
                foreach (var inst in equipped)
                {
                    if (inst.level < 5)
                    {
                        availableTypes.Add(inst.type);
                    }
                }
            }

            // Shuffle availableTypes and pick up to 3
            for (int i = 0; i < availableTypes.Count; i++)
            {
                InstrumentType temp = availableTypes[i];
                int randomIndex = Random.Range(i, availableTypes.Count);
                availableTypes[i] = availableTypes[randomIndex];
                availableTypes[randomIndex] = temp;
            }

            int count = Mathf.Min(3, availableTypes.Count);
            for (int i = 0; i < count; i++)
            {
                InstrumentType t = availableTypes[i];
                int currentLv = InstrumentManager.Instance != null ? InstrumentManager.Instance.GetInstrumentLevel(t) : 0;
                InstrumentInfo info = new InstrumentInfo(t, currentLv + 1);
                currentChoices.Add(info);
            }

            // Setup buttons
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (i < currentChoices.Count)
                {
                    cardButtons[i].gameObject.SetActive(true);
                    InstrumentInfo choice = currentChoices[i];
                    InstrumentDefinition def = InstrumentPatternDatabase.GetDefinition(choice.type);

                    int slotIdx = i;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardSelected(slotIdx));

                    if (cardTitleTexts != null && i < cardTitleTexts.Length)
                    {
                        string colorHex = ColorUtility.ToHtmlStringRGB(def.themeColor);
                        string badge = (choice.level == 1) ? "<color=#00FF7F>[NEW]</color>" : $"<color=#FFD700>[Lv.{choice.level}]</color>";
                        cardTitleTexts[i].text = $"<color=#FFFF00>[Key {i + 1}]</color>\n<color=#{colorHex}>{def.name}</color> {badge}";
                    }

                    if (cardDescTexts != null && i < cardDescTexts.Length)
                    {
                        cardDescTexts[i].text = $"{def.description}\n(Dmg +{choice.extraDamage}, Multi +{choice.extraProjectiles})";
                    }
                }
                else
                {
                    if (i < cardButtons.Length) cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void OnCardSelected(int index)
        {
            if (index < 0 || index >= currentChoices.Count) return;

            InstrumentInfo selected = currentChoices[index];
            if (InstrumentManager.Instance != null)
            {
                InstrumentManager.Instance.AcquireOrUpgradeInstrument(selected.type);
            }

            if (cardPanel != null) cardPanel.SetActive(false);
            Time.timeScale = 1.0f; // Resume game
        }
    }
}
