using BepInEx;
using BepInEx.Configuration;
using Eremite.Controller;
using Eremite.Model;
using System.Collections.Generic;
using UnityEngine;

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
        public static Settings GameSettings => MainController.Instance.Settings;

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

        private KeyboardShortcut dumpKeyBind;

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded  since {this.gameObject.activeSelf}");
            dumpKeyBind = new(KeyCode.F3);
        }

        private void Update(){
            if (dumpKeyBind.IsDown()) Dumper.DoDump();
        }

        private void OnDestroy()
        {
            /*
                Note that unless you change the BepInEx configuration, the Bepin manager GameObject is destroyed almost immediately,
                which takes this plugin (which is a Component attached to said manager) down with it, and thus calls OnDestroy.
                You don't want this to happen because it obviously breaks things like the Update() method.
                Fix this by making sure the BepInEx entrypoint is set to MainController constructor in Assembly-CSharp.dll
                This can be done by changing the values under [Preloader.Entrypoint] in BepInEx/config/BepInEx.cfg
            */
            Logger.LogInfo($"Destroying {PluginInfo.PLUGIN_GUID} now");
        }
    }
}