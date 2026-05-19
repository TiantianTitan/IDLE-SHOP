# MVP30 Unity Resource Table

This table defines the MVP 3.0 runtime sprite order. Unity setup now uses the prepared MVP3.0 loader, with MVP2.3 fallbacks for assets that have not been generated yet.

## Master Assets

| Key | Path | Status |
| --- | --- | --- |
| customer_base | `Assets/Art/MVP30/Characters/Customer/Base/Customer_Female_A_Base.png` | approved |
| staff_cashier_base | `Assets/Art/MVP30/Characters/Staff/Cashier/Base/Staff_Cashier_A_Base.png` | approved |
| shelf_master | `Assets/Art/MVP30/Scene/Shelf/Base/Shelf_Master.png` | approved |
| order_ticket_master | `Assets/Art/MVP30/UI/Orders/OrderTicket_Master.png` | approved |

## Runtime Sprite Order

This order is prepared for `ProjectBootstrapper`.

## Product Sprite Array

This array is passed separately into `IdleShopGame.ConfigureArt`.

| Index | Runtime Product | MVP30 path | Fallback |
| ---: | --- | --- | --- |
| 0 | rice_ball | `Assets/Art/MVP30/Products/product_onigiri_01.png` | integrated; fallback `Assets/Art/Products/rice_ball.png` |
| 1 | beverage | `Assets/Art/MVP30/Products/product_tea_01.png` | integrated; fallback `Assets/Art/Products/sparkling_water.png` |
| 2 | bread | TBD | `Assets/Art/Products/bread.png` |
| 3 | coffee | TBD | `Assets/Art/Products/coffee.png` |
| 4 | lunch_box | `Assets/Art/MVP30/Products/product_bento_01.png` | integrated; fallback `Assets/Art/Products/lunch_box.png` |
| 5 | dessert | `Assets/Art/MVP30/Products/product_dessert_01.png` | integrated; fallback `Assets/Art/Products/dessert.png` |
| 6 | flower | TBD | `Assets/Art/Products/flower.png` |
| 7 | gift_box | TBD | `Assets/Art/Products/gift_box.png` |

## MVP Art Sprite Order

| Index | Key | MVP30 path | Fallback |
| ---: | --- | --- | --- |
| 0 | scene_shop_interior | TBD | `Assets/Art/MVP23/scene_shop_interior_base.png` |
| 1 | cashier_counter | TBD | `Assets/Art/MVP23/scene_cashier_counter.png` |
| 2 | shelf_full | `Assets/Art/MVP30/Scene/Shelf/States/shelf_full_01.png` | integrated; fallback `Assets/Art/MVP23/scene_product_shelf_full.png` |
| 3 | shelf_low | `Assets/Art/MVP30/Scene/Shelf/States/shelf_low_01.png` | integrated; fallback `Assets/Art/MVP23/scene_product_shelf_low.png` |
| 4 | shelf_empty | `Assets/Art/MVP30/Scene/Shelf/States/shelf_empty_01.png` | integrated; fallback `Assets/Art/MVP23/scene_product_shelf_empty.png` |
| 5 | customer_enter_a | `Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png` | integrated; fallback `Assets/Art/MVP23/customer_enter_01.png` |
| 6 | customer_enter_b | `Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png` | integrated; fallback `Assets/Art/MVP23/npc_customer_walk_02.png` |
| 7 | customer_idle | `Assets/Art/MVP30/Characters/Customer/Idle/customer_idle_01.png` | integrated; fallback `Assets/Art/MVP23/customer_waiting_01.png` |
| 8 | customer_pay | `Assets/Art/MVP30/Characters/Customer/Pay/customer_pay_01.png` | integrated; fallback `Assets/Art/MVP23/npc_customer_pay.png` |
| 9 | customer_happy | `Assets/Art/MVP30/Characters/Customer/Happy/customer_happy_01.png` | integrated; fallback `Assets/Art/MVP23/customer_happy_01.png` |
| 10 | customer_disappointed | TBD | `Assets/Art/MVP23/npc_customer_disappointed.png` |
| 11 | staff_cashier_idle | `Assets/Art/MVP30/Characters/Staff/Cashier/Idle/staff_cashier_idle_01.png` | integrated; fallback `Assets/Art/MVP23/npc_staff_cashier_idle.png` |
| 12 | staff_cashier_work | `Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_work_01.png` | integrated; fallback `Assets/Art/MVP23/npc_staff_cashier_work.png` |
| 13 | fx_cash_float | `Assets/Art/MVP30/FX/fx_cash_float.png` | integrated; fallback `Assets/Art/MVP23/fx_cash_float.png` |
| 14 | fx_restock_success_ring | `Assets/Art/MVP30/FX/fx_restock_success_ring.png` | integrated; fallback `Assets/Art/MVP23/fx_restock_success_ring.png` |
| 15 | ui_order_ticket | `Assets/Art/MVP30/UI/Orders/ui_order_ticket.png` | integrated; fallback `Assets/Art/MVP23/ui_order_ticket.png` |
| 16 | ui_order_item_slot | `Assets/Art/MVP30/UI/Orders/ui_order_item_slot.png` | integrated; fallback `Assets/Art/MVP23/ui_order_item_slot.png` |
| 17 | ui_stock_warning_badge | `Assets/Art/MVP30/UI/Orders/ui_stock_warning_badge.png` | integrated; fallback `Assets/Art/MVP23/ui_stock_warning_badge.png` |
| 18 | ui_current_item_frame | `Assets/Art/MVP30/UI/Orders/ui_current_item_frame.png` | integrated; fallback `Assets/Art/MVP23/ui_current_item_frame.png` |
| 19 | icon_checkout_one | TBD | `Assets/Art/MVP23/icon_checkout_one.png` |
| 20 | icon_restock_item | TBD | `Assets/Art/MVP23/icon_restock_item.png` |
| 21 | icon_upgrade_product | TBD | `Assets/Art/MVP23/icon_upgrade_product.png` |
| 22 | scene_empty_shelf_overlay | TBD | `Assets/Art/MVP23/scene_empty_shelf_overlay.png` |
| 23 | ui_order_complete_stamp | `Assets/Art/MVP30/UI/Orders/ui_order_complete_stamp.png` | integrated; fallback `Assets/Art/MVP23/ui_order_complete_stamp.png` |
| 24 | fx_low_stock_pulse | TBD | `Assets/Art/MVP23/fx_low_stock_pulse.png` |
| 25 | fx_customer_waiting_bubble | TBD | `Assets/Art/MVP23/fx_customer_waiting_bubble.png` |
| 26 | ui_scene_floor_shadow | TBD | `Assets/Art/MVP23/ui_scene_floor_shadow.png` |
| 27 | fx_order_complete_glow | `Assets/Art/MVP30/FX/fx_order_complete_glow.png` | integrated; fallback `Assets/Art/MVP23/fx_order_complete_glow.png` |
| 28 | fx_item_selected_glow | `Assets/Art/MVP30/FX/fx_item_selected_glow.png` | integrated; fallback `Assets/Art/MVP23/fx_item_selected_glow.png` |
| 29 | fx_cash_float | `Assets/Art/MVP30/FX/fx_cash_float.png` | integrated; fallback `Assets/Art/MVP23/fx_cash_float.png` |
| 30 | fx_restock_success_ring | `Assets/Art/MVP30/FX/fx_restock_success_ring.png` | integrated; fallback `Assets/Art/MVP23/fx_restock_success_ring.png` |
| 31 | fx_upgrade_success | `Assets/Art/MVP30/FX/fx_upgrade_success.png` | integrated; fallback `Assets/Art/MVP23/fx_upgrade_success.png` |
| 32 | customer_leave | `Assets/Art/MVP30/Characters/Customer/Leave/customer_leave_01.png` | integrated; fallback `Assets/Art/MVP23/customer_leave_01.png` |
| 33 | staff_cashier_success | `Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_success_01.png` | integrated; fallback `Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_work_01.png` |
| 34 | shelf_restocked | `Assets/Art/MVP30/Scene/Shelf/States/shelf_restocked_01.png` | integrated; fallback `Assets/Art/MVP30/Scene/Shelf/States/shelf_full_01.png` |
