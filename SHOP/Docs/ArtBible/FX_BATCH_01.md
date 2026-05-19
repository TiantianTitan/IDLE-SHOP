# FX Batch 01

Purpose: MVP3.0 feedback effects for order completion, selected items, cash rewards, restock success, and upgrades.

## Source Master Context

- `Customer_Female_A_Base.png`
- `Staff_Cashier_A_Base.png`
- `Shelf_Master.png`
- `OrderTicket_Master.png`
- `Order UI Batch 01`

## Unity Requirements

- Unity Hub / Unity Editor 2D mobile game sprites.
- Real alpha-transparent PNG.
- No checkerboard, black, white, gray, or colored preview background.
- No text.
- No watermark.
- Centered effect with safe padding.
- Phone-screen readable.

## Integrated Assets

| Asset | Unity Path | Runtime Use | Status |
| --- | --- | --- | --- |
| fx_order_complete_glow.png | `Assets/Art/MVP30/FX/` | Warm glow behind the completed order stamp. | integrated |
| fx_item_selected_glow.png | `Assets/Art/MVP30/FX/` | Highlight for selected order/product item. | integrated |
| fx_cash_float.png | `Assets/Art/MVP30/FX/` | Backing effect behind Unity-rendered cash pop text. | integrated |
| fx_restock_success_ring.png | `Assets/Art/MVP30/FX/` | Restock success pulse/ring. | integrated |
| fx_upgrade_success.png | `Assets/Art/MVP30/FX/` | Upgrade success burst. | integrated |

## Candidate Files

The `_A` and `_B` files in `Assets/Art/MVP30/FX/` are retained as source candidates. Runtime loading uses only the final no-suffix filenames listed above.

## Integration Notes

- `ProjectBootstrapper.LoadMvp30PreparedArtSprites()` already loads these files from `Assets/Art/MVP30/FX/` first.
- MVP2.3 FX paths remain as fallback only.
- All five approved files were checked as `512x512 srgba opaque=false`.
