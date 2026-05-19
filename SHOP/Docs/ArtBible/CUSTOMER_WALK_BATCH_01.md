# Customer Walk Batch 01

Purpose: MVP3.0 four-frame customer walk cycle for entering and leaving the shop scene.

## Unity Requirements

- Unity Hub / Unity Editor 2D character sprites.
- Real alpha-transparent PNG.
- No checkerboard, black, white, gray, or colored preview background.
- Same approved `Customer_Female_A_Base.png` identity.
- Same clothing, colors, outline, lighting, and body proportion as the approved action set.
- Same canvas size across the batch.

## Integrated Assets

| Asset | Unity Path | Runtime Use | Status |
| --- | --- | --- | --- |
| customer_walk_01.png | `Assets/Art/MVP30/Characters/Customer/Walk/` | Walk cycle frame 1. | integrated |
| customer_walk_02.png | `Assets/Art/MVP30/Characters/Customer/Walk/` | Walk cycle frame 2. | integrated |
| customer_walk_03.png | `Assets/Art/MVP30/Characters/Customer/Walk/` | Walk cycle frame 3. | integrated |
| customer_walk_04.png | `Assets/Art/MVP30/Characters/Customer/Walk/` | Walk cycle frame 4. | integrated |

## Integration Notes

- `ProjectBootstrapper.LoadMvp30PreparedArtSprites()` now loads all four walk frames.
- `IdleShopGame` editor auto-configuration imports and loads all four walk frames.
- Customer entering now uses a four-frame walk cycle instead of reusing `customer_enter_01.png` as a fake walk frame.
- Customer state flow remains: enter -> idle/wait -> pay -> happy/leave.
