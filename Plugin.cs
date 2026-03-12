using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace MortarStrikes
{
    [BepInPlugin("com.necoval.mortarstrikes", "Mortar Strikes", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static MortarConfig Cfg { get; private set; }

        public static ConfigEntry<KeyboardShortcut> DebugTriggerKey;

        private MortarStrikeManager _strikeManager;
        private bool _fikaRaidStartedSubscribed;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Cfg = MortarConfig.Load();
            DebugTriggerKey = Config.Bind("Debug", "Manual Trigger Key",
                new KeyboardShortcut(KeyCode.F9), "Press to trigger a mortar strike");
            Log.LogInfo("Mortar Strikes v1.0.0 loaded. Config: BepInEx/plugins/MortarStrikes/config.json");

            FikaSync.Init();
            _fikaRaidStartedSubscribed = FikaSync.TrySubscribeRaidStarted(OnFikaRaidStarted);
        }

        private void OnFikaRaidStarted()
        {
            CreateStrikeManager(raidAlreadyStarted: true);
        }

        private void Update()
        {
            if (_strikeManager == null && !_fikaRaidStartedSubscribed && IsInRaid())
            {
                CreateStrikeManager(raidAlreadyStarted: false);
            }
            if (_strikeManager != null && !IsInRaid())
            {
                GameObject.Destroy(_strikeManager.gameObject);
                _strikeManager = null;
            }
            if (DebugTriggerKey.Value.IsDown() && _strikeManager != null)
            {
                Log.LogWarning("[F9] Manual mortar strike");
                _strikeManager.TriggerStrikeInstant();
            }
        }

        private void CreateStrikeManager(bool raidAlreadyStarted)
        {
            if (_strikeManager != null) return;
            var go = new GameObject("MortarStrikeManager");
            _strikeManager = go.AddComponent<MortarStrikeManager>();
            _strikeManager.RaidAlreadyStarted = raidAlreadyStarted;
        }

        public static bool IsInRaid()
        {
            // When FIKA is present (including headless), use FikaGlobals.IsInRaid—MainPlayer may be null or set late on headless
            if (FikaSync.TryGetFikaIsInRaid(out bool fikaResult))
                return fikaResult;
            var gw = Singleton<GameWorld>.Instance;
            return gw != null && gw.MainPlayer != null;
        }

        public static bool IsHost()
        {
            return FikaSync.IsServer();
        }

        public static void ReloadConfig()
        {
            Cfg = MortarConfig.Load();
        }
    }
}
