# DadsDecor

DadsDecor moves existing hammer pieces without dismantling and rebuilding them.

Equip the hammer and aim at a placed piece. Click the middle mouse button normally to dismantle it, or hold the middle mouse button for 0.35 seconds to pick it up. The selected piece becomes the active hammer placement. Position and rotate it with Valheim's normal build controls, then place it with the left mouse button. Press `Escape` to cancel; the original remains unchanged until a valid placement completes.

## Behavior

- Uses Valheim's normal placement ghost, snapping, range, ward, biome, dungeon, overlap, and space checks.
- Distinguishes a normal middle-mouse dismantle click from a middle-mouse pickup hold.
- Moves the original network object so creator and component data remain attached to it.
- Consumes no building materials for a completed move.
- Retains normal hammer stamina and durability use on placement.
- Requires normal removal access at the source location and normal placement access at the destination.
- Rejects terrain-modifying pieces because moving their live terrain operation would leave terrain artifacts.
- Synchronizes the moved transform through a routed RPC. Install DadsDecor on each multiplayer client for immediate position updates.

## Configuration

- `Mod Enabled`: enables or disables DadsDecor.
- `Cancel Move Shortcut`: defaults to `Escape`.

Configuration path: `BepInEx/config/com.dadisbored.dadsdecor.cfg`.

## Installation

Install BepInExPack for Valheim, then place `DadsDecor.dll` in `BepInEx/plugins/DadsDecor/`.

## Compatibility

- Built against Valheim `1.0.12`.
- Requires BepInEx only.
- Install on the server and all participating clients for multiplayer use.
