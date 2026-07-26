using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;
using ConductorSymphony.Item;
using ConductorSymphony.Utility;

namespace ConductorSymphony.UI
{
    public class LevelUpUI : MonoSingleton<LevelUpUI>
    {
        [Header("UI Panel & Buttons")]
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Text[] cardTitleTexts;
        [SerializeField] private Image[] cardIconImages;
        [SerializeField] private Text[] cardDescTexts;

        private List<InstrumentInfo> currentChoices = new List<InstrumentInfo>();

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureUIComponents();
        }

        private void OnEnable()
        {
            PlayerExperience.OnLevelUpEvent += HandleLevelUp;
            EliteRewardChest.OnEliteChestCollectedEvent += HandleEliteChestCollected;
        }

        private void OnDisable()
        {
            PlayerExperience.OnLevelUpEvent -= HandleLevelUp;
            EliteRewardChest.OnEliteChestCollectedEvent -= HandleEliteChestCollected;
        }

        private void HandleLevelUp(bool isGameStart)
        {
            ShowLevelUpSelection(isGameStart);
        }

        private void HandleEliteChestCollected()
        {
            ShowEliteRewardSelection();
        }

        private void EnsureUIComponents()
        {
            if (cardPanel == null)
            {
                cardPanel = transform.gameObject;
            }

            // Create EventSystem if missing
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            if (cardButtons == null || cardButtons.Length == 0)
            {
                cardButtons = new Button[3];
                cardTitleTexts = new Text[3];
                cardIconImages = new Image[3];
                cardDescTexts = new Text[3];

                float cardWidth = 220f;

                for (int i = 0; i < 3; i++)
                {
                    GameObject btnObj = new GameObject($"CardButton_{i}");
                    btnObj.transform.SetParent(cardPanel.transform, false);

                    Image img = btnObj.AddComponent<Image>();
                    img.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

                    Button btn = btnObj.AddComponent<Button>();
                    cardButtons[i] = btn;

                    RectTransform rt = btnObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(cardWidth, 270f);
                    rt.anchoredPosition = new Vector2(-250f + (i * 250f), 0f);

                    // Title Text
                    GameObject tObj = new GameObject("TitleText");
                    tObj.transform.SetParent(btnObj.transform, false);
                    Text title = tObj.AddComponent<Text>();
                    title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    title.fontSize = 18;
                    title.color = Color.white;
                    title.alignment = TextAnchor.MiddleCenter;

                    RectTransform trt = tObj.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0, 0.72f);
                    trt.anchorMax = new Vector2(1, 1f);
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;

                    cardTitleTexts[i] = title;

                    // Icon Image (Pixel Art Sprite)
                    GameObject iconObj = new GameObject("IconImage");
                    iconObj.transform.SetParent(btnObj.transform, false);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.preserveAspect = true;

                    RectTransform irt = iconObj.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0.15f, 0.35f);
                    irt.anchorMax = new Vector2(0.85f, 0.72f);
                    irt.offsetMin = Vector2.zero;
                    irt.offsetMax = Vector2.zero;

                    cardIconImages[i] = iconImg;

                    // Desc Text
                    GameObject dObj = new GameObject("DescText");
                    dObj.transform.SetParent(btnObj.transform, false);
                    Text desc = dObj.AddComponent<Text>();
                    desc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    desc.fontSize = 13;
                    desc.color = new Color(0.85f, 0.85f, 0.85f);
                    desc.alignment = TextAnchor.MiddleCenter;

                    RectTransform drt = dObj.GetComponent<RectTransform>();
                    drt.anchorMin = new Vector2(0, 0);
                    drt.anchorMax = new Vector2(1, 0.35f);
                    drt.offsetMin = Vector2.zero;
                    drt.offsetMax = Vector2.zero;

                    cardDescTexts[i] = desc;
                }
            }

            cardPanel.SetActive(false);
        }

        private void Update()
        {
            if (cardPanel != null && cardPanel.activeSelf && Time.timeScale <= 0f)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame && currentChoices.Count >= 1) OnCardSelected(0);
                    if (keyboard.digit2Key.wasPressedThisFrame && currentChoices.Count >= 2) OnCardSelected(1);
                    if (keyboard.digit3Key.wasPressedThisFrame && currentChoices.Count >= 3) OnCardSelected(2);
                }
            }
        }

        public void ShowLevelUpSelection(bool isGameStart = false)
        {
            Time.timeScale = 0.0f; // Pause game
            cardPanel.SetActive(true);

            currentChoices.Clear();
            List<InstrumentType> availableTypes = new List<InstrumentType>();

            List<InstrumentInfo> equipped = InstrumentManager.Instance != null ? InstrumentManager.Instance.AcquiredInstruments : new List<InstrumentInfo>();
            int unlocked = InstrumentManager.Instance != null ? InstrumentManager.Instance.GetUnlockedSlotsCount() : 2;
            bool slotsFull = equipped.Count >= unlocked;

            HashSet<InstrumentGroup> equippedGroups = new HashSet<InstrumentGroup>();
            foreach (var inst in equipped)
            {
                equippedGroups.Add(InstrumentPatternDatabase.GetGroup(inst.type));
            }

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
                if (!slotsFull)
                {
                    foreach (InstrumentType t in System.Enum.GetValues(typeof(InstrumentType)))
                    {
                        if (!InstrumentManager.Instance.HasInstrument(t))
                        {
                            // Exclude instruments that belong to an already equipped group
                            if (!equippedGroups.Contains(InstrumentPatternDatabase.GetGroup(t)))
                            {
                                availableTypes.Add(t);
                            }
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

            // Center alignment for available choices (1, 2, or 3 cards)
            int activeCount = currentChoices.Count;
            float cardWidth = 220f;
            float spacing = 30f;
            float totalWidth = (activeCount * cardWidth) + ((activeCount - 1) * spacing);
            float startX = -totalWidth / 2f + cardWidth / 2f;

            // Setup buttons
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (i < activeCount)
                {
                    cardButtons[i].gameObject.SetActive(true);

                    RectTransform rt = cardButtons[i].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        float posX = startX + i * (cardWidth + spacing);
                        rt.anchoredPosition = new Vector2(posX, 0f);
                    }

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

                    if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                    {
                        Sprite iconSprite = Resources.Load<Sprite>($"Sprites/Instruments/{choice.type}");
                        cardIconImages[i].sprite = iconSprite;
                        cardIconImages[i].color = (iconSprite != null) ? Color.white : Color.clear;
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

        public void ShowEliteRewardSelection()
        {
            ShowLevelUpSelection(isGameStart: false);
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
