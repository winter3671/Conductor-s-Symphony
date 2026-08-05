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

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            ShowMainPanel();
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
