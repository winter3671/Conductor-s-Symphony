using UnityEngine;
using UnityEngine.UI;
using ConductorSymphony.Settings;

namespace ConductorSymphony.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider instrumentVolumeSlider;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            bgmVolumeSlider.onValueChanged.AddListener(v => GameSettings.BgmVolume01 = v);
            sfxVolumeSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume01 = v);
            instrumentVolumeSlider.onValueChanged.AddListener(v => GameSettings.InstrumentVolume01 = v);
            backButton.onClick.AddListener(() => mainMenuController.ShowMainPanel());
        }

        private void OnEnable()
        {
            bgmVolumeSlider.SetValueWithoutNotify(GameSettings.BgmVolume01);
            sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume01);
            instrumentVolumeSlider.SetValueWithoutNotify(GameSettings.InstrumentVolume01);
        }
    }
}
