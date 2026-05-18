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
| Customer_Female_A_Base.png | `Assets/Art/MVP30/Characters/Customer/Base/` | planned | Primary customer identity. |
| Staff_Cashier_A_Base.png | `Assets/Art/MVP30/Characters/Staff/Cashier/Base/` | planned | Cashier identity. |
| Shelf_Master.png | `Assets/Art/MVP30/Scene/Shelf/Base/` | planned | Source for full/low/empty states. |
| OrderTicket_Master.png | `Assets/Art/MVP30/UI/Orders/` | planned | Source for order ticket states. |

## Runtime Assets

| Asset | Path | Status | Gameplay State |
| --- | --- | --- | --- |
| customer_enter_01.png | `Characters/Customer/Enter/` | planned | New customer enters. |
| customer_idle_01.png | `Characters/Customer/Idle/` | planned | Customer waiting. |
| customer_pay_01.png | `Characters/Customer/Pay/` | planned | Sale in progress. |
| customer_happy_01.png | `Characters/Customer/Happy/` | planned | Order complete. |
| customer_leave_01.png | `Characters/Customer/Leave/` | planned | Customer exits. |
| staff_cashier_idle_01.png | `Characters/Staff/Cashier/Idle/` | planned | Cashier idle. |
| staff_cashier_work_01.png | `Characters/Staff/Cashier/Work/` | planned | Checkout action. |
| shelf_full_01.png | `Scene/Shelf/States/` | planned | Stock healthy. |
| shelf_low_01.png | `Scene/Shelf/States/` | planned | Stock low. |
| shelf_empty_01.png | `Scene/Shelf/States/` | planned | Missing stock. |
| ui_order_complete_stamp.png | `UI/Orders/` | integrated | Completion stamp. |
| fx_order_complete_glow.png | `FX/` | integrated | Completion glow. |
| fx_item_selected_glow.png | `FX/` | integrated | Selected item glow. |
| fx_cash_float.png | `FX/` | integrated | Cash pop backing. |
| fx_restock_success_ring.png | `FX/` | integrated | Restock success ring. |
| fx_upgrade_success.png | `FX/` | integrated | Upgrade success burst. |

## Acceptance Checklist

- Same identity as master asset.
- Same outline thickness.
- Same lighting direction.
- Same canvas size within the set.
- Transparent background where required.
- Readable at phone UI size.
- Filename matches manifest.
- Unity import type is Sprite.
- Integrated fallback exists when asset is missing.
