using BepInEx;
using BepInEx.Configuration;
using Eremite.Controller;
using Eremite.Model;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace BubbleStormTweaks
{
    public class SimpleString
    {
        public string Key;
        public string Value;

        public SimpleString(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator SimpleString(string value) => new(null, value);
        public static implicit operator LocaText(SimpleString value) => new() { key = value.Key };
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private static Harmony harmony;
        public static Settings GameSettings => MainController.Instance.Settings;
        private InputAction dumpAction;

        public static void LogInfo(object data)
        {
            if (data == null) Instance.Logger.LogInfo("<<NULL>>");
            else Instance.Logger.LogInfo(data);
        }

        public static void LogInfo(IEnumerable<object> seq)
        {
            foreach (var obj in seq)
                LogInfo(obj);
        }

        public static void LogError(object data) => Instance.Logger.LogError(data);

        private float update = 0.0f;

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            SetUpKeybind();
        }

        private void SetUpKeybind(){
            dumpAction = new("select_race_1", InputActionType.Button, expectedControlType: "Button");
            dumpAction.AddBinding("<Keyboard>/tab", groups: "Keyboard");
            Plugin.LogInfo("Added binding for dumper");

            dumpAction.performed += Dumper.DoDump;
            dumpAction.Enable();
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"Destroying {PluginInfo.PLUGIN_GUID} now");
            harmony?.UnpatchSelf();
            // dumpAction?.Disable();
            // dumpAction?.Dispose();
        }
    }
}