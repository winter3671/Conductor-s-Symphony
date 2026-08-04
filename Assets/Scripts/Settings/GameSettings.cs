using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConductorSymphony.Settings
{
    public static class GameSettings
    {
        private const string BgmVolumeKey = "Settings.BgmVolume01";
        private const string SfxVolumeKey = "Settings.SfxVolume01";
        private const string InstrumentVolumeKey = "Settings.InstrumentVolume01";
        private const string SyncOffsetKey = "Settings.RhythmSyncOffsetMs";
        private const string BindingKeyPrefix = "Settings.Binding.";

        private static readonly Dictionary<GameAction, Key> DefaultBindings = new Dictionary<GameAction, Key>
        {
            { GameAction.HitLeft, Key.Q },
            { GameAction.HitUpLeft, Key.W },
            { GameAction.HitUpRight, Key.E },
            { GameAction.HitRight, Key.R },
            { GameAction.MoveUp, Key.UpArrow },
            { GameAction.MoveDown, Key.DownArrow },
            { GameAction.MoveLeft, Key.LeftArrow },
            { GameAction.MoveRight, Key.RightArrow },
        };

        public static float BgmVolume01
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume01
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static float InstrumentVolume01
        {
            get => PlayerPrefs.GetFloat(InstrumentVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(InstrumentVolumeKey, Mathf.Clamp01(value));
        }

        // Positive = 입력을 SongTime 기준보다 "늦게" 한 것으로 보정(늦게 누르는 성향 보정), 음수 = 반대.
        public static float RhythmSyncOffsetMs
        {
            get => PlayerPrefs.GetFloat(SyncOffsetKey, 0f);
            set => PlayerPrefs.SetFloat(SyncOffsetKey, value);
        }

        public static float RhythmSyncOffsetSeconds => RhythmSyncOffsetMs / 1000f;

        public static Key GetBinding(GameAction action)
        {
            string raw = PlayerPrefs.GetString(BindingKeyPrefix + action, string.Empty);
            if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, out Key parsed))
            {
                return parsed;
            }
            return DefaultBindings[action];
        }

        public static void SetBinding(GameAction action, Key key)
        {
            PlayerPrefs.SetString(BindingKeyPrefix + action, key.ToString());
        }

        public static bool IsKeyBoundToOtherAction(Key key, GameAction excluding)
        {
            foreach (GameAction action in DefaultBindings.Keys)
            {
                if (action != excluding && GetBinding(action) == key)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
