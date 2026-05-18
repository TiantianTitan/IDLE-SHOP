# Naming and Folders

## Root

Unity runtime art lives under:

`SHOP/IdleShopUnity/Assets/Art/MVP30/`

## Folder Layout

```text
MVP30/
  Characters/
    Customer/
      Base/
      Enter/
      Idle/
      Pay/
      Happy/
      Leave/
    Staff/
      Cashier/
        Base/
        Idle/
        Work/
  Scene/
    Shelf/
      Base/
      States/
  UI/
    Orders/
  FX/
```

## Master Asset Names

- `Customer_Female_A_Base.png`
- `Staff_Cashier_A_Base.png`
- `Shelf_Master.png`
- `OrderTicket_Master.png`

Master assets are stable references. Do not overwrite them after approval.

## Runtime Names

Use lowercase runtime names for Unity-loaded variants:

- `customer_enter_01.png`
- `customer_idle_01.png`
- `customer_pay_01.png`
- `customer_happy_01.png`
- `customer_leave_01.png`
- `staff_cashier_idle_01.png`
- `staff_cashier_work_01.png`
- `shelf_full_01.png`
- `shelf_low_01.png`
- `shelf_empty_01.png`
- `ui_order_complete_stamp.png`
- `fx_order_complete_glow.png`
- `fx_item_selected_glow.png`
- `fx_cash_float.png`
- `fx_restock_success_ring.png`
- `fx_upgrade_success.png`

## Versioning

Use suffixes for iterations:

- `_v01`
- `_v02`
- `_approved`

Example:

`Customer_Female_A_Base_v03_approved.png`

Only approved files should be copied or renamed to runtime names.
