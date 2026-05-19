# Order UI Batch 01

Reference master:

`Assets/Art/MVP30/UI/Orders/OrderTicket_Master.png`

Do not generate a new UI style. Every output must preserve the approved order UI direction.

## Fixed Identity

- Green framed hanging order ticket style.
- Warm paper and wood tones.
- Clean readable center areas for Unity-rendered text.
- Cozy mobile idle shop game UI style.
- Warm top-left lighting.
- This is a Unity game sprite for Unity Hub / Unity Editor import, not a presentation image.
- Real alpha-transparent background.
- No background pixels at all: no gray/white checkerboard pattern, pure black background, pure white background, white box, gray canvas, colored backdrop, or fake transparent background.
- No text or watermark.

## Outputs

| File | Target Folder | Use | Status |
| --- | --- | --- | --- |
| ui_order_ticket.png | `Assets/Art/MVP30/UI/Orders/` | Current order card background. | integrated |
| ui_order_item_slot.png | `Assets/Art/MVP30/UI/Orders/` | Order item row background. | integrated |
| ui_current_item_frame.png | `Assets/Art/MVP30/UI/Orders/` | Selected order item frame. | integrated |
| ui_order_complete_stamp.png | `Assets/Art/MVP30/UI/Orders/` | Completion stamp behind Unity text. | integrated |
| ui_stock_warning_badge.png | `Assets/Art/MVP30/UI/Orders/` | Missing stock warning badge. | integrated |

## Acceptance Checklist

- Ticket and item slot have clean safe areas for Unity-rendered text.
- Current item frame has a transparent center.
- Completion stamp has no baked text.
- Stock warning badge is readable at small size.
- Real PNG alpha channel exists.
- No checkerboard, black, white, gray, or colored background pixels.
