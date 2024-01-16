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


    public interface ISettingInjector
    {
        public void Inject(Settings settings);
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

        public static bool doInjectSettings = false;

        public static void LogInfo(object data)
        {
            if (data == null)
            {
                Instance.Logger.LogInfo("<<NULL>>");
            }
            else
            {
                Instance.Logger.LogInfo(data);
            }
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
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");

            harmony = new Harmony("bubblestorm");

            harmony.PatchAll(typeof(Plugin));

        }

        public static Settings GameSettings => MainController.Instance.Settings;

        private static readonly List<SimpleString> allStrings = new();

        //[HarmonyPatch(typeof(Settings))]
        //[HarmonyPatch(nameof(Settings.GetBuilding))]
        //[HarmonyPrefix]
        //public static void Settings_GetBuilding(string modelName)
        //{
        //    Plugin.LogInfo("GetBuilding: " + modelName);

        //}

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
            var stringProviders = Assembly.GetAssembly(typeof(Plugin)).GetTypes().Where(t => t.GetCustomAttribute<StringProvider>() != null).ToArray();

            foreach (var stringProvider in stringProviders)
            {
                foreach (var field in stringProvider.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Where(f => f.FieldType == typeof(SimpleString)))
                {
                    var str = field.GetValue(null) as SimpleString;
                    str.Key = $"{stringProvider.FullName}.{field.Name}";
                    allStrings.Add(str);
                }
            }

            if (doInjectSettings)
            {
                foreach (var injector in Injectors<ISettingInjector>())
                {
                    injector.Inject(GameSettings);
                }

                InvalidateSettingsCaches();
            }
        }

        private static void InvalidateSettingsCaches()
        {
            //Invalidate all Settings caches since mods can add new models after the caches have been created for the first time
            var cacheType = typeof(ModelCache<>);
            foreach (var cacheField in typeof(Settings).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.FieldType.IsGenericType))
            {
                var genericType = cacheField.FieldType.GetGenericTypeDefinition();
                if (cacheType == genericType)
                {
                    object cache = cacheField.GetValue(GameSettings);
                    cache.GetType().GetField("cache", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cache, null);
                }
            }
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

        [HarmonyPatch(typeof(AppServices), nameof(AppServices.CreateServices))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void InitServices(AppServices __instance)
        {

            __instance.TextsService.OnTextsChanged.Subscribe(new Action(() =>
            {
                var s = MB.TextsService as TextsService;
                s.texts["MenuUI_KeyBindings_Action_select_race_1"] = "Pick Next Race";

                foreach (var str in allStrings)
                {
                    s.texts[str.Key] = str.Value;
                }
            }));
        }
    }
}