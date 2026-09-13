using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsDecor;

internal static class MoveController
{
    private const string MoveRpc = "DadsDecor_MovePiece_V1";
    private static readonly MethodInfo GetRightItemMethod = AccessTools.Method(typeof(Humanoid), "GetRightItem");
    private static readonly MethodInfo CheckCanRemovePieceMethod = AccessTools.Method(typeof(Player), "CheckCanRemovePiece", new[] { typeof(Piece) });
    private static readonly MethodInfo ClearCachedSupportMethod = AccessTools.Method(typeof(WearNTear), "ClearCachedSupport");
    private static readonly FieldInfo PlaceRotationField = AccessTools.Field(typeof(Player), "m_placeRotation");
    private static readonly FieldInfo PlaceRotationDegreesField = AccessTools.Field(typeof(Player), "m_placeRotationDegrees");
    private static readonly FieldInfo RemoveRayMaskField = AccessTools.Field(typeof(Player), "m_removeRayMask");

    private static Player? _player;
    private static Piece? _sourcePiece;
    private static Piece? _selectedPrefab;
    private static ZRoutedRpc? _registeredRpc;
    private static int _requirementCheckDepth;
    private static bool _insidePlacementUpdate;
    private static bool _moveCompleted;

    internal static bool Active =>
        _player != null && _sourcePiece != null && _selectedPrefab != null;

    internal static void Update()
    {
        if (!Active)
        {
            if (_player != null || _sourcePiece != null || _selectedPrefab != null) ClearMove();
            return;
        }

        Player player = _player!;
        if (DadsDecorPlugin.ModEnabled.Value != true ||
            Player.m_localPlayer != player ||
            player.IsDead() ||
            !player.InPlaceMode() ||
            !IsHammerEquipped(player) ||
            player.GetSelectedPiece() != _selectedPrefab)
        {
            Cancel(true);
            return;
        }

        if (ShortcutDown(DadsDecorPlugin.CancelShortcut.Value)) Cancel(true);
    }

    internal static bool HandleHammerAction(Player player)
    {
        if (DadsDecorPlugin.ModEnabled.Value != true ||
            player != Player.m_localPlayer ||
            !player.InPlaceMode() ||
            !IsHammerEquipped(player) ||
            !ShortcutDown(DadsDecorPlugin.MoveShortcut.Value))
            return false;

        Piece? piece = FindAimedPiece(player);
        if (piece == null) return true;
        BeginMove(player, piece);
        return true;
    }

    internal static bool IsMovingSelection(Player player, Piece piece)
    {
        return Active && player == _player && piece == _selectedPrefab;
    }

    internal static void BeginPlacementUpdate(Player player)
    {
        _insidePlacementUpdate = Active && player == _player;
        _moveCompleted = false;
    }

    internal static void EndPlacementUpdate()
    {
        _insidePlacementUpdate = false;
        _moveCompleted = false;
    }

    internal static bool TryCompleteMove(Player player, Piece piece, Vector3 position, Quaternion rotation)
    {
        if (!_insidePlacementUpdate || !IsMovingSelection(player, piece)) return false;

        Piece source = _sourcePiece!;
        ZNetView? view = source.GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            Cancel(true);
            return false;
        }
        ZDO? zdo = view.GetZDO();
        if (zdo == null)
        {
            Cancel(true);
            return false;
        }

        string displayName = DisplayName(source);
        view.ClaimOwnership();
        zdo.SetPosition(position);
        zdo.SetRotation(rotation);
        ApplyTransform(source.gameObject, position, rotation);

        if (ZRoutedRpc.instance != null)
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, MoveRpc, zdo.m_uid, position, rotation);

        source.m_placeEffect.Create(position, rotation, source.transform, 1f, -1, player.GetZDOID());
        _moveCompleted = true;
        ClearMove();
        player.Message(MessageHud.MessageType.TopLeft, $"Moved {displayName}.");
        return true;
    }

    internal static bool ShouldSkipResourceConsumption(Player player)
    {
        return _insidePlacementUpdate && _moveCompleted && player == Player.m_localPlayer;
    }

    internal static bool BeginRequirementCheck(Player player, Piece piece)
    {
        bool applies = IsMovingSelection(player, piece);
        if (applies) _requirementCheckDepth++;
        return applies;
    }

    internal static void EndRequirementCheck(bool applied)
    {
        if (applied && _requirementCheckDepth > 0) _requirementCheckDepth--;
    }

    internal static bool ShouldSupplyMaterials(Inventory inventory)
    {
        return _requirementCheckDepth > 0 &&
               _player != null &&
               ReferenceEquals(inventory, _player.GetInventory());
    }

    internal static void Cancel(bool showMessage)
    {
        if (!Active)
        {
            ClearMove();
            return;
        }

        Player? player = _player;
        string displayName = DisplayName(_sourcePiece!);
        ClearMove();
        if (showMessage && player != null)
            player.Message(MessageHud.MessageType.TopLeft, $"Move cancelled: {displayName} remained in place.");
    }

    internal static void RegisterRoutedRpc()
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc == null || ReferenceEquals(rpc, _registeredRpc)) return;
        rpc.Register<ZDOID, Vector3, Quaternion>(MoveRpc, ReceiveMove);
        _registeredRpc = rpc;
    }

    private static void BeginMove(Player player, Piece piece)
    {
        if (!ValidateSource(player, piece)) return;

        if (Active) Cancel(false);
        CopyRotation(player, piece.transform.rotation);
        if (!player.SetSelectedPiece(piece))
        {
            player.Message(MessageHud.MessageType.Center, "That piece is not available in the equipped hammer.");
            return;
        }

        Piece selected = player.GetSelectedPiece();
        if (selected == null)
        {
            player.Message(MessageHud.MessageType.Center, "That piece is not available in the equipped hammer.");
            return;
        }

        _player = player;
        _sourcePiece = piece;
        _selectedPrefab = selected;
        player.Message(
            MessageHud.MessageType.TopLeft,
            $"Picked up {DisplayName(piece)}. Place with left click or press Escape to cancel.");
    }

    private static bool ValidateSource(Player player, Piece piece)
    {
        if (!piece.m_canBeRemoved || Location.IsInsideNoBuildLocation(piece.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
            return false;
        }
        if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true, false))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_privatezone");
            return false;
        }
        if (!(bool)CheckCanRemovePieceMethod.Invoke(player, new object[] { piece })) return false;
        if (!piece.CanBeRemoved())
        {
            player.Message(MessageHud.MessageType.Center, "$msg_cantremovenow");
            return false;
        }
        if (piece.GetComponentInChildren<TerrainModifier>() != null)
        {
            player.Message(MessageHud.MessageType.Center, "Terrain-modifying pieces cannot be moved.");
            return false;
        }

        ZNetView view = piece.GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            player.Message(MessageHud.MessageType.Center, "That piece has no valid network object.");
            return false;
        }
        return true;
    }

    private static Piece? FindAimedPiece(Player player)
    {
        GameCamera camera = GameCamera.instance;
        if (camera == null) return null;

        int removeRayMask = (int)RemoveRayMaskField.GetValue(player);
        if (!Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 50f, removeRayMask) ||
            Vector3.Distance(hit.point, player.GetEyePoint()) >= player.m_maxPlaceDistance)
            return null;

        Piece? piece = hit.collider.GetComponentInParent<Piece>();
        if (piece == null && hit.collider.GetComponent<Heightmap>() != null)
            piece = TerrainModifier.FindClosestModifierPieceInRange(hit.point, 2.5f);
        return piece;
    }

    private static void CopyRotation(Player player, Quaternion rotation)
    {
        float degrees = (float)PlaceRotationDegreesField.GetValue(player);
        if (Mathf.Approximately(degrees, 0f)) return;
        int step = (int)Math.Round(rotation.eulerAngles.y / degrees);
        PlaceRotationField.SetValue(player, step);
    }

    private static bool IsHammerEquipped(Player player)
    {
        ItemDrop.ItemData? item = GetRightItemMethod.Invoke(player, Array.Empty<object>()) as ItemDrop.ItemData;
        return item?.m_dropPrefab != null &&
               string.Equals(Utils.GetPrefabName(item.m_dropPrefab), "Hammer", StringComparison.Ordinal);
    }

    private static bool ShortcutDown(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey)) return false;
        foreach (KeyCode modifier in shortcut.Modifiers)
            if (!Input.GetKey(modifier)) return false;
        return true;
    }

    private static string DisplayName(Piece piece)
    {
        return Localization.instance != null ? Localization.instance.Localize(piece.m_name) : piece.m_name;
    }

    private static void ReceiveMove(long sender, ZDOID id, Vector3 position, Quaternion rotation)
    {
        if (ZNetScene.instance == null) return;
        GameObject instance = ZNetScene.instance.FindInstance(id);
        if (instance != null) ApplyTransform(instance, position, rotation);
    }

    private static void ApplyTransform(GameObject instance, Vector3 position, Quaternion rotation)
    {
        instance.transform.SetPositionAndRotation(position, rotation);
        WearNTear wear = instance.GetComponent<WearNTear>();
        if (wear != null && ClearCachedSupportMethod != null)
            ClearCachedSupportMethod.Invoke(wear, Array.Empty<object>());
    }

    private static void ClearMove()
    {
        _player = null;
        _sourcePiece = null;
        _selectedPrefab = null;
        _requirementCheckDepth = 0;
    }
}
