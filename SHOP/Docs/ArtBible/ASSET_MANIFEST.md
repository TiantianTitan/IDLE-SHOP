# Asset Manifest

This manifest tracks the intended MVP 3.0 art set.

Status values:

- `planned`
- `generated`
- `approved`
- `integrated`
- `needs_revision`

## Master Assets

| Asset | Path | Status | Notes |
| --- | --- | --- | --- |
| Customer_Female_A_Base.png | `Assets/Art/MVP30/Characters/Customer/Base/` | approved | Short bob hair, cream cardigan, coral top, teal skirt, brown bag and shoes. |
| Staff_Cashier_A_Base.png | `Assets/Art/MVP30/Characters/Staff/Cashier/Base/` | approved | Female cashier with cream shirt, warm orange apron, deep green lower outfit, brown shoes. |
| Shelf_Master.png | `Assets/Art/MVP30/Scene/Shelf/Base/` | approved | Warm wood shelf with green decorative trim, source for stock states. |
| OrderTicket_Master.png | `Assets/Art/MVP30/UI/Orders/` | approved | Green framed hanging order ticket with clear center safe area. |

## Runtime Assets

| Asset | Path | Status | Gameplay State |
| --- | --- | --- | --- |
| customer_enter_01.png | `Characters/Customer/Enter/` | integrated | New customer enters. |
| customer_idle_01.png | `Characters/Customer/Idle/` | integrated | Customer waiting. |
| customer_pay_01.png | `Characters/Customer/Pay/` | integrated | Sale in progress. |
| customer_happy_01.png | `Characters/Customer/Happy/` | integrated | Order complete. |
| customer_leave_01.png | `Characters/Customer/Leave/` | integrated | Customer exits. |
| staff_cashier_idle_01.png | `Characters/Staff/Cashier/Idle/` | integrated | Cashier idle. |
| staff_cashier_work_01.png | `Characters/Staff/Cashier/Work/` | integrated | Checkout action. |
| staff_cashier_success_01.png | `Characters/Staff/Cashier/Work/` | integrated | Order complete confirmation. |
| shelf_full_01.png | `Scene/Shelf/States/` | integrated | Stock healthy. |
| shelf_low_01.png | `Scene/Shelf/States/` | integrated | Stock low. |
| shelf_empty_01.png | `Scene/Shelf/States/` | integrated | Missing stock. |
| shelf_restocked_01.png | `Scene/Shelf/States/` | integrated | Short restock success state. |
| ui_order_ticket.png | `UI/Orders/` | integrated | Current order card background. |
| ui_order_item_slot.png | `UI/Orders/` | integrated | Order item row background. |
| ui_current_item_frame.png | `UI/Orders/` | integrated | Selected order item frame. |
| ui_stock_warning_badge.png | `UI/Orders/` | integrated | Missing stock warning badge. |
| ui_order_complete_stamp.png | `UI/Orders/` | integrated | Completion stamp. |
| fx_order_complete_glow.png | `FX/` | integrated | Completion glow. |
| fx_item_selected_glow.png | `FX/` | integrated | Selected item glow. |
| fx_cash_float.png | `FX/` | integrated | Cash pop backing. |
| fx_restock_success_ring.png | `FX/` | integrated | Restock success ring. |
| fx_upgrade_success.png | `FX/` | integrated | Upgrade success burst. |
| product_onigiri_01.png | `Products/` | integrated | Rice ball product sprite. |
| product_tea_01.png | `Products/` | integrated | Beverage product sprite. |
| product_bento_01.png | `Products/` | integrated | Bento/lunch box product sprite. |
| product_dessert_01.png | `Products/` | integrated | Dessert product sprite. |
| icon_checkout_one.png | `UI/Icons/` | integrated | Checkout one item action icon. |
| icon_restock_item.png | `UI/Icons/` | integrated | Restock current item action icon. |
| icon_upgrade_product.png | `UI/Icons/` | integrated | Product upgrade action icon. |
| icon_order.png | `UI/Icons/` | integrated | Current order/order entry icon. |

## Acceptance Checklist

- Same identity as master asset.
- Same outline thickness.
- Same lighting direction.
- Same canvas size within the set.
- Intended for Unity Hub / Unity Editor sprite import.
- Real PNG alpha channel where required.
- No gray/white checkerboard, pure black background, pure white background, white card, gray canvas, colored backdrop, or fake transparent background baked into the image.
- No white/gray pixels inside internal cutout spaces such as between legs, arm gaps, bag strap gaps, or hair gaps.
- Readable at phone UI size.
- Filename matches manifest.
- Unity import type is Sprite.
- Integrated fallback exists when asset is missing.
