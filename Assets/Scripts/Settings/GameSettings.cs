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

        // RhythmManager.CheckHit()에서 currentTime = SongTime + RhythmSyncOffsetSeconds 로 사용됨.
        // 양수 = 입력 시각을 실제보다 늦은 것으로 간주(평소 "일찍" 누르는 성향 보정),
        // 음수 = 입력 시각을 실제보다 이른 것으로 간주(평소 "늦게" 누르는 성향 보정).
        // 판정 오차보다 훨씬 큰 값이 저장되는 사고를 막기 위해 ±300ms로 clamp.
        private const float MaxOffsetMs = 300f;

        public static float RhythmSyncOffsetMs
        {
            get => PlayerPrefs.GetFloat(SyncOffsetKey, 0f);
            set => PlayerPrefs.SetFloat(SyncOffsetKey, Mathf.Clamp(value, -MaxOffsetMs, MaxOffsetMs));
        }

        public static float RhythmSyncOffsetSeconds => RhythmSyncOffsetMs / 1000f;

        public static Key GetBinding(GameAction action)
        {
            string raw = PlayerPrefs.GetString(BindingKeyPrefix + action, string.Empty);
            if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, out Key parsed))
            {
                return parsed;
            }
            return DefaultBindings.TryGetValue(action, out Key defaultKey) ? defaultKey : Key.None;
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

        // 슬라이더 드래그 등 매 프레임 호출될 수 있는 setter에서는 I/O 비용 때문에 저장하지 않고,
        // 패널을 닫거나 계측을 적용하는 시점처럼 "확정" 지점에서 호출해 PlayerPrefs를 디스크에 flush한다.
        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
