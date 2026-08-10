using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ConductorSymphony.Player;
using ConductorSymphony.Instrument;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;
using ConductorSymphony.Audio;
using ConductorSymphony.Settings;
using ConductorSymphony.UI;

namespace ConductorSymphony.Rhythm
{
    public class RhythmUI : MonoSingleton<RhythmUI>
    {
        [Header("UI References")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text ratingText;
        [SerializeField] private Text bossHpText;
        [SerializeField] private Text victoryText;
        [SerializeField] private Text defeatText;
        [SerializeField] private Button returnToMenuButton;

        // 2026-08-10: HP/EXP는 "HP: 80/100" 텍스트를 완전히 대체하는 바 그래프로 전환.
        // 기존 hpText/expText/instrumentSlotText [SerializeField]는 제거 - 아래 필드들은 전부
        // EnsureHealthExpBarElements()/EnsureInstrumentSlotElements()가 씬/프리팹 편집 없이
        // 코드로 자동 생성한다(bossHpText 등 기존 Ensure* 패턴과 동일한 방식).
        [Header("HP / EXP Bar")]
        private Image hpBarFrame;
        private Image hpBarFill;
        private Text hpValueText;
        private Image expBarFrame;
        private Image expBarFill;
        private Text expValueText;
        private Text expLevelText;

        [Header("Instrument Slots (Q/R/W/E)")]
        private readonly Image[] slotFrames = new Image[4];
        private readonly Image[] slotIcons = new Image[4];
        private readonly Text[] slotKeyLabels = new Text[4];
        private readonly Text[] slotLevelLabels = new Text[4];
        private readonly Text[] slotLockMessages = new Text[4];

        // 2026-08-10: ESC 일시정지 메뉴. 씬/프리팹 편집 없이 기존 Ensure*Elements() 패턴을 그대로
        // 따라 코드로 생성한다(MainMenu 버튼 스타일을 코드로 복제 - MainMenuCanvas.prefab의
        // StartButton 등이 커스텀 스프라이트 없이 순수 Image.color + Button.ColorTint 조합이라
        // 코드로 그대로 재현 가능했음). "환경설정"은 사용자 결정에 따라 볼륨 3종 조절만 지원하는
        // 축소판(메인 메뉴의 키 리바인드/싱크 보정 화면과는 별개)으로 구성.
        [Header("Pause Menu")]
        private GameObject pauseMenuRoot;       // 다임(dim) 오버레이 - 항상 존재, 하위 3개 패널의 공통 부모
        private GameObject pauseButtonsPanel;   // 계속하기/환경설정/메인으로/게임종료
        private GameObject pauseSettingsPanel;  // 볼륨 3종 슬라이더 + 뒤로가기
        private GameObject confirmDialogPanel;  // 메시지 + 취소/확인 (메인으로/게임종료 공용)
        private Text confirmMessageText;
        private System.Action pendingConfirmAction;
        private bool isPaused = false;

        private float ratingTimer = 0f;
        private bool hasEnded = false; // 승리/패배가 이미 한 번 표시되면 이후 이벤트(예: 화면 표시 직후 겹친 콜리전)는 무시

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DestroyLegacyTextElements();
            EnsureBossUIElements();
            EnsureEndScreenElements();
            EnsureHealthExpBarElements();
            EnsureInstrumentSlotElements();
            EnsurePauseMenuElements();
        }

        // 2026-08-10: Unity MCP 실측에서 발견된 회귀 - 이번 세션 이전에 존재하던 레거시
        // HPText/ExpText/InstrumentSlotText GameObject가 씬(Gameplay.unity)에 여전히 active
        // 상태로 남아있어 새 HP/EXP 바·악기 슬롯과 겹쳐 보였음. 더 이상 어떤 코드도 이 오브젝트를
        // 참조하지 않으므로(hpText/expText/instrumentSlotText 필드 자체를 제거함) 씬을 직접
        // 편집하는 대신(README §5.3) 런타임에 이름으로 찾아 비활성화해서 방어적으로 처리.
        private void DestroyLegacyTextElements()
        {
            string[] legacyNames = { "HPText", "ExpText", "InstrumentSlotText" };
            Transform searchRoot = transform.parent != null ? transform.parent : transform;
            foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                foreach (string legacyName in legacyNames)
                {
                    if (t.name == legacyName && t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
        }

        private Sprite LoadUISprite(string name)
        {
            // Sprites/UI/ 아래에 나노바나나로 만든 프레임 아트가 없으면 null이 반환되고,
            // 아래 Ensure* 메서드들이 임시 반투명 사각형으로 대체한다 - 나중에 파일만 채워
            // 넣으면 코드 수정 없이 자동으로 적용된다(배경 타일 아트 때와 동일한 패턴).
            return Resources.Load<Sprite>($"Sprites/UI/{name}");
        }

        // 2026-08-10: "HP: 80/100" 텍스트 → 실제 채워지는 바 그래프로 전환. 프레임(테두리) 아트는
        // Sprites/UI/HpBarFrame, Sprites/UI/ExpBarFrame을 시도하고, 채움(fill)은 별도 아트 없이
        // Image.Type.Filled + 단색으로 처리(9-slice 프레임 아트만 있으면 충분히 폴리싱되어 보임).
        private void EnsureHealthExpBarElements()
        {
            if (hpBarFill != null && expBarFill != null) return;

            GameObject root = new GameObject("HealthExpBars");
            root.transform.SetParent(transform, false);
            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            // 2026-08-10: Unity MCP 실측에서 기존 ScoreText(anchoredPosition 30,-30 / 300x50,
            // y범위 [-30,-80])·ComboText(30,-70 / 300x50, y범위 [-70,-120])와 HP/EXP 바가 세로로
            // 겹치는 회귀가 확인됨(HP바가 SCORE 대부분을, EXP바가 COMBO 전체를 가림). Score/Combo
            // 텍스트 최하단(y=-120) 아래로 12px 여유를 두고 시작하도록 -132로 내림.
            rootRt.anchoredPosition = new Vector2(24f, -132f);
            // 2026-08-10: 사용자 요청으로 바 안에 "72/100" 형태 숫자를 얹기로 함 - 숫자가 잘 보이려면
            // 바 자체가 더 커야 해서 HP 260x50→320x64, EXP 260x38→320x48로 재확대(HpBarFrame.png/
            // ExpBarFrame.png 원본 비율 3.2:1/4.6:1과 크게 어긋나지 않는 선에서 키움).
            rootRt.sizeDelta = new Vector2(400f, 120f); // HP(64)+간격(8)+EXP(48)

            // HP Bar
            GameObject hpFrameObj = new GameObject("HpBarFrame");
            hpFrameObj.transform.SetParent(root.transform, false);
            hpBarFrame = hpFrameObj.AddComponent<Image>();
            Sprite hpFrameSprite = LoadUISprite("HpBarFrame");
            if (hpFrameSprite != null)
            {
                hpBarFrame.sprite = hpFrameSprite;
                // 2026-08-10: Sliced(9-slice) → Simple로 변경. 원본 아트가 2260x696처럼 고해상도인데
                // 실제 표시 크기는 320x64 등으로 훨씬 작아서, .meta의 spriteBorder(픽셀 분석으로
                // 추정한 값)가 타겟 크기 대비 지나치게 커버려 9-slice 모서리 연산이 찌그러지거나
                // 아예 깨져 보이는 문제가 반복됨(사용자 실측: EXP 바/악기 슬롯 프레임이 거의 안
                // 보이거나 조각나 보임). Simple은 border를 무시하고 스프라이트 전체를 타겟 크기에
                // 맞춰 단순히 늘려서 그리므로 이 문제 자체가 발생하지 않음 - 디자인이 복잡한
                // 모서리 장식이 있는 게 아니라 얇은 테두리 위주라 늘어나도 티가 잘 안 남.
                hpBarFrame.type = Image.Type.Simple;
            }
            else
            {
                hpBarFrame.color = new Color(0.05f, 0.05f, 0.1f, 0.6f); // 아트 대기용 임시 배경
            }
            RectTransform hpFrameRt = hpFrameObj.GetComponent<RectTransform>();
            hpFrameRt.anchorMin = new Vector2(0f, 1f);
            hpFrameRt.anchorMax = new Vector2(0f, 1f);
            hpFrameRt.pivot = new Vector2(0f, 1f);
            hpFrameRt.anchoredPosition = Vector2.zero;
            hpFrameRt.sizeDelta = new Vector2(320f, 64f);

            GameObject hpFillObj = new GameObject("HpBarFill");
            hpFillObj.transform.SetParent(hpFrameObj.transform, false);
            hpBarFill = hpFillObj.AddComponent<Image>();
            // 2026-08-10 버그 수정: sprite가 null인 Image에 Type.Filled를 쓰면 fillAmount 프로퍼티는
            // 정상적으로 바뀌는데(그래서 지난 라운드 리플렉션 검증은 통과했었음) 실제 화면엔 항상
            // 꽉 찬 사각형으로만 렌더링되고 잘림(clip)이 전혀 반영되지 않는 uGUI 특성이 있음 -
            // Unity가 Filled의 잘림 메시를 스프라이트 UV 기준으로 생성하기 때문. 배경 스크롤 때
            // mainTextureOffset이 셰이더에서 무시되던 것과 같은 부류의 "값은 맞는데 화면엔 반영 안
            // 되는" 검증 함정. ProceduralSpriteFactory로 흰색 1x1 스프라이트를 만들어 채워주면
            // 해결됨(다른 곳에서 이미 쓰던 유틸리티 재사용).
            hpBarFill.sprite = ProceduralSpriteFactory.CreateUnitSquare(Color.white);
            hpBarFill.color = new Color(0.85f, 0.2f, 0.25f); // 붉은 계열 HP
            hpBarFill.type = Image.Type.Filled;
            hpBarFill.fillMethod = Image.FillMethod.Horizontal;
            hpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpBarFill.fillAmount = 1f;
            RectTransform hpFillRt = hpFillObj.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            // 2026-08-10: 사용자 실측 - 채움 바가 프레임 테두리 위아래로 살짝 삐져나와 보임. 여백을
            // 좌우(14)/상하(11)로 나눠서 프레임의 실제 테두리 두께 비율(HpBarFrame.png 테두리 실측
            // 비율을 320x64 타겟에 대입한 값)에 더 가깝게 조정. 계속 안 맞으면 이 offsetMin/Max
            // 값을 직접 조절하면 됨 - x는 좌우 여백, y는 상하 여백.
            hpFillRt.offsetMin = new Vector2(20f, 15f);
            hpFillRt.offsetMax = new Vector2(-20f, -15f);

            // 2026-08-10: 바가 절반만 채워져도(채움 색 위 절반 + 빈 배경 위 절반) 숫자가 둘 다에서
            // 잘 읽히도록 흰 글씨 + 검은 Outline 조합 사용(어느 쪽 배경에서도 대비가 확보되는
            // 표준적인 방식). hpFrameObj의 자식 중 hpFillObj보다 나중에 만들어서 항상 그 위에 그려짐.
            GameObject hpValueObj = new GameObject("HpValueText");
            hpValueObj.transform.SetParent(hpFrameObj.transform, false);
            hpValueText = hpValueObj.AddComponent<Text>();
            hpValueText.font = GameFonts.Headline;
            hpValueText.fontSize = 26;
            hpValueText.color = Color.white;
            hpValueText.alignment = TextAnchor.MiddleCenter;
            hpValueText.text = "100 / 100";
            Outline hpValueOutline = hpValueObj.AddComponent<Outline>();
            hpValueOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            hpValueOutline.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform hpValueRt = hpValueObj.GetComponent<RectTransform>();
            hpValueRt.anchorMin = Vector2.zero;
            hpValueRt.anchorMax = Vector2.one;
            hpValueRt.offsetMin = Vector2.zero;
            hpValueRt.offsetMax = Vector2.zero;

            // EXP Bar (HP 바로 아래)
            GameObject expFrameObj = new GameObject("ExpBarFrame");
            expFrameObj.transform.SetParent(root.transform, false);
            expBarFrame = expFrameObj.AddComponent<Image>();
            Sprite expFrameSprite = LoadUISprite("ExpBarFrame");
            if (expFrameSprite != null)
            {
                expBarFrame.sprite = expFrameSprite;
                expBarFrame.type = Image.Type.Simple; // 이유는 위 hpBarFrame 주석 참고
            }
            else
            {
                expBarFrame.color = new Color(0.05f, 0.05f, 0.1f, 0.6f);
            }
            RectTransform expFrameRt = expFrameObj.GetComponent<RectTransform>();
            expFrameRt.anchorMin = new Vector2(0f, 1f);
            expFrameRt.anchorMax = new Vector2(0f, 1f);
            expFrameRt.pivot = new Vector2(0f, 1f);
            expFrameRt.anchoredPosition = new Vector2(0f, -72f); // HP 바(64 높이) + 8 간격
            expFrameRt.sizeDelta = new Vector2(320f, 48f);

            GameObject expFillObj = new GameObject("ExpBarFill");
            expFillObj.transform.SetParent(expFrameObj.transform, false);
            expBarFill = expFillObj.AddComponent<Image>();
            // 2026-08-10: HP 바와 동일한 이유로 흰색 스프라이트 부여(sprite=null이면 fillAmount가
            // 화면에 반영 안 됨 - 위 hpBarFill 주석 참고).
            expBarFill.sprite = ProceduralSpriteFactory.CreateUnitSquare(Color.white);
            // 2026-08-10: 사용자 요청으로 시안(파란) 계열 → 초록/연두 계열로 변경.
            expBarFill.color = new Color(0.45f, 0.85f, 0.25f);
            expBarFill.type = Image.Type.Filled;
            expBarFill.fillMethod = Image.FillMethod.Horizontal;
            expBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            expBarFill.fillAmount = 0f;
            RectTransform expFillRt = expFillObj.GetComponent<RectTransform>();
            expFillRt.anchorMin = Vector2.zero;
            expFillRt.anchorMax = Vector2.one;
            // 2026-08-10: HP와 같은 이유로 좌우(8)/상하(7)로 나눠 조정. ExpBarFrame.png가 HP보다
            // 얇은 프레임이라 여백도 약간 더 작게.
            expFillRt.offsetMin = new Vector2(17f, 15f);
            expFillRt.offsetMax = new Vector2(-17f, -15f);

            GameObject expValueObj = new GameObject("ExpValueText");
            expValueObj.transform.SetParent(expFrameObj.transform, false);
            expValueText = expValueObj.AddComponent<Text>();
            expValueText.font = GameFonts.Headline;
            expValueText.fontSize = 18;
            expValueText.color = Color.white;
            expValueText.alignment = TextAnchor.MiddleCenter;
            expValueText.text = "0 / 100";
            Outline expValueOutline = expValueObj.AddComponent<Outline>();
            expValueOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            expValueOutline.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform expValueRt = expValueObj.GetComponent<RectTransform>();
            expValueRt.anchorMin = Vector2.zero;
            expValueRt.anchorMax = Vector2.one;
            expValueRt.offsetMin = Vector2.zero;
            expValueRt.offsetMax = Vector2.zero;

            GameObject expLevelObj = new GameObject("ExpLevelText");
            expLevelObj.transform.SetParent(root.transform, false);
            expLevelText = expLevelObj.AddComponent<Text>();
            expLevelText.font = GameFonts.Body;
            expLevelText.fontSize = 16;
            expLevelText.color = Color.white;
            expLevelText.alignment = TextAnchor.MiddleLeft;
            expLevelText.text = "Lv.1";
            RectTransform expLevelRt = expLevelObj.GetComponent<RectTransform>();
            expLevelRt.anchorMin = new Vector2(0f, 1f);
            expLevelRt.anchorMax = new Vector2(0f, 1f);
            expLevelRt.pivot = new Vector2(0f, 1f);
            expLevelRt.anchoredPosition = new Vector2(328f, -72f); // EXP 바와 같은 top y좌표
            // 2026-08-10: 사용자 실측 - "Lv.N" 라벨이 EXP 바보다 살짝 위에 떠 보임. sizeDelta.y를
            // EXP 바 높이(48, expFrameRt와 동일)로 맞추면 MiddleLeft 정렬 텍스트가 바와 정확히
            // 같은 세로 중심에 놓임(기존 22는 바 높이보다 작아서 위쪽으로 치우쳐 보였음).
            expLevelRt.sizeDelta = new Vector2(60f, 48f);
        }

        // 2026-08-10: "SLOTS: [Q: Piano Lv.2] [R: EMPTY] ..." 한 줄 텍스트 → Q/R/W/E 4칸 아이콘
        // 슬롯으로 전환. 아이콘은 이미 있는 Resources/Sprites/Instruments/{Type}.png를 그대로
        // 재사용(신규 아트 불필요) - 슬롯 테두리(Sprites/UI/InstrumentSlotFrame)만 신규 아트 대기 중,
        // 없으면 임시 사각형으로 표시된다.
        //
        // 2026-08-10 추가 수정(사용자 실측): (1) 너무 작아서 안 보였다는 피드백으로 56px→124px로
        // 약 2.2배 확대. (2) 화면 표시 순서를 실제 장착 데이터 순서(Q=0,R=1,W=2,E=3, RhythmManager.
        // GetLaneForSlot() 기준)와 다르게 만듦 - 판정 링이 Q(왼쪽,180도)/W(왼쪽위,135도)/E(오른쪽위,
        // 45도)/R(오른쪽,0도) 순으로 배치돼 있어서(RhythmManager.GetLaneDirection), 화면 왼쪽→오른쪽
        // 순서가 Q,W,E,R이 되도록 슬롯 표시 순서만 재배열(SlotDisplayToDataIndex). 실제 장착
        // 데이터(instruments 리스트, GetUnlockedSlotsCount 판정 등)는 여전히 Q,R,W,E 순서 그대로이고,
        // UI에 그릴 때만 이 매핑을 거쳐 화면 위치를 바꾼다.
        private static readonly int[] SlotDisplayToDataIndex = { 0, 2, 3, 1 }; // 화면 위치(좌→우) → 실제 장착 슬롯 인덱스

        private void EnsureInstrumentSlotElements()
        {
            if (slotFrames[0] != null) return;

            GameObject root = new GameObject("InstrumentSlots");
            root.transform.SetParent(transform, false);
            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 28f);
            rootRt.sizeDelta = new Vector2(600f, 140f);

            // 화면 좌→우 표시 순서(Q,W,E,R) - 실제 장착 슬롯 인덱스는 SlotDisplayToDataIndex로 매핑.
            string[] keyLabels = new string[] { "Q", "W", "E", "R" };
            float slotSize = 124f; // 기존 56px 대비 약 2.2배 확대(사용자 요청 2~2.5배 범위)
            float gap = 26f;
            float totalWidth = slotSize * 4 + gap * 3;
            float startX = -totalWidth / 2f + slotSize / 2f;

            Sprite slotFrameSprite = LoadUISprite("InstrumentSlotFrame");

            for (int i = 0; i < 4; i++)
            {
                GameObject slotObj = new GameObject($"Slot_{keyLabels[i]}");
                slotObj.transform.SetParent(root.transform, false);
                RectTransform slotRt = slotObj.AddComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 0f);
                slotRt.anchorMax = new Vector2(0.5f, 0f);
                slotRt.pivot = new Vector2(0.5f, 0f);
                slotRt.anchoredPosition = new Vector2(startX + i * (slotSize + gap), 0f);
                slotRt.sizeDelta = new Vector2(slotSize, slotSize);

                Image frame = slotObj.AddComponent<Image>();
                if (slotFrameSprite != null)
                {
                    frame.sprite = slotFrameSprite;
                    // InstrumentSlotFrame.png는 1254x1254인데 실제 표시는 124x124라 border 비율
                    // 문제가 더 심함(사용자 실측: 프레임이 아예 안 보임) - Simple로 전환.
                    frame.type = Image.Type.Simple;
                }
                else
                {
                    frame.color = new Color(0.05f, 0.05f, 0.1f, 0.7f);
                }
                slotFrames[i] = frame;

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.enabled = false;
                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(18f, 18f);
                iconRt.offsetMax = new Vector2(-18f, -18f);
                slotIcons[i] = icon;

                GameObject keyObj = new GameObject("KeyLabel");
                keyObj.transform.SetParent(slotObj.transform, false);
                Text keyText = keyObj.AddComponent<Text>();
                keyText.font = GameFonts.Headline;
                keyText.fontSize = 28;
                keyText.color = new Color(1f, 1f, 0.4f);
                keyText.alignment = TextAnchor.UpperLeft;
                keyText.text = keyLabels[i];
                RectTransform keyRt = keyObj.GetComponent<RectTransform>();
                keyRt.anchorMin = new Vector2(0f, 1f);
                keyRt.anchorMax = new Vector2(0f, 1f);
                keyRt.pivot = new Vector2(0f, 1f);
                keyRt.anchoredPosition = new Vector2(4f, -2f);
                keyRt.sizeDelta = new Vector2(36f, 30f);
                slotKeyLabels[i] = keyText;

                GameObject lvlObj = new GameObject("LevelLabel");
                lvlObj.transform.SetParent(slotObj.transform, false);
                Text lvlText = lvlObj.AddComponent<Text>();
                lvlText.font = GameFonts.Body;
                lvlText.fontSize = 20;
                lvlText.color = Color.white;
                lvlText.alignment = TextAnchor.LowerRight;
                lvlText.text = "";
                RectTransform lvlRt = lvlObj.GetComponent<RectTransform>();
                lvlRt.anchorMin = new Vector2(1f, 0f);
                lvlRt.anchorMax = new Vector2(1f, 0f);
                lvlRt.pivot = new Vector2(1f, 0f);
                lvlRt.anchoredPosition = new Vector2(-4f, 2f);
                lvlRt.sizeDelta = new Vector2(50f, 24f);
                slotLevelLabels[i] = lvlText;

                // 2026-08-10: 사용자 피드백 - 잠긴 슬롯의 "필요 레벨" 정보가 구석에 작은 숫자로만
                // 떠서 어떤 슬롯 얘기인지 잘 안 읽힘. 프레임 안쪽 중앙에 "Lv.5\n해금" 형태로 크게
                // 표시하도록 별도 텍스트 추가(장착된 슬롯엔 안 쓰고 잠긴 슬롯에서만 보임).
                GameObject lockMsgObj = new GameObject("LockMessage");
                lockMsgObj.transform.SetParent(slotObj.transform, false);
                Text lockMsgText = lockMsgObj.AddComponent<Text>();
                lockMsgText.font = GameFonts.Body;
                lockMsgText.fontSize = 18;
                lockMsgText.color = new Color(0.85f, 0.85f, 0.85f);
                lockMsgText.alignment = TextAnchor.MiddleCenter;
                lockMsgText.text = "";
                lockMsgText.enabled = false;
                Outline lockMsgOutline = lockMsgObj.AddComponent<Outline>();
                lockMsgOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                lockMsgOutline.effectDistance = new Vector2(1f, -1f);
                RectTransform lockMsgRt = lockMsgObj.GetComponent<RectTransform>();
                lockMsgRt.anchorMin = Vector2.zero;
                lockMsgRt.anchorMax = Vector2.one;
                lockMsgRt.offsetMin = new Vector2(6f, 6f);
                lockMsgRt.offsetMax = new Vector2(-6f, -6f);
                slotLockMessages[i] = lockMsgText;
            }
        }

        private void EnsureBossUIElements()
        {
            // Create BossHpText if null
            if (bossHpText == null)
            {
                GameObject bObj = new GameObject("BossHpText");
                bObj.transform.SetParent(transform, false);
                bossHpText = bObj.AddComponent<Text>();
                bossHpText.font = GameFonts.Headline;
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
                victoryText.font = GameFonts.Headline;
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
                defeatText.font = GameFonts.Headline;
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
                label.font = GameFonts.Body;
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

            HandleEscapeInput();
        }

        // 2026-08-10: Time.timeScale이 0이어도 Update()는 계속 호출되므로(FixedUpdate/시간 기반
        // 로직만 멈춤) 일시정지 중에도 ESC로 다시 재개할 수 있음 - PlayerController가 이미 같은
        // 전제로 timeScale==0일 때 이동 입력을 무시하는 것과 동일한 패턴.
        private void HandleEscapeInput()
        {
            if (hasEnded) return; // 승리/패배 화면에서는 일시정지 메뉴를 열지 않음
            if (LevelUpUI.Instance != null && LevelUpUI.Instance.IsSelectionActive) return; // 레벨업 카드 선택 중엔 무시(둘 다 timeScale=0 풀스크린 모달이라 겹치면 안 됨)
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (confirmDialogPanel != null && confirmDialogPanel.activeSelf)
            {
                HideConfirmDialog();
            }
            else if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
            {
                ClosePauseSettings();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        public void UpdateHealthUI(int currentHp, int maxHp)
        {
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            }
            if (hpValueText != null)
            {
                hpValueText.text = $"{currentHp} / {maxHp}";
            }
        }

        public void UpdateExpUI(int level, int currentExp, int maxExp)
        {
            if (expBarFill != null)
            {
                expBarFill.fillAmount = maxExp > 0 ? Mathf.Clamp01((float)currentExp / maxExp) : 0f;
            }
            if (expValueText != null)
            {
                expValueText.text = $"{currentExp} / {maxExp}";
            }
            if (expLevelText != null)
            {
                expLevelText.text = $"Lv.{level}";
            }
        }

        public void UpdateInstrumentUI(List<InstrumentInfo> instruments)
        {
            if (slotFrames[0] == null) return;

            int unlocked = InstrumentManager.Instance != null ? InstrumentManager.Instance.GetUnlockedSlotsCount() : 2;
            // 2026-08-10: Unity MCP 실측에서 발견된 버그 수정 - GetUnlockedSlotsCount()의 실제 해금
            // 기준은 Lv1~4→1슬롯(Q), Lv5~9→2슬롯(+R), Lv10~14→3슬롯(+W), Lv15+→4슬롯(+E)이라
            // R/W/E의 실제 필요 레벨은 5/10/15인데 기존 배열이 인덱스 한 칸 밀려있고 값도
            // ("", "Lv.5", "Lv.8")로 틀려 있었음(R엔 라벨이 아예 안 뜨고 W/E엔 잘못된 값이 떴었음).
            string[] lockReqs = new string[] { "", "Lv.5", "Lv.10", "Lv.15" };

            // uiIndex = 화면 좌→우 표시 위치(Q,W,E,R), dataIndex = 실제 장착 슬롯 인덱스(Q=0,R=1,W=2,E=3)
            for (int uiIndex = 0; uiIndex < 4; uiIndex++)
            {
                int dataIndex = SlotDisplayToDataIndex[uiIndex];
                bool isLocked = dataIndex >= unlocked;
                bool isFilled = dataIndex < instruments.Count;

                // 2026-08-10: Unity MCP 재검증에서 발견된 버그 수정 - 이 색은 원래 프레임 아트가 없을
                // 때(반투명 사각형 대체용)만 쓰려던 값인데, 스프라이트 유무와 무관하게 항상 곱해지고
                // 있었음. 이제 InstrumentSlotFrame.png 실제 아트가 로드된 상태에서도 같은 거의-검은색이
                // 곱해져 아트의 색/디테일이 거의 다 뭉개져 보였음(hasBorder=true인데도 눈으로는 밋밋한
                // 검은 사각형처럼 보이던 원인). 실제 아트가 있으면 RGB는 흰색으로 유지하고 잠금 상태는
                // 알파로만 표현 - 아트가 없을 때만 기존 반투명 사각형 대체 색을 그대로 사용.
                if (slotFrames[uiIndex].sprite != null)
                {
                    // 2026-08-10: 잠긴 슬롯 안에 이제 "Lv.N/해금" 텍스트가 들어가므로, 프레임이 너무
                    // 어두우면(기존 0.4) 텍스트를 담는 그릇 자체가 잘 안 보여 0.55로 소폭 상향.
                    slotFrames[uiIndex].color = isLocked ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
                }
                else
                {
                    slotFrames[uiIndex].color = isLocked
                        ? new Color(0.08f, 0.08f, 0.08f, 0.55f)  // 잠김 - 어둡게 죽임
                        : new Color(0.05f, 0.05f, 0.1f, 0.85f);  // 해금 - 정상 표시
                }

                if (isFilled)
                {
                    InstrumentInfo info = instruments[dataIndex];
                    Sprite iconSprite = Resources.Load<Sprite>($"Sprites/Instruments/{info.type}");
                    slotIcons[uiIndex].enabled = iconSprite != null;
                    slotIcons[uiIndex].sprite = iconSprite;
                    // 2026-08-10: 사용자 요청 - 악기 아이콘은 원본 색 그대로 사용(틴트 제거).
                    slotIcons[uiIndex].color = Color.white;
                    slotLevelLabels[uiIndex].text = $"Lv.{info.level}";
                    slotLockMessages[uiIndex].enabled = false;
                }
                else
                {
                    slotIcons[uiIndex].enabled = false;
                    slotLevelLabels[uiIndex].text = "";
                    // 2026-08-10: 잠긴 슬롯은 구석의 작은 숫자 대신 프레임 중앙에 큰 "Lv.N / 해금"
                    // 메시지로 표시 - 어느 슬롯이 언제 열리는지 한눈에 안 들어온다는 피드백 반영.
                    if (isLocked)
                    {
                        slotLockMessages[uiIndex].text = $"{lockReqs[dataIndex]}\n해금";
                        slotLockMessages[uiIndex].enabled = true;
                    }
                    else
                    {
                        slotLockMessages[uiIndex].enabled = false;
                    }
                }

                slotKeyLabels[uiIndex].color = isLocked ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : new Color(1f, 1f, 0.4f);
            }
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
            // 2026-08-10: 사용자 실측에서 사망 후에도 음악이 계속 재생된다는 버그가 보고됨(승리
            // 화면도 동일 문제 있을 것으로 판단해 함께 처리). 레벨업 팝업 때 이미 쓰던
            // AudioLayerManager.PauseAllAudio()를 재사용 - 완전 정지(Stop)가 아니라 Pause인 이유는
            // "메인으로" 버튼을 누르면 SceneManager.LoadScene으로 씬 자체가 넘어가면서 오디오
            // 소스도 함께 정리되므로 굳이 상태를 더 복잡하게 만들 필요가 없기 때문.
            if (AudioLayerManager.Instance != null)
            {
                AudioLayerManager.Instance.PauseAllAudio();
            }
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
            // 2026-08-10: 사용자 실측 버그 수정 - 사망해도 음악이 계속 재생되고 있었음.
            if (AudioLayerManager.Instance != null)
            {
                AudioLayerManager.Instance.PauseAllAudio();
            }
        }

        private void OnReturnToMenuClicked()
        {
            Time.timeScale = 1f; // MainMenu(및 이후 재진입할 Gameplay)가 멈춰있지 않도록 반드시 복원
            SceneManager.LoadScene("MainMenu");
        }

        // ===================== 일시정지 메뉴 =====================

        private void EnsurePauseMenuElements()
        {
            if (pauseMenuRoot != null) return;

            // 최상위 다임 오버레이 - Awake()에서 마지막에 생성되므로(다른 Ensure*보다 나중 호출)
            // 자동으로 형제 순서상 맨 마지막이 되어 항상 다른 HUD 요소들보다 위에 그려진다.
            pauseMenuRoot = new GameObject("PauseMenuRoot");
            pauseMenuRoot.transform.SetParent(transform, false);
            RectTransform rootRt = pauseMenuRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            Image dim = pauseMenuRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            dim.raycastTarget = true; // 아래 게임 화면 클릭 차단

            EnsurePauseButtonsPanel(pauseMenuRoot.transform);
            EnsurePauseSettingsPanel(pauseMenuRoot.transform);
            EnsureConfirmDialogElements(pauseMenuRoot.transform);

            pauseMenuRoot.SetActive(false);
        }

        // MainMenuCanvas.prefab의 StartButton/SettingsButton/QuitButton과 동일한 스타일(커스텀
        // 스프라이트 없이 연회색 Image.color + Button ColorTint 조합)을 코드로 재현.
        private Button CreateMenuStyleButton(Transform parent, string label, float width, float height)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f);
            colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = GameFonts.Body;
            labelText.fontSize = 28;
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = label;
            RectTransform lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            return btn;
        }

        private void EnsurePauseButtonsPanel(Transform parent)
        {
            pauseButtonsPanel = new GameObject("PauseButtonsPanel");
            pauseButtonsPanel.transform.SetParent(parent, false);
            RectTransform rt = pauseButtonsPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 420f);
            rt.anchoredPosition = Vector2.zero;

            Image panelBg = pauseButtonsPanel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            VerticalLayoutGroup layout = pauseButtonsPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 18f;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pauseButtonsPanel.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.font = GameFonts.Headline;
            title.fontSize = 32;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            title.text = "일시정지";
            LayoutElement titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredWidth = 360f;
            titleLe.preferredHeight = 50f;

            CreateMenuStyleButton(pauseButtonsPanel.transform, "계속하기", 360f, 64f).onClick.AddListener(ResumeGame);
            CreateMenuStyleButton(pauseButtonsPanel.transform, "환경설정", 360f, 64f).onClick.AddListener(OpenPauseSettings);
            CreateMenuStyleButton(pauseButtonsPanel.transform, "메인으로", 360f, 64f).onClick.AddListener(
                () => ShowConfirmDialog("정말 메인으로 이동하시겠습니까?", ConfirmGoToMainMenu));
            CreateMenuStyleButton(pauseButtonsPanel.transform, "게임종료", 360f, 64f).onClick.AddListener(
                () => ShowConfirmDialog("정말 게임을 종료하시겠습니까?", ConfirmQuitGame));
        }

        // 사용자 결정: 메인 메뉴의 전체 설정 화면(볼륨 3종 + 키 리바인드 8행 + 싱크 보정)을 그대로
        // 재사용하지 않고, 게임 중엔 볼륨 3종 조절만 되는 축소판으로 구성(빠른 접근 위주).
        private void EnsurePauseSettingsPanel(Transform parent)
        {
            pauseSettingsPanel = new GameObject("PauseSettingsPanel");
            pauseSettingsPanel.transform.SetParent(parent, false);
            RectTransform rt = pauseSettingsPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(460f, 400f);
            rt.anchoredPosition = Vector2.zero;

            Image panelBg = pauseSettingsPanel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            VerticalLayoutGroup layout = pauseSettingsPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 20f;
            layout.padding = new RectOffset(36, 36, 30, 30);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pauseSettingsPanel.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.font = GameFonts.Headline;
            title.fontSize = 30;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            title.text = "환경설정";
            LayoutElement titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredWidth = 380f;
            titleLe.preferredHeight = 46f;

            CreateVolumeSliderRow(pauseSettingsPanel.transform, "배경음(BGM)", GameSettings.BgmVolume01,
                v => { GameSettings.BgmVolume01 = v; ApplyVolumeChange(); });
            CreateVolumeSliderRow(pauseSettingsPanel.transform, "효과음(SFX)", GameSettings.SfxVolume01,
                v => { GameSettings.SfxVolume01 = v; ApplyVolumeChange(); });
            CreateVolumeSliderRow(pauseSettingsPanel.transform, "악기 음량", GameSettings.InstrumentVolume01,
                v => { GameSettings.InstrumentVolume01 = v; ApplyVolumeChange(); });

            CreateMenuStyleButton(pauseSettingsPanel.transform, "뒤로가기", 300f, 60f).onClick.AddListener(ClosePauseSettings);

            pauseSettingsPanel.SetActive(false);
        }

        // 2026-08-10: bgmSource/악기 소스 볼륨은 "재생 시작 시점"에만 GameSettings를 한 번 읽는
        // 구조라(AudioLayerManager.PlayBossBattleBGM/ActivateInstrumentAudio), 이미 재생 중인
        // 소스에 슬라이더 조작이 즉시 반영되도록 AudioLayerManager.RefreshVolumesFromSettings()를
        // 매 슬라이더 변경마다 호출한다.
        private void ApplyVolumeChange()
        {
            if (AudioLayerManager.Instance != null)
            {
                AudioLayerManager.Instance.RefreshVolumesFromSettings();
            }
        }

        private void CreateVolumeSliderRow(Transform parent, string labelText, float initialValue01, UnityAction<float> onChanged)
        {
            GameObject rowObj = new GameObject($"Row_{labelText}");
            rowObj.transform.SetParent(parent, false);
            rowObj.AddComponent<RectTransform>();
            LayoutElement rowLe = rowObj.AddComponent<LayoutElement>();
            rowLe.preferredWidth = 380f;
            rowLe.preferredHeight = 58f;

            VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.UpperLeft;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            Text label = labelObj.AddComponent<Text>();
            label.font = GameFonts.Body;
            label.fontSize = 20;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.text = labelText;
            LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 380f;
            labelLe.preferredHeight = 26f;

            Slider slider = CreateSimpleSlider(rowObj.transform, 380f, 24f, initialValue01);
            slider.onValueChanged.AddListener(onChanged);
        }

        // Unity 기본 Slider 프리팹과 동일한 최소 계층(Background/Fill Area/Fill/Handle Slide
        // Area/Handle)을 코드로 구성. Slider는 fillRect의 anchorMax.x를 직접 갱신하는 방식이라
        // (HP/EXP 바의 Image.Type.Filled와 달리) Fill Image에 별도 스프라이트가 없어도 정상 동작함 -
        // sprite==null+Type.Filled일 때만 발생하는 그 렌더링 함정과는 무관한 구조.
        private Slider CreateSimpleSlider(Transform parent, float width, float height, float initialValue01)
        {
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(parent, false);
            sliderObj.AddComponent<RectTransform>();
            LayoutElement sliderLe = sliderObj.AddComponent<LayoutElement>();
            sliderLe.preferredWidth = width;
            sliderLe.preferredHeight = height;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.15f);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.2f);
            bgRt.anchorMax = new Vector2(1f, 0.8f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-5f, 0f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.45f, 0.85f, 0.25f, 1f); // EXP 바와 동일한 초록 계열
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f); // Slider가 값에 따라 anchorMax.x를 직접 갱신
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            GameObject handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRt = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white;
            RectTransform handleRt = handleObj.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, height + 10f);
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.transition = Selectable.Transition.ColorTint;
            slider.SetValueWithoutNotify(initialValue01);

            return slider;
        }

        // 메인으로/게임종료 공용 확인 다이얼로그.
        private void EnsureConfirmDialogElements(Transform parent)
        {
            confirmDialogPanel = new GameObject("ConfirmDialogPanel");
            confirmDialogPanel.transform.SetParent(parent, false);
            RectTransform rt = confirmDialogPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(440f, 220f);
            rt.anchoredPosition = Vector2.zero;

            Image panelBg = confirmDialogPanel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.97f);

            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(confirmDialogPanel.transform, false);
            confirmMessageText = msgObj.AddComponent<Text>();
            confirmMessageText.font = GameFonts.Body;
            confirmMessageText.fontSize = 24;
            confirmMessageText.color = Color.white;
            confirmMessageText.alignment = TextAnchor.MiddleCenter;
            confirmMessageText.text = "";
            RectTransform msgRt = msgObj.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0f, 0.42f);
            msgRt.anchorMax = new Vector2(1f, 1f);
            msgRt.offsetMin = new Vector2(20f, 0f);
            msgRt.offsetMax = new Vector2(-20f, -20f);

            GameObject buttonsRowObj = new GameObject("ButtonsRow");
            buttonsRowObj.transform.SetParent(confirmDialogPanel.transform, false);
            RectTransform buttonsRowRt = buttonsRowObj.AddComponent<RectTransform>();
            buttonsRowRt.anchorMin = new Vector2(0.5f, 0f);
            buttonsRowRt.anchorMax = new Vector2(0.5f, 0f);
            buttonsRowRt.pivot = new Vector2(0.5f, 0f);
            buttonsRowRt.anchoredPosition = new Vector2(0f, 28f);
            buttonsRowRt.sizeDelta = new Vector2(380f, 64f);

            HorizontalLayoutGroup rowLayout = buttonsRowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 24f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            CreateMenuStyleButton(buttonsRowObj.transform, "취소", 160f, 60f).onClick.AddListener(HideConfirmDialog);
            CreateMenuStyleButton(buttonsRowObj.transform, "확인", 160f, 60f).onClick.AddListener(OnConfirmDialogAccepted);

            confirmDialogPanel.SetActive(false);
        }

        private void OpenPauseMenu()
        {
            if (pauseMenuRoot == null) return;
            isPaused = true;
            pauseMenuRoot.SetActive(true);
            pauseButtonsPanel.SetActive(true);
            pauseSettingsPanel.SetActive(false);
            confirmDialogPanel.SetActive(false);
            Time.timeScale = 0f;
            if (AudioLayerManager.Instance != null) AudioLayerManager.Instance.PauseAllAudio();
        }

        private void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            pauseMenuRoot.SetActive(false);
            Time.timeScale = 1f;
            if (AudioLayerManager.Instance != null) AudioLayerManager.Instance.ResumeAllAudio();
        }

        private void OpenPauseSettings()
        {
            pauseButtonsPanel.SetActive(false);
            pauseSettingsPanel.SetActive(true);
        }

        private void ClosePauseSettings()
        {
            pauseSettingsPanel.SetActive(false);
            pauseButtonsPanel.SetActive(true);
            // SettingsPanelController.OnDisable()과 동일한 이유 - 슬라이더 드래그 중엔 매 프레임
            // PlayerPrefs I/O를 피하려 저장하지 않으므로, 패널을 닫는 "확정" 시점에 한 번 flush.
            GameSettings.Save();
        }

        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            pendingConfirmAction = onConfirm;
            confirmMessageText.text = message;
            pauseButtonsPanel.SetActive(false);
            pauseSettingsPanel.SetActive(false);
            confirmDialogPanel.SetActive(true);
        }

        private void HideConfirmDialog()
        {
            confirmDialogPanel.SetActive(false);
            pendingConfirmAction = null;
            // 다이얼로그를 취소하면 항상 4버튼 패널로 돌아간다(메인으로/게임종료 둘 다 4버튼
            // 패널에서만 열리므로, 다이얼로그 진입 경로가 설정 화면 쪽엔 없음).
            pauseButtonsPanel.SetActive(true);
        }

        private void OnConfirmDialogAccepted()
        {
            System.Action action = pendingConfirmAction;
            confirmDialogPanel.SetActive(false);
            pendingConfirmAction = null;
            action?.Invoke();
        }

        private void ConfirmGoToMainMenu()
        {
            Time.timeScale = 1f; // OnReturnToMenuClicked()와 동일 - 다음 씬이 멈춰있지 않도록 반드시 복원
            SceneManager.LoadScene("MainMenu");
        }

        private void ConfirmQuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
