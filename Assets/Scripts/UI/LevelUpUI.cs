using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ConductorSymphony.Instrument;
using ConductorSymphony.Passive;
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

        // 카드 한 장의 후보 - 악기 업그레이드/신규 악기 또는 패시브 스탯 강화 둘 중 하나를 담는다.
        // (game_balance_design.docx section 2: "성장 구간엔 기존 무기 레벨업 또는 패시브 스탯 강화 무작위 제시")
        private class LevelUpChoice
        {
            public bool isPassive;

            // isPassive == false
            public InstrumentType instrumentType;
            public int instrumentTargetLevel;

            // isPassive == true
            public PassiveStatType passiveType;
            public int passiveTargetLevel;
        }

        private List<LevelUpChoice> currentChoices = new List<LevelUpChoice>();
        private Sprite passiveIconSprite;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            passiveIconSprite = ProceduralSpriteFactory.CreateFilledCircle(32, 12f, Color.white);
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
                    title.font = GameFonts.Headline;
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
                    desc.font = GameFonts.Body;
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

            if (Audio.AudioLayerManager.Instance != null)
            {
                Audio.AudioLayerManager.Instance.PauseAllAudio();
            }

            cardPanel.SetActive(true);

            currentChoices.Clear();
            List<LevelUpChoice> candidates = new List<LevelUpChoice>();

            List<InstrumentInfo> equipped = InstrumentManager.Instance != null ? InstrumentManager.Instance.AcquiredInstruments : new List<InstrumentInfo>();
            int unlocked = InstrumentManager.Instance != null ? InstrumentManager.Instance.GetUnlockedSlotsCount() : 2;
            bool slotsFull = equipped.Count >= unlocked;

            if (!slotsFull)
            {
                // Add all unacquired instruments from remaining 9 instruments
                foreach (InstrumentType t in System.Enum.GetValues(typeof(InstrumentType)))
                {
                    if (!InstrumentManager.Instance.HasInstrument(t))
                    {
                        candidates.Add(new LevelUpChoice { isPassive = false, instrumentType = t, instrumentTargetLevel = 1 });
                    }
                }
            }

            // Add equipped instruments that are not max level (< 5), including Drums
            foreach (var inst in equipped)
            {
                if (inst.level < 5)
                {
                    candidates.Add(new LevelUpChoice { isPassive = false, instrumentType = inst.type, instrumentTargetLevel = inst.level + 1 });
                }
            }

            // 패시브 스탯 8종 (game_balance_design.docx section 4) - 슬롯 제한 없이 레벨 5 미만이면 항상 후보
            if (PassiveStatManager.Instance != null)
            {
                foreach (PassiveStatType pt in PassiveStatDatabase.AllTypes)
                {
                    int curLv = PassiveStatManager.Instance.GetLevel(pt);
                    if (curLv < PassiveStatDatabase.MaxLevel)
                    {
                        candidates.Add(new LevelUpChoice { isPassive = true, passiveType = pt, passiveTargetLevel = curLv + 1 });
                    }
                }
            }

            // Shuffle candidates and pick up to 3
            for (int i = 0; i < candidates.Count; i++)
            {
                LevelUpChoice temp = candidates[i];
                int randomIndex = Random.Range(i, candidates.Count);
                candidates[i] = candidates[randomIndex];
                candidates[randomIndex] = temp;
            }

            int count = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                currentChoices.Add(candidates[i]);
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

                    LevelUpChoice choice = currentChoices[i];

                    int slotIdx = i;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardSelected(slotIdx));

                    if (choice.isPassive)
                    {
                        PassiveStatDefinition def = PassiveStatDatabase.GetDefinition(choice.passiveType);

                        if (cardTitleTexts != null && i < cardTitleTexts.Length)
                        {
                            string colorHex = ColorUtility.ToHtmlStringRGB(def.themeColor);
                            string badge = (choice.passiveTargetLevel == 1) ? "<color=#00FF7F>[NEW]</color>" : $"<color=#FFD700>[Lv.{choice.passiveTargetLevel}]</color>";
                            cardTitleTexts[i].text = $"<color=#FFFF00>[Key {i + 1}]</color>\n<color=#{colorHex}>{def.name}</color> {badge}\n<size=11>({def.theme})</size>";
                        }

                        if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                        {
                            // 패시브 전용 아이콘 아트가 아직 없어 테마 컬러로 물들인 원형 플레이스홀더 사용
                            cardIconImages[i].sprite = passiveIconSprite;
                            cardIconImages[i].color = def.themeColor;
                        }

                        if (cardDescTexts != null && i < cardDescTexts.Length)
                        {
                            cardDescTexts[i].text = def.description;
                        }
                    }
                    else
                    {
                        InstrumentDefinition def = InstrumentPatternDatabase.GetDefinition(choice.instrumentType);
                        InstrumentInfo previewInfo = new InstrumentInfo(choice.instrumentType, choice.instrumentTargetLevel);

                        if (cardTitleTexts != null && i < cardTitleTexts.Length)
                        {
                            string colorHex = ColorUtility.ToHtmlStringRGB(def.themeColor);
                            string badge = (choice.instrumentTargetLevel == 1) ? "<color=#00FF7F>[NEW]</color>" : $"<color=#FFD700>[Lv.{choice.instrumentTargetLevel}]</color>";
                            cardTitleTexts[i].text = $"<color=#FFFF00>[Key {i + 1}]</color>\n<color=#{colorHex}>{def.name}</color> {badge}";
                        }

                        if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                        {
                            Sprite iconSprite = Resources.Load<Sprite>($"Sprites/Instruments/{choice.instrumentType}");
                            cardIconImages[i].sprite = iconSprite;
                            cardIconImages[i].color = (iconSprite != null) ? Color.white : Color.clear;
                        }

                        if (cardDescTexts != null && i < cardDescTexts.Length)
                        {
                            cardDescTexts[i].text = $"{def.description}\n(Dmg +{previewInfo.extraDamage}, Multi +{previewInfo.extraProjectiles})";
                        }
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

            LevelUpChoice selected = currentChoices[index];
            if (selected.isPassive)
            {
                if (PassiveStatManager.Instance != null)
                {
                    PassiveStatManager.Instance.AcquireOrUpgrade(selected.passiveType);
                }
            }
            else
            {
                if (InstrumentManager.Instance != null)
                {
                    InstrumentManager.Instance.AcquireOrUpgradeInstrument(selected.instrumentType);
                }
            }

            if (cardPanel != null) cardPanel.SetActive(false);
            Time.timeScale = 1.0f; // Resume game

            if (Audio.AudioLayerManager.Instance != null)
            {
                Audio.AudioLayerManager.Instance.ResumeAllAudio();
            }
        }
    }
}
