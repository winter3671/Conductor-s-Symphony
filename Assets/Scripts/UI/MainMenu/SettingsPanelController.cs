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
        [SerializeField] private Text syncOffsetLabel;
        [SerializeField] private Button syncCalibrationButton;
        [SerializeField] private Button syncResetButton;
        [SerializeField] private GameObject syncCalibrationPanel;

        private void Awake()
        {
            bgmVolumeSlider.onValueChanged.AddListener(v => GameSettings.BgmVolume01 = v);
            sfxVolumeSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume01 = v);
            instrumentVolumeSlider.onValueChanged.AddListener(v => GameSettings.InstrumentVolume01 = v);
            backButton.onClick.AddListener(() => mainMenuController.ShowMainPanel());
            syncCalibrationButton.onClick.AddListener(() => syncCalibrationPanel.SetActive(true));
            syncResetButton.onClick.AddListener(ResetSyncOffset);
        }

        // 계측이 잘못되거나(예: 이전 크리티컬 버그로 화면 지연을 오디오 지연으로 착각) 값을 되돌리고 싶을 때,
        // 재계측 없이 즉시 0ms(보정 없음)로 되돌리는 탈출구.
        private void ResetSyncOffset()
        {
            GameSettings.RhythmSyncOffsetMs = 0f;
            GameSettings.Save();
            RefreshSyncLabel();
        }

        private void OnEnable()
        {
            bgmVolumeSlider.SetValueWithoutNotify(GameSettings.BgmVolume01);
            sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume01);
            instrumentVolumeSlider.SetValueWithoutNotify(GameSettings.InstrumentVolume01);
            RefreshSyncLabel();
        }

        private void OnDisable()
        {
            // 볼륨/키 리바인딩은 슬라이더 드래그마다 PlayerPrefs에 즉시 반영되지만 디스크 flush는 안 하므로
            // (매 프레임 I/O 비용 방지), 패널을 닫는 이 시점에 한 번 명시적으로 flush한다.
            GameSettings.Save();
        }

        public void RefreshSyncLabel()
        {
            float ms = GameSettings.RhythmSyncOffsetMs;
            syncOffsetLabel.text = $"현재 오프셋: {ms:+0;-0;0}ms";
        }
    }
}
