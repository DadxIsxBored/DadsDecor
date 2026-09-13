using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsDecor;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DadsDecorPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.dadisbored.dadsdecor";
    public const string PluginName = "DadsDecor";
    public const string PluginVersion = "1.1.0";

    internal static ConfigEntry<bool> ModEnabled = null!;
    internal static ConfigEntry<KeyboardShortcut> CancelShortcut = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        ModEnabled = Config.Bind(
            "1 - General",
            "Mod Enabled",
            true,
            "Enable DadsDecor.");
        CancelShortcut = Config.Bind(
            "2 - Controls",
            "Cancel Move Shortcut",
            new KeyboardShortcut(KeyCode.Escape),
            "Cancel the active move without changing the original piece.");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(DadsDecorPlugin).Assembly);
        MoveController.RegisterRoutedRpc();
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded for Valheim 1.0.12.");
    }

    private void Update()
    {
        MoveController.RegisterRoutedRpc();
        MoveController.Update();
    }

    private void OnDestroy()
    {
        MoveController.Reset();
        _harmony?.UnpatchSelf();
    }
}
