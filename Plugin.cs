using BepInEx;
using Eremite;
using Eremite.Buildings;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Orders;
using Eremite.Services;
using Eremite.WorldMap;
using HarmonyLib;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StringProvider : Attribute { }

    public class StringHelper
    {
        private string prefix;
        public StringHelper(string prefix)
        {
            this.prefix = prefix;
        }

        public SimpleString New(string name, string value) => new($"{prefix}.{name}", value);
    }

    public interface IKeybindInjector
    {
        public void Inject();
    }


    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/bubblestorm");
        public Harmony harmony;
        public static Plugin Instance;

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

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            harmony = new Harmony("bubblestorm");
            harmony.PatchAll(typeof(Plugin));
        }

        public static Settings GameSettings => MainController.Instance.Settings;

        private static IEnumerable<T> Injectors<T>() where T : class
        {
            var injectorType = typeof(T);
            foreach (var injector in Assembly.GetAssembly(typeof(Plugin)).GetTypes().Where(t => injectorType.IsAssignableFrom(t) && !t.IsAbstract))
            {
                yield return Activator.CreateInstance(injector) as T;
            }
        }


        [HarmonyPatch(typeof(MainController), nameof(MainController.InitSettings))]
        [HarmonyPostfix]
        private static void InitSettings()
        {
            InjectKeybindings();
        }

        [HarmonyPatch(typeof(InputConfig), MethodType.Constructor)]
        [HarmonyPostfix]
        public static void InjectKeybindings()
        {
            foreach (var injector in Injectors<IKeybindInjector>())
            {
                injector.Inject();
            }
        }
    }
}