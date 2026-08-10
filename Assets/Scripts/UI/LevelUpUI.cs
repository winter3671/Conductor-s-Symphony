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
        private Text[] cardKeyLabels;  // 카드 "위"에 따로 뜨는 [Key N] 라벨(카드 프레임 밖)
        private Text[] cardThemeTexts; // 하단 패널 맨 아래에 따로 뜨는 테마 뱃지(예: "방어", "기동성")
        private Text levelUpTitleText; // "LEVEL UP!" 문구(씬에 원래 있던 TitleText, CardPanel의 직계 자식)

        // 2026-08-10: 일시정지 메뉴(RhythmUI)가 ESC 입력을 처리할 때, 레벨업 카드 선택 화면이 떠
        // 있는 동안엔 동시에 열리면 안 되므로(둘 다 Time.timeScale=0을 쓰는 별개의 풀스크린
        // 모달이라 겹치면 입력 처리가 꼬임) 이 상태를 밖에서 확인할 수 있도록 노출.
        public bool IsSelectionActive => cardPanel != null && cardPanel.activeSelf;

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

        // 2026-08-10: 카드 프레임 아트(LevelUpCardFrame.png, 1024x1536, 2:3)를 실측 픽셀 분석해 얻은
        // 레이아웃 - 헤더 배너(제목)/중앙 액자 창(아이콘)/하단 패널(설명) 세 구간의 경계.
        // 카드 크기 변천: 220x270(최초) → 380x570(1차 확대) → 570x855(사용자 요청으로 가로/세로
        // 각각 1.5배 추가 확대, 2:3 비율은 유지).
        // 이 값들은 "설계 해상도"(2560x1440, 아래 Reference* 상수)에서의 기준치이고, 실제 화면이
        // 이보다 좁거나 낮으면 ComputeCardScale()이 반환하는 비율만큼 축소해서 씀 - 3차 검증에서
        // 1600x900 같은 좁은 해상도에서 카드 3장이 화면 밖으로 잘리고 "[Key N]"/"LEVEL UP!" 문구가
        // 화면 위로 밀려나 완전히 안 보이는 문제가 실측으로 확인되어 추가.
        private const float BaseCardWidth = 570f;
        private const float BaseCardHeight = 855f; // LevelUpCardFrame.png와 동일한 2:3 비율
        private const float BaseCardSpacing = 30f;

        // 2026-08-10: 사용자 요청 - 카드+"LEVEL UP!" 문구 전체를 살짝 위로 올려서, 카드가 화면
        // 세로 중앙 부근에 오도록 함. 카드와 타이틀 둘 다 이 값만큼 같은 방향으로 올려서 서로의
        // 상대적 간격은 그대로 유지한 채 그룹 전체가 이동하도록 함.
        private const float BaseGroupVerticalShift = 60f;

        // 카드 레이아웃을 처음 확정한 Game 뷰 해상도 - RhythmCanvas의 CanvasScaler가 Constant Pixel
        // Size라 화면이 이보다 작으면 위 Base* 픽셀값들이 그대로 화면을 벗어난다.
        private const float ReferenceScreenWidth = 2560f;
        private const float ReferenceScreenHeight = 1440f;

        // 실제 화면(Screen.width/height)이 설계 해상도보다 작을 때만 축소하는 배율(1.0 = 축소 없음).
        // 가로/세로 중 더 빡빡한 쪽 기준으로 균등 스케일하므로 카드 비율(2:3)은 항상 유지된다.
        private static float ComputeCardScale()
        {
            return Mathf.Min(1f, Mathf.Min(Screen.width / ReferenceScreenWidth, Screen.height / ReferenceScreenHeight));
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            passiveIconSprite = ProceduralSpriteFactory.CreateFilledCircle(32, 12f, Color.white);
            EnsureUIComponents();
            EnsureCardIcons();
            EnsureCardKeyAndThemeLabels();
            EnsureLevelUpTitleStyling();
            EnsureCardVisualUpgrade();
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

                for (int i = 0; i < 3; i++)
                {
                    GameObject btnObj = new GameObject($"CardButton_{i}");
                    btnObj.transform.SetParent(cardPanel.transform, false);

                    Image img = btnObj.AddComponent<Image>();
                    img.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

                    Button btn = btnObj.AddComponent<Button>();
                    cardButtons[i] = btn;

                    RectTransform rt = btnObj.GetComponent<RectTransform>();
                    // 이 블록 실행 후에도 EnsureCardVisualUpgrade()가 항상 다시 한번 덮어쓰므로
                    // 여기 값이 최종 크기와 어긋나도 무해함(기준 크기만 임시로 사용).
                    rt.sizeDelta = new Vector2(BaseCardWidth, BaseCardHeight);
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

        // 2026-08-10: 위 EnsureUIComponents()의 "없으면 생성" 블록은 cardButtons가 비어있을 때만
        // 실행되는데, 실제로는 Gameplay.unity 씬에 카드 버튼 3개가 이미 예전에 에디터에서 직접
        // 만들어져 있어서(코드 생성이 아님) 이 블록이 항상 스킵되고 있었음. 그런데 cardIconImages
        // 필드만은 씬에 빈 배열(`cardIconImages: []`)로 저장돼 있어서 아이콘이 전혀 표시되지 않는
        // 버그가 있었음(사용자가 스크린샷에서 카드에 악기 아이콘이 안 보인다고 지적한 부분과 일치).
        // 각 카드 버튼 아래에 IconImage 자식이 있는지 개별적으로 확인/생성하도록 분리해서, 씬에
        // 버튼이 이미 있든 없든 아이콘만큼은 항상 보장되도록 함.
        private void EnsureCardIcons()
        {
            if (cardButtons == null || cardButtons.Length == 0) return;

            bool needsRebuild = cardIconImages == null || cardIconImages.Length != cardButtons.Length;
            if (!needsRebuild)
            {
                foreach (Image img in cardIconImages)
                {
                    if (img == null) { needsRebuild = true; break; }
                }
            }
            if (!needsRebuild) return;

            cardIconImages = new Image[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] == null) continue;

                Transform existing = cardButtons[i].transform.Find("IconImage");
                Image iconImg;
                if (existing != null)
                {
                    iconImg = existing.GetComponent<Image>();
                    if (iconImg == null) iconImg = existing.gameObject.AddComponent<Image>();
                }
                else
                {
                    GameObject iconObj = new GameObject("IconImage");
                    iconObj.transform.SetParent(cardButtons[i].transform, false);
                    // Image의 [RequireComponent(RectTransform)]에 의해 RectTransform은 자동으로
                    // 붙으므로 별도로 추가하지 않음(다른 Ensure* 메서드들과 동일한 관례).
                    iconImg = iconObj.AddComponent<Image>();
                    iconImg.preserveAspect = true;
                }

                cardIconImages[i] = iconImg;
            }
        }

        // 2026-08-10: 사용자 요청 - "[Key N]"은 카드 프레임 안이 아니라 카드 "위"에 따로 뜨게,
        // 테마 뱃지("방어"/"기동성" 등)는 [NEW] 뒤가 아니라 하단 설명 영역 맨 아래에 따로 뜨게
        // 분리. 두 텍스트 모두 EnsureCardIcons()와 동일한 개별 idempotent 생성 패턴.
        private void EnsureCardKeyAndThemeLabels()
        {
            if (cardButtons == null || cardButtons.Length == 0) return;

            bool needsRebuild = cardKeyLabels == null || cardKeyLabels.Length != cardButtons.Length
                || cardThemeTexts == null || cardThemeTexts.Length != cardButtons.Length;
            if (!needsRebuild)
            {
                for (int i = 0; i < cardButtons.Length; i++)
                {
                    if (cardKeyLabels[i] == null || cardThemeTexts[i] == null) { needsRebuild = true; break; }
                }
            }
            if (!needsRebuild) return;

            cardKeyLabels = new Text[cardButtons.Length];
            cardThemeTexts = new Text[cardButtons.Length];

            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] == null) continue;

                Transform keyExisting = cardButtons[i].transform.Find("KeyLabel");
                Text keyText;
                if (keyExisting != null)
                {
                    keyText = keyExisting.GetComponent<Text>();
                    if (keyText == null) keyText = keyExisting.gameObject.AddComponent<Text>();
                }
                else
                {
                    GameObject keyObj = new GameObject("KeyLabel");
                    keyObj.transform.SetParent(cardButtons[i].transform, false);
                    keyText = keyObj.AddComponent<Text>();
                }
                cardKeyLabels[i] = keyText;

                Transform themeExisting = cardButtons[i].transform.Find("ThemeText");
                Text themeText;
                if (themeExisting != null)
                {
                    themeText = themeExisting.GetComponent<Text>();
                    if (themeText == null) themeText = themeExisting.gameObject.AddComponent<Text>();
                }
                else
                {
                    GameObject themeObj = new GameObject("ThemeText");
                    themeObj.transform.SetParent(cardButtons[i].transform, false);
                    themeText = themeObj.AddComponent<Text>();
                }
                cardThemeTexts[i] = themeText;
            }
        }

        // 2026-08-10: "LEVEL UP!" 문구(씬에 TitleText라는 이름으로 이미 존재 - CardPanel의 직계
        // 자식)는 지금까지 LevelUpUI.cs 어디에서도 참조된 적 없어서(별도 [SerializeField]가 없음)
        // 유니티 기본 폰트/작은 크기(32) 그대로였음. 이름으로 찾아서 폰트/크기/위치를 코드로 관리.
        // "LEVEL UP!" 텍스트가 카드/KeyLabel 위 공간을 얼마나 띄울지 - 카드 절반 높이 + KeyLabel이
        // 차지하는 높이(오프셋 14 + 자기 높이 56) + 여유 간격, 전부 BaseCardHeight와 같은 기준
        // 해상도 단위. scale을 곱해 실제 화면에 맞춤.
        private const float BaseTitleClearance = (BaseCardHeight / 2f) + 14f + 56f + 20f;

        private void EnsureLevelUpTitleStyling()
        {
            if (cardPanel == null) return;

            Transform titleTransform = cardPanel.transform.Find("TitleText");
            if (titleTransform == null) return;

            levelUpTitleText = titleTransform.GetComponent<Text>();
            if (levelUpTitleText == null) return;

            float scale = ComputeCardScale();

            levelUpTitleText.font = GameFonts.Headline;
            levelUpTitleText.fontSize = Mathf.RoundToInt(48 * scale); // 사용자 요청으로 32→48 확대
            Outline titleOutline = levelUpTitleText.GetComponent<Outline>();
            if (titleOutline == null)
            {
                titleOutline = levelUpTitleText.gameObject.AddComponent<Outline>();
                titleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            }
            titleOutline.effectDistance = new Vector2(2f, -2f) * scale;

            // 2026-08-10: Unity MCP 3차 검증에서 발견된 버그 수정 - 원래 타이틀은 화면 세로 85%
            // 지점에 독립적으로 고정된 앵커 + GroupVerticalShift 오프셋으로 배치돼 있었음. 이 85%
            // 앵커는 화면 크기와 무관한 고정 비율이라, 좁은 화면(1600x900 실측)에서 카드+KeyLabel이
            // 차지하는 절대 공간과 타이틀의 85% 위치 사이 여유가 부족해져 "[Key N]" 라벨과 겹치는
            // 문제가 확인됨. 카드와 동일한 화면 중앙 앵커로 바꾸고, 카드 절반 높이 + KeyLabel 공간 +
            // 여유 간격을 직접 더해 "항상 카드+KeyLabel 바로 위"에 오도록 재계산 - 화면 크기가
            // 달라져도 같은 scale 값을 쓰므로 겹칠 일이 없음.
            RectTransform trt = titleTransform.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, (BaseGroupVerticalShift + BaseTitleClearance) * scale);
        }

        // 2026-08-10: 카드 프레임 아트(LevelUpCardFrame.png) 연동 + 카드 확대 + 3구간(제목/아이콘/설명)
        // 배치를 새 아트에 맞춤. 씬에 박제된 예전 크기(220x270)를 Awake()에서 매번 명시적으로
        // 덮어써서, 코드가 항상 최종 권한을 갖도록 함(이 카드 버튼들이 코드 생성이 아니라 씬에 직접
        // 만들어져 있었다는 걸 EnsureCardIcons() 주석에서 발견한 것과 같은 이유).
        private void EnsureCardVisualUpgrade()
        {
            if (cardButtons == null || cardButtons.Length == 0) return;

            Sprite frameSprite = Resources.Load<Sprite>("Sprites/UI/LevelUpCardFrame");
            float scale = ComputeCardScale();

            for (int i = 0; i < cardButtons.Length; i++)
            {
                Button btn = cardButtons[i];
                if (btn == null) continue;

                Image cardImg = btn.GetComponent<Image>();
                if (cardImg != null && frameSprite != null)
                {
                    cardImg.sprite = frameSprite;
                    // 9-slice 대신 Simple 사용 - HP/EXP 바·악기 슬롯 프레임 때와 동일한 이유
                    // (원본 해상도 대비 표시 크기 비율 문제로 9-slice가 깨지는 걸 원천 회피).
                    cardImg.type = Image.Type.Simple;
                    cardImg.color = Color.white;
                }

                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // 2026-08-10: 사용자가 "카드가 화면 세로 중앙에 오면 좋겠다"고 요청 - 씬에
                    // 박제된 예전 앵커 값이 정확히 중앙이 아니었을 가능성을 배제하기 위해, 앵커/
                    // 피벗을 (0.5,0.5)로 명시적으로 강제해서 anchoredPosition.y=0이 항상 정확한
                    // 화면 세로 중앙을 의미하도록 함(이후 ShowLevelUpSelection()에서
                    // GroupVerticalShift만큼 위로 살짝 올림). 크기는 scale로 화면에 맞게 축소.
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(BaseCardWidth * scale, BaseCardHeight * scale);
                }

                // 아이콘을 새 프레임의 중앙 액자 창에 맞춤 - LevelUpCardFrame.png(1024x1536) 픽셀
                // 분석 실측값: 창 내부가 가로 23~76%, 세로 44~77% 구간.
                if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                {
                    RectTransform irt = cardIconImages[i].GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0.25f, 0.46f);
                    irt.anchorMax = new Vector2(0.75f, 0.75f);
                    irt.offsetMin = Vector2.zero;
                    irt.offsetMax = Vector2.zero;
                }

                // 제목을 헤더 배너 구간(세로 80~97.5%)에 맞춤. 헤더에 장식용 트레블 클레프+월계관
                // 그래픽이 있어 텍스트와 겹칠 수 있어서, HP/EXP 바 숫자 오버레이 때와 동일하게
                // Outline을 추가해 가독성을 확보. "[Key N]"은 이제 카드 밖 KeyLabel로 분리됐고
                // 테마 뱃지도 ThemeText로 분리돼서, 제목은 "이름 [뱃지]" 한 줄이면 충분함.
                if (cardTitleTexts != null && i < cardTitleTexts.Length && cardTitleTexts[i] != null)
                {
                    Text title = cardTitleTexts[i];
                    RectTransform trt = title.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0.08f, 0.80f);
                    trt.anchorMax = new Vector2(0.92f, 0.975f);
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                    title.font = GameFonts.Headline;
                    title.fontSize = Mathf.RoundToInt(38 * scale); // 카드 1.5배 확대에 맞춰 26→38
                    Outline titleOutline = title.GetComponent<Outline>();
                    if (titleOutline == null)
                    {
                        titleOutline = title.gameObject.AddComponent<Outline>();
                        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    }
                    titleOutline.effectDistance = new Vector2(1.5f, -1.5f) * scale;
                }

                // 설명을 하단 패널 구간 중 위쪽(세로 17~36%)에 맞춤 - 아래쪽(9~16%)은 테마 뱃지용으로
                // 비워둠(바로 아래 블록).
                if (cardDescTexts != null && i < cardDescTexts.Length && cardDescTexts[i] != null)
                {
                    Text desc = cardDescTexts[i];
                    RectTransform drt = desc.GetComponent<RectTransform>();
                    drt.anchorMin = new Vector2(0.10f, 0.17f);
                    drt.anchorMax = new Vector2(0.90f, 0.36f);
                    drt.offsetMin = Vector2.zero;
                    drt.offsetMax = Vector2.zero;
                    desc.font = GameFonts.Body;
                    desc.fontSize = Mathf.RoundToInt(30 * scale); // 사용자 요청으로 가독성 위해 24→30 추가 확대
                }

                // 2026-08-10: 사용자 요청 - "[Key N]"을 카드 프레임 안이 아니라 카드 바로 위에 별도로
                // 배치. 포인트 앵커(0.5,1) + pivot(0.5,0)으로 카드 상단 바깥쪽에 고정.
                if (cardKeyLabels != null && i < cardKeyLabels.Length && cardKeyLabels[i] != null)
                {
                    Text keyLabel = cardKeyLabels[i];
                    RectTransform krt = keyLabel.GetComponent<RectTransform>();
                    krt.anchorMin = new Vector2(0.5f, 1f);
                    krt.anchorMax = new Vector2(0.5f, 1f);
                    krt.pivot = new Vector2(0.5f, 0f);
                    // KeyLabel은 카드 프레임 "바깥"에 고정 픽셀 오프셋으로 붙어있어(카드 자체의
                    // 앵커 비율과 무관) 카드처럼 자동으로 같이 줄어들지 않음 - scale을 직접 곱해야
                    // 좁은 화면에서 카드 위로 벗어나지 않음(3차 검증 1600x900 실측으로 확인된 문제).
                    krt.sizeDelta = new Vector2(240f, 56f) * scale;
                    krt.anchoredPosition = new Vector2(0f, 14f * scale);
                    keyLabel.font = GameFonts.Headline;
                    keyLabel.fontSize = Mathf.RoundToInt(32 * scale);
                    keyLabel.color = new Color(1f, 1f, 0.4f);
                    keyLabel.alignment = TextAnchor.MiddleCenter;
                    Outline keyOutline = keyLabel.GetComponent<Outline>();
                    if (keyOutline == null)
                    {
                        keyOutline = keyLabel.gameObject.AddComponent<Outline>();
                        keyOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    }
                    keyOutline.effectDistance = new Vector2(1.5f, -1.5f) * scale;
                }

                // 2026-08-10: 사용자 요청 - 테마 뱃지("방어"/"기동성" 등)를 [NEW] 뒤가 아니라 하단
                // 패널 맨 아래(세로 9~16%)로 분리 배치. 악기 카드는 테마 개념이 없어 빈 문자열이라
                // 자동으로 안 보임.
                if (cardThemeTexts != null && i < cardThemeTexts.Length && cardThemeTexts[i] != null)
                {
                    Text themeText = cardThemeTexts[i];
                    RectTransform thrt = themeText.GetComponent<RectTransform>();
                    thrt.anchorMin = new Vector2(0.10f, 0.09f);
                    thrt.anchorMax = new Vector2(0.90f, 0.16f);
                    thrt.offsetMin = Vector2.zero;
                    thrt.offsetMax = Vector2.zero;
                    themeText.font = GameFonts.Body;
                    themeText.fontSize = Mathf.RoundToInt(22 * scale); // 사용자 요청으로 가독성 위해 18→22 확대
                    themeText.color = new Color(0.7f, 0.7f, 0.75f);
                    themeText.alignment = TextAnchor.MiddleCenter;
                }
            }
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

            // 2026-08-10: 카드 크기/폰트/KeyLabel 배치가 Awake() 시점 화면 크기 기준으로 한 번만
            // 계산돼 있으면, 그 이후 창 크기가 바뀌었을 때(에디터 Game 뷰 해상도 변경 등) 다음
            // 레벨업에도 낡은 크기를 쓰게 됨 - 카드를 띄울 때마다 현재 화면 기준으로 다시 계산해
            // 항상 최신 화면 크기에 맞도록 함(idempotent라 매번 다시 불러도 안전).
            EnsureCardVisualUpgrade();
            EnsureLevelUpTitleStyling();

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
                    // 2026-08-10: HUD 작업 실측 중 발견된 별개 버그 수정 - 바로 위 191/192줄은
                    // InstrumentManager.Instance null 체크가 있는데 여기만 없어서, 한 프레임에
                    // 레벨을 여러 번 올려 이 메서드가 재진입하는 등의 상황에서 NullReferenceException으로
                    // 죽을 수 있었음(Time.timeScale=0f를 이미 실행한 뒤라 게임이 영구 정지하는 심각한
                    // 프리즈 버그). 191/192줄과 동일하게 방어.
                    if (InstrumentManager.Instance != null && !InstrumentManager.Instance.HasInstrument(t))
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
            float scale = ComputeCardScale();
            float cardWidth = BaseCardWidth * scale;
            float spacing = BaseCardSpacing * scale;
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
                        // 세로 중앙(0) 대신 GroupVerticalShift만큼 살짝 위로 - TitleText도 동일한
                        // 오프셋만큼 올라가 있어 둘 사이 간격은 그대로 유지됨.
                        rt.anchoredPosition = new Vector2(posX, BaseGroupVerticalShift * scale);
                    }

                    LevelUpChoice choice = currentChoices[i];

                    int slotIdx = i;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardSelected(slotIdx));

                    // [Key N]은 이제 카드 밖 별도 라벨 - 패시브/악기 공통.
                    if (cardKeyLabels != null && i < cardKeyLabels.Length && cardKeyLabels[i] != null)
                    {
                        cardKeyLabels[i].text = $"[Key {i + 1}]";
                    }

                    if (choice.isPassive)
                    {
                        PassiveStatDefinition def = PassiveStatDatabase.GetDefinition(choice.passiveType);

                        if (cardTitleTexts != null && i < cardTitleTexts.Length)
                        {
                            string colorHex = ColorUtility.ToHtmlStringRGB(def.themeColor);
                            string badge = (choice.passiveTargetLevel == 1) ? "<color=#00FF7F>[NEW]</color>" : $"<color=#FFD700>[Lv.{choice.passiveTargetLevel}]</color>";
                            cardTitleTexts[i].text = $"<color=#{colorHex}>{def.name}</color> {badge}";
                        }

                        if (cardThemeTexts != null && i < cardThemeTexts.Length)
                        {
                            cardThemeTexts[i].text = $"({def.theme})";
                        }

                        if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                        {
                            // 2026-08-10: 사용자가 나노바나나로 8종 패시브 도트 아이콘을 전부 제작해
                            // Sprites/Passives/{Type}.png로 저장 완료 - 실제 아트를 우선 시도하고,
                            // 혹시 못 찾으면(파일 없거나 이름 불일치) 기존 절차적 원형 플레이스홀더로
                            // 자동 폴백(악기 아이콘 로딩과 동일한 패턴).
                            Sprite passiveArt = Resources.Load<Sprite>($"Sprites/Passives/{choice.passiveType}");
                            if (passiveArt != null)
                            {
                                cardIconImages[i].sprite = passiveArt;
                                cardIconImages[i].color = Color.white;
                            }
                            else
                            {
                                cardIconImages[i].sprite = passiveIconSprite;
                                cardIconImages[i].color = def.themeColor;
                            }
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
                            cardTitleTexts[i].text = $"<color=#{colorHex}>{def.name}</color> {badge}";
                        }

                        if (cardThemeTexts != null && i < cardThemeTexts.Length)
                        {
                            cardThemeTexts[i].text = ""; // 악기는 테마 개념이 없음
                        }

                        if (cardIconImages != null && i < cardIconImages.Length && cardIconImages[i] != null)
                        {
                            Sprite iconSprite = Resources.Load<Sprite>($"Sprites/Instruments/{choice.instrumentType}");
                            cardIconImages[i].sprite = iconSprite;
                            cardIconImages[i].color = (iconSprite != null) ? Color.white : Color.clear;
                        }

                        if (cardDescTexts != null && i < cardDescTexts.Length)
                        {
                            // 2026-08-10: 사용자 지적 - "(피해 +0, 투사체 +0)"이 버그처럼 보인다는
                            // 피드백. extraDamage/extraProjectiles는 전 악기 공통 범용 보너스(Lv3/5,
                            // Lv4+)일 뿐이라 드럼처럼 Lv2에 별도 배율(범위/피해량 +20%)을 받는 악기는
                            // 실제 효과가 있어도 항상 "+0"으로 보였음. InstrumentLevelStats.cs의 실측
                            // 배율표를 diff한 실제 효과 목록으로 교체 - 하드코딩 문자열이 아니라 값에서
                            // 직접 계산하므로 나중에 밸런스를 조정해도 카드 문구가 같이 따라간다.
                            string effectText = BuildInstrumentLevelUpEffectText(choice.instrumentType, choice.instrumentTargetLevel, previewInfo);
                            cardDescTexts[i].text = string.IsNullOrEmpty(effectText)
                                ? def.description
                                : $"{def.description}\n{effectText}";
                        }
                    }
                }
                else
                {
                    if (i < cardButtons.Length) cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // 2026-08-10: 카드 설명란 맨 아래 괄호 문구를 만드는 헬퍼. targetLevel<=1(신규 습득)이면
        // "레벨업 효과"라는 개념 자체가 없으므로 null을 돌려줘 def.description만 보여준다.
        // InstrumentLevelStats.GetLevelUpHighlights()가 뽑아준 실측 배율 변화(범위/피해량/넉백 등)에,
        // 전 악기 공통 범용 보너스인 extraDamage/extraProjectiles의 델타(Lv3/5, Lv4+에서만 발생)를
        // 덧붙인다 - 이 둘은 서로 다른 별개의 보너스 체계라 중복이 아니라 병기.
        private string BuildInstrumentLevelUpEffectText(InstrumentType type, int targetLevel, InstrumentInfo previewInfo)
        {
            if (targetLevel <= 1) return null;

            List<string> highlights = InstrumentLevelStats.GetLevelUpHighlights(type, targetLevel);

            InstrumentInfo prevInfo = new InstrumentInfo(type, targetLevel - 1);
            if (previewInfo.extraDamage != prevInfo.extraDamage)
            {
                highlights.Add($"고정 피해 +{previewInfo.extraDamage - prevInfo.extraDamage}");
            }
            if (previewInfo.extraProjectiles != prevInfo.extraProjectiles)
            {
                highlights.Add($"투사체 +{previewInfo.extraProjectiles - prevInfo.extraProjectiles}");
            }

            if (highlights.Count == 0) return null;
            return $"({string.Join(", ", highlights)})";
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
