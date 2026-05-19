# Scene Foundation Batch 01

Purpose: MVP3.0 foundation scene assets for the main shop stage.

## Unity Requirements

- Unity Hub / Unity Editor 2D mobile game scene sprites.
- Cozy mobile idle shop style.
- Warm top-left lighting.
- Phone-screen readable.
- No text.
- No watermark.
- Must not bake UI, staff, customer, or order cards into the scene background.

## Integrated Assets

| Asset | Unity Path | Runtime Use | Status |
| --- | --- | --- | --- |
| scene_shop_interior_base.png | `Assets/Art/MVP30/Scene/Interior/` | Main shop stage background. | integrated |
| scene_cashier_counter.png | `Assets/Art/MVP30/Scene/Counter/` | Standalone cashier counter layer. | integrated |

## Integration Notes

- `ProjectBootstrapper.LoadMvp30PreparedArtSprites()` now loads both scene foundation assets before MVP2.3 fallbacks.
- `IdleShopGame` editor auto-configuration now also loads both scene foundation assets from MVP3.0.
- `scene_cashier_counter.png` was checked as a real alpha PNG for the official runtime filename.
- Scene foundation import max size is set to 2048 in `ProjectBootstrapper`.
