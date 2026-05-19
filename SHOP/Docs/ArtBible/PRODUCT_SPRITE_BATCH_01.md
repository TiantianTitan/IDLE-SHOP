# Product Sprite Batch 01

Purpose: MVP3.0 readable product sprites for order rows, shelf focus points, inventory UI, and restock/sale feedback.

## Unity Requirements

- Unity Hub / Unity Editor 2D mobile game sprites.
- Real alpha-transparent PNG.
- No checkerboard, black, white, gray, or colored preview background.
- No fake transparency or baked canvas.
- No text.
- No watermark.
- Centered product with safe padding.
- Consistent scale across the batch.
- Phone-screen readable silhouette.

## Integrated Assets

| Asset | Unity Path | Runtime Slot | Status |
| --- | --- | --- | --- |
| product_onigiri_01.png | `Assets/Art/MVP30/Products/` | `rice_ball` product sprite | integrated |
| product_tea_01.png | `Assets/Art/MVP30/Products/` | beverage product sprite | integrated |
| product_bento_01.png | `Assets/Art/MVP30/Products/` | `lunch_box` product sprite | integrated |
| product_dessert_01.png | `Assets/Art/MVP30/Products/` | `dessert` product sprite | integrated |

## Candidate Files

The `_A` and `_B` files in `Assets/Art/MVP30/Products/` are retained as source candidates. Runtime loading uses only the final no-suffix filenames listed above.

## Integration Notes

- `ProjectBootstrapper` maps this batch into the existing `productSprites` array.
- MVP2.3 product sprites remain as fallback.
- The current runtime still keeps old product art for bread, coffee, flower, and gift box until later batches replace them.
- All four approved files were checked as `512x512 srgba opaque=false`.
