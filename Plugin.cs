using System;
using System.Reflection;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace Mod
{
    [BepInPlugin("radsi.random", "Random Build", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal new static ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;

            TryLoad();
        }

        private void TryLoad()
        {
            try
            {
                new Harmony("radsi.random").PatchAll();

                Log.LogInfo("Plugin has been loaded!");

                TryPatch();
            }
            catch (System.Exception e)
            {
                Log.LogError("Error loading the plugin!");
                Log.LogError(e);
            }
        }

        private void TryPatch()
        {
            try
            {
                Log.LogInfo($"Attempting to patch...");

                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

                Log.LogInfo($"Successfully patched!");
            }
            catch (System.Exception e)
            {
                Log.LogError($"Error registering patch!");
                Log.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(MyPlayer), nameof(MyPlayer.Spawn), MethodType.Normal)]
    class MyPlayerSpawnPatch
    {
        [HarmonyPostfix]
        static void Spawn_Postfix(MyPlayer __instance)
        {
            string build_path = Path.Join(Paths.PluginPath, "megabonk_build.json");

            if (!File.Exists(build_path))
            {
                Plugin.Log.LogInfo("No build");
                return;
            }

            string json = File.ReadAllText(build_path);

            BuildData build;

            try
            {
                build = JsonSerializer.Deserialize<BuildData>(json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex.Message);
                return;
            }

            __instance.inventory.weaponInventory = new Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponInventory();
            __instance.inventory.tomeInventory = new TomeInventory();

            foreach (string weapon in build.Weapons)
            {
                if (Enum.TryParse<EWeapon>(weapon, out var weaponEnum))
                {
                    __instance.inventory.weaponInventory.AddWeapon(DataManager.Instance.GetWeapon(weaponEnum), null);
                }
            }
            
            foreach (string tome in build.Tomes)
            {
                if (Enum.TryParse<ETome>(tome, out var tomeEnum))
                {
                    __instance.inventory.tomeInventory.AddTome(DataManager.Instance.tomeData[tomeEnum], new Il2CppSystem.Collections.Generic.List<StatModifier>(), ERarity.New);
                }
            }
        }
    }

    public class BuildData
    {
        [JsonPropertyName("character")]
        public string Character { get; set; }
        [JsonPropertyName("weapons")]
        public string[] Weapons { get; set; }
        [JsonPropertyName("tomes")]
        public string[] Tomes { get; set; }
    }
}