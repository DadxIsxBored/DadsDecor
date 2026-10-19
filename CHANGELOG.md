# Changelog

## 1.1.2

- Prevent container resource patches from consuming materials when placing a moved piece.

## 1.1.1

- Rebuild moved pieces' world-space support bounds after restoring their colliders so support checks use the new location.
- Refresh nearby support checks after placement.

## 1.1.0

- Replaced the pickup key combination with a middle-mouse hold.
- Retained normal middle-mouse dismantling for short clicks.
- Added a 0.35-second threshold that classifies pickup before Valheim's release-triggered dismantle action.

## 1.0.1

- Fixed middle-mouse interception to match Valheim's release-triggered dismantle action.
- Hid the source piece and disabled its local colliders while the placement ghost is active.
- Restored the source visuals and colliders when a move is cancelled.

## 1.0.0

- Added `Left Alt + middle mouse` pickup for placed hammer pieces.
- Added placement through Valheim's normal hammer ghost and validation path.
- Added material-free relocation of the original network object.
- Added configurable pickup and cancellation shortcuts.
- Added multiplayer transform synchronization.
- Added source removal permission checks and destination placement checks.
- Added cancellation without changing the original piece.
- Excluded terrain-modifying pieces.
