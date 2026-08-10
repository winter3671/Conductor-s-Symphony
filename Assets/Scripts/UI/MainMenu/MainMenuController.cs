using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConductorSymphony.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private const string BackgroundObjectName = "MenuBackground";

        private void Awake()
        {
            EnsureBackgroundElement();
            startButton.onClick.AddListener(OnStartClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            ShowMainPanel();
        }

        private void EnsureBackgroundElement()
        {
            Transform existing = transform.Find(BackgroundObjectName);
            if (existing == null)
            {
                Sprite backgroundSprite = Resources.Load<Sprite>("Sprites/Background/MainMenuBackground");

                GameObject bgObject = new GameObject(BackgroundObjectName, typeof(RectTransform), typeof(Image));
                RectTransform bgRect = bgObject.GetComponent<RectTransform>();
                bgRect.SetParent(transform, false);
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                bgRect.SetAsFirstSibling();

                Image bgImage = bgObject.GetComponent<Image>();
                bgImage.raycastTarget = false;
                if (backgroundSprite != null)
                {
                    bgImage.sprite = backgroundSprite;
                    bgImage.type = Image.Type.Simple;
                    bgImage.preserveAspect = false;
                    bgImage.color = Color.white;
                }
                else
                {
                    bgImage.color = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);
                }
            }

            // MainPanel/SettingsPanel은 프리팹에 원래부터 있던 전체화면 불투명 Image(짙은 남색,
            // alpha=1)를 자체 배경으로 갖고 있어 - MenuBackground를 맨 뒤(SetAsFirstSibling)에
            // 깔아도 이 두 패널이 그 위를 완전히 뒤덮어 화면엔 늘 이 단색만 보이고 새 배경 이미지는
            // 절대 보이지 않았다(Unity MCP 실측에서 발견). RGB는 그대로 두고 alpha만 0으로 낮춰
            // 레이아웃 그룹 컨테이너 역할은 유지하면서 배경이 그대로 투과되도록 함.
            MakePanelBackgroundTransparent(mainPanel);
            MakePanelBackgroundTransparent(settingsPanel);
        }

        private static void MakePanelBackgroundTransparent(GameObject panel)
        {
            if (panel == null) return;
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null) return;

            Color c = panelImage.color;
            panelImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene("Gameplay");
        }

        private void OnSettingsClicked()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
