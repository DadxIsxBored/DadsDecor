using System;
using HarmonyLib;
using UnityEngine;

namespace DadsDecor;

[HarmonyPatch(typeof(Player), "RemovePiece")]
internal static class RemovePiecePatch
{
    private static bool Prefix(Player __instance, ref bool __result)
    {
        if (!MoveController.HandleRemoveAction(__instance)) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Player), "UpdatePlacement")]
internal static class UpdatePlacementPatch
{
    private static void Prefix(Player __instance)
    {
        MoveController.BeginPlacementUpdate(__instance);
    }

    private static Exception Finalizer(Exception __exception)
    {
        MoveController.EndPlacementUpdate();
        return __exception;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
internal static class PlacePiecePatch
{
    private static bool Prefix(Player __instance, Piece piece, Vector3 pos, Quaternion rot)
    {
        return !MoveController.TryCompleteMove(__instance, piece, pos, rot);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
internal static class HavePieceRequirementsPatch
{
    private static void Prefix(Player __instance, Piece piece, out bool __state)
    {
        __state = MoveController.BeginRequirementCheck(__instance, piece);
    }

    private static Exception Finalizer(Exception __exception, bool __state)
    {
        MoveController.EndRequirementCheck(__state);
        return __exception;
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), new[] { typeof(string), typeof(int), typeof(bool) })]
internal static class CountMoveMaterialsPatch
{
    private static void Postfix(Inventory __instance, ref int __result)
    {
        if (MoveController.ShouldSupplyMaterials(__instance)) __result = int.MaxValue;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources), new[] { typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int) })]
[HarmonyPriority(Priority.First)]
internal static class ConsumeMoveResourcesPatch
{
    private static bool Prefix(Player __instance)
    {
        return !MoveController.ShouldSkipResourceConsumption(__instance);
    }
}

[HarmonyPatch(typeof(Player), "OnDestroy")]
internal static class PlayerDestroyPatch
{
    private static void Prefix(Player __instance)
    {
        if (__instance == Player.m_localPlayer) MoveController.Reset();
    }
}
