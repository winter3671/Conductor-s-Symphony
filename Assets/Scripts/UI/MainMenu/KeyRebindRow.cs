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

        // 한 번에 하나의 행만 입력을 대기할 수 있도록 하는 전역 락.
        // 없으면 A행 "변경" 클릭 후 키를 누르기 전에 B행 "변경"을 누르면
        // 두 행 모두 waitingForKey=true가 되어 다음 키 입력이 어느 행에 적용될지 알 수 없었다(동시성 버그).
        private static KeyRebindRow activeRow;

        private bool waitingForKey;

        private void Awake()
        {
            rebindButton.onClick.AddListener(BeginRebind);
            if (warningLabel != null) warningLabel.text = string.Empty;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (activeRow == this)
            {
                CancelRebind();
            }
        }

        private void BeginRebind()
        {
            if (activeRow != null && activeRow != this)
            {
                activeRow.CancelRebind();
            }
            activeRow = this;
            waitingForKey = true;
            keyLabel.text = "키 입력...";
            if (warningLabel != null) warningLabel.text = string.Empty;
        }

        private void CancelRebind()
        {
            waitingForKey = false;
            if (activeRow == this) activeRow = null;
            RefreshLabel();
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
            if (activeRow == this) activeRow = null;

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
