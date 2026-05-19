# Action Icon Batch 01

Purpose: MVP3.0 action icons for checkout, restock, upgrade, and order entry/identity.

## Unity Requirements

- Unity Hub / Unity Editor 2D mobile game sprites.
- Real alpha-transparent PNG.
- No checkerboard, black, white, gray, or colored preview background.
- No fake transparency or baked canvas.
- No text.
- No watermark.
- Centered icon with safe padding.
- Consistent scale across the batch.
- Clear silhouette at small button size.

## Integrated Assets

| Asset | Unity Path | Runtime Use | Status |
| --- | --- | --- | --- |
| icon_checkout_one.png | `Assets/Art/MVP30/UI/Icons/` | Quick checkout and recommendation checkout action. | integrated |
| icon_restock_item.png | `Assets/Art/MVP30/UI/Icons/` | Restock action and recommendation restock action. | integrated |
| icon_upgrade_product.png | `Assets/Art/MVP30/UI/Icons/` | Upgrade action and recommendation upgrade action. | integrated |
| icon_order.png | `Assets/Art/MVP30/UI/Icons/` | Current order title identity icon. | integrated |

## Candidate Files

The `_A` and `_B` files in `Assets/Art/MVP30/UI/Icons/` are retained as source candidates. Runtime loading uses only the final no-suffix filenames listed above.

## Integration Notes

- `ProjectBootstrapper.LoadMvp30PreparedArtSprites()` now loads checkout, restock, and upgrade icons from `Assets/Art/MVP30/UI/Icons/` first.
- MVP2.3 icon paths remain as fallback for the three active action buttons.
- `icon_order.png` is loaded into the MVP30 sprite table and displayed in the current order title row.
- All four approved files were checked as `512x512 srgba opaque=false`.
