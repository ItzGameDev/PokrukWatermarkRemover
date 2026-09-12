using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace WatermarkRemover
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("org.pokruk.gorillatag.cameramod", BepInDependency.DependencyFlags.SoftDependency)]
    public class WatermarkRemover : BaseUnityPlugin
    {
        private const string PluginGuid = "com.watermarkremover.gorillatag";
        private const string PluginName = "WatermarkRemover";
        private const string PluginVersion = "1.0.0";

        private Harmony _harmony;
        private static ManualLogSource _logger;
        private static WatermarkRemover _instance;

        private void Awake()
        {
            _instance = this;
            _logger = Logger;
            _logger.LogInfo($"{PluginName} v{PluginVersion} loading...");

            _harmony = new Harmony(PluginGuid);

            PatchCameraModWatermark();

            _logger.LogInfo($"{PluginName} loaded successfully.");
        }

        private void PatchCameraModWatermark()
        {
            try
            {
                Assembly cameraModAssembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "CameraMod" || asm.GetName().Name == "CameraMod.dll")
                    {
                        cameraModAssembly = asm;
                        break;
                    }
                }

                if (cameraModAssembly == null)
                {
                    _logger.LogWarning("CameraMod assembly not found yet. Waiting for it to load...");
                    StartCoroutine(WaitAndPatch());
                    return;
                }

                ApplyPatches(cameraModAssembly);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during initial patch attempt: {ex}");
            }
        }

        private IEnumerator WaitAndPatch()
        {
            Assembly cameraModAssembly = null;
            float elapsed = 0f;
            float timeout = 30f;

            while (cameraModAssembly == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "CameraMod" || asm.GetName().Name == "CameraMod.dll")
                    {
                        cameraModAssembly = asm;
                        break;
                    }
                }
            }

            if (cameraModAssembly != null)
            {
                _logger.LogInfo("CameraMod assembly found. Applying patches...");
                ApplyPatches(cameraModAssembly);
            }
            else
            {
                _logger.LogWarning("CameraMod assembly not found after waiting. Watermark removal disabled.");
            }
        }

        private void ApplyPatches(Assembly cameraModAssembly)
        {
            int patchCount = 0;

            patchCount += PatchWaterMarkMethod(cameraModAssembly);
            patchCount += PatchDrawMainWindow(cameraModAssembly);
            patchCount += PatchDrawWindow(cameraModAssembly);
            patchCount += PatchDrawSpectatorWindow(cameraModAssembly);
            patchCount += DisableWatermarkField(cameraModAssembly);
            patchCount += PatchOnGUI(cameraModAssembly);

            _logger.LogInfo($"Applied {patchCount} patches to remove watermark.");
        }

        private int PatchWaterMarkMethod(Assembly asm)
        {
            int count = 0;
            foreach (var type in asm.GetTypes())
            {
                var method = type.GetMethod("WaterMark",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                if (method != null)
                {
                    var prefix = new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                        nameof(WatermarkRemoverPatchers.SkipMethod), BindingFlags.Public | BindingFlags.Static));
                    _harmony.Patch(method, prefix);
                    _logger.LogInfo($"Patched WaterMark method in {type.FullName}");
                    count++;
                }
            }
            return count;
        }

        private int PatchDrawMainWindow(Assembly asm)
        {
            int count = 0;
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;

                var method = type.GetMethod("DrawMainWindow",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                if (method != null)
                {
                    var postfix = new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                        nameof(WatermarkRemoverPatchers.StripWatermarkFromWindow), BindingFlags.Public | BindingFlags.Static));

                    if (postfix != null)
                    {
                        _harmony.Patch(method, null, postfix);
                        _logger.LogInfo($"Patched DrawMainWindow in {type.FullName}");
                        count++;
                    }
                }
            }
            return count;
        }

        private int PatchDrawWindow(Assembly asm)
        {
            int count = 0;
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;

                var method = type.GetMethod("DrawWindow",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                if (method != null)
                {
                    var postfix = new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                        nameof(WatermarkRemoverPatchers.StripWatermarkFromWindow), BindingFlags.Public | BindingFlags.Static));

                    if (postfix != null)
                    {
                        _harmony.Patch(method, null, postfix);
                        _logger.LogInfo($"Patched DrawWindow in {type.FullName}");
                        count++;
                    }
                }
            }
            return count;
        }

        private int PatchDrawSpectatorWindow(Assembly asm)
        {
            int count = 0;
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;

                var method = type.GetMethod("DrawSpectatorWindow",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                if (method != null)
                {
                    var postfix = new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                        nameof(WatermarkRemoverPatchers.StripWatermarkFromWindow), BindingFlags.Public | BindingFlags.Static));

                    if (postfix != null)
                    {
                        _harmony.Patch(method, null, postfix);
                        _logger.LogInfo($"Patched DrawSpectatorWindow in {type.FullName}");
                        count++;
                    }
                }
            }
            return count;
        }

        private int DisableWatermarkField(Assembly asm)
        {
            int count = 0;
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    var field = type.GetField("watermarkEnabled",
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        if (field.IsStatic)
                        {
                            field.SetValue(null, false);
                            _logger.LogInfo($"Set static watermarkEnabled = false in {type.FullName}");
                        }
                        else
                        {
                            _harmony.Patch(
                                type.GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                                type.GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                                postfix: new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                                    nameof(WatermarkRemoverPatchers.DisableWatermarkFieldPost), BindingFlags.Public | BindingFlags.Static)));
                            _logger.LogInfo($"Patched instance watermarkEnabled in {type.FullName}");
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error disabling watermarkEnabled: {ex}");
            }
            return count;
        }

        private int PatchOnGUI(Assembly asm)
        {
            int count = 0;
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;

                var method = type.GetMethod("OnGUI",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    var postfix = new HarmonyMethod(typeof(WatermarkRemoverPatchers).GetMethod(
                        nameof(WatermarkRemoverPatchers.SkipWatermarkInOnGUI), BindingFlags.Public | BindingFlags.Static));

                    if (postfix != null)
                    {
                        _harmony.Patch(method, null, postfix);
                        count++;
                    }
                }
            }
            return count;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _logger.LogInfo($"{PluginName} unloaded.");
        }
    }

    public static class WatermarkRemoverPatchers
    {
        public static bool SkipMethod()
        {
            return false;
        }

        public static void StripWatermarkFromWindow(object __instance)
        {
            try
            {
                var type = __instance.GetType();

                var watermarkField = type.GetField("watermarkEnabled",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (watermarkField != null)
                {
                    if (watermarkField.IsStatic)
                        watermarkField.SetValue(null, false);
                    else
                        watermarkField.SetValue(__instance, false);
                }

                var adminField = type.GetField("adminLabel",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (adminField != null)
                {
                    if (adminField.FieldType == typeof(string))
                    {
                        if (adminField.IsStatic)
                            adminField.SetValue(null, string.Empty);
                        else
                            adminField.SetValue(__instance, string.Empty);
                    }
                }
            }
            catch { }
        }

        public static void DisableWatermarkFieldPost(object __instance)
        {
            try
            {
                var type = __instance.GetType();
                var field = type.GetField("watermarkEnabled",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(__instance, false);
                }
            }
            catch { }
        }

        public static void SkipWatermarkInOnGUI(object __instance)
        {
            try
            {
                var type = __instance.GetType();
                var asmName = type.Assembly.GetName().Name;
                if (asmName != "CameraMod") return;

                var watermarkField = type.GetField("watermarkEnabled",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (watermarkField != null)
                {
                    if (watermarkField.IsStatic)
                        watermarkField.SetValue(null, false);
                    else
                        watermarkField.SetValue(__instance, false);
                }

                var showNotifField = type.GetField("showNotification",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (showNotifField != null && showNotifField.FieldType == typeof(bool))
                {
                    if (showNotifField.IsStatic)
                        showNotifField.SetValue(null, false);
                    else
                        showNotifField.SetValue(__instance, false);
                }
            }
            catch { }
        }
    }
}
