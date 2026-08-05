using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using ConductorSymphony.Settings;

namespace ConductorSymphony.UI
{
    public class KeyRebindRow : MonoBehaviour
    {
        [SerializeField] private GameAction action;
        [SerializeField] private Text keyLabel;
        [SerializeField] private Button rebindButton;
        [SerializeField] private Text warningLabel;

        private bool waitingForKey;

        private void Awake()
        {
            rebindButton.onClick.AddListener(BeginRebind);
            if (warningLabel != null) warningLabel.text = string.Empty;
            RefreshLabel();
        }

        private void BeginRebind()
        {
            waitingForKey = true;
            keyLabel.text = "키 입력...";
            if (warningLabel != null) warningLabel.text = string.Empty;
        }

        private void Update()
        {
            if (!waitingForKey) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control.wasPressedThisFrame)
                {
                    TryAssign(control.keyCode);
                    break;
                }
            }
        }

        private void TryAssign(Key key)
        {
            waitingForKey = false;

            if (key == Key.Escape)
            {
                RefreshLabel();
                return;
            }

            if (GameSettings.IsKeyBoundToOtherAction(key, action))
            {
                if (warningLabel != null) warningLabel.text = "이미 사용 중인 키입니다";
                RefreshLabel();
                return;
            }

            GameSettings.SetBinding(action, key);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            keyLabel.text = GameSettings.GetBinding(action).ToString();
        }
    }
}
