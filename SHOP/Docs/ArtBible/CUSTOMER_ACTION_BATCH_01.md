# Customer Action Batch 01

Reference master:

`Assets/Art/MVP30/Characters/Customer/Base/Customer_Female_A_Base.png`

Do not generate new character directions. Every output must preserve the approved customer identity.

## Fixed Identity

- Short brown bob hair with side-swept bangs.
- Cream cardigan.
- Coral top.
- Teal skirt.
- Brown small shoulder bag.
- Brown shoes.
- Cozy mobile idle shop game style.
- Warm top-left lighting.
- 4px soft dark brown outline.
- This is a Unity game sprite for Unity Hub / Unity Editor import, not a presentation image.
- Real alpha-transparent background.
- No background pixels at all: no gray/white checkerboard pattern, pure black background, pure white background, white box, gray canvas, colored backdrop, or fake transparent background.
- Internal negative spaces such as between legs, arm gaps, bag strap gaps, hair gaps, and clothing gaps must also be alpha-transparent.
- Same canvas size as the master asset.

## Outputs

| File | Target Folder | Action | Status |
| --- | --- | --- | --- |
| customer_enter_01.png | `Assets/Art/MVP30/Characters/Customer/Enter/` | Customer entering from the side, gentle walking pose, facing the counter. | integrated |
| customer_idle_01.png | `Assets/Art/MVP30/Characters/Customer/Idle/` | Customer waiting at counter, relaxed neutral pose. | integrated |
| customer_pay_01.png | `Assets/Art/MVP30/Characters/Customer/Pay/` | Customer paying with one hand extended toward the counter. | integrated |
| customer_happy_01.png | `Assets/Art/MVP30/Characters/Customer/Happy/` | Customer smiling after order completion. | integrated |
| customer_leave_01.png | `Assets/Art/MVP30/Characters/Customer/Leave/` | Customer satisfied and leaving, slight step-away pose. | integrated |

## ChatGPT Batch Prompt

```text
Based on Customer_Female_A_Base.png, generate Customer Action Batch 01.

Generate only these files:
1. customer_enter_01.png
2. customer_idle_01.png
3. customer_pay_01.png
4. customer_happy_01.png
5. customer_leave_01.png

Requirements:
- This is a Unity game sprite for Unity Hub / Unity Editor import, not a presentation image.
- Keep the exact same character identity.
- Keep the same short brown bob hair, cream cardigan, coral top, teal skirt, brown shoulder bag, and brown shoes.
- Keep the same head-to-body ratio, outline thickness, color palette, and warm top-left lighting.
- Only change the pose/action.
- Side or slight 3/4 side view, facing the cashier counter direction.
- Real alpha-transparent background.
- Do not draw any background pixels: no gray/white checkerboard, pure black background, pure white background, white box, gray canvas, colored backdrop, or fake transparent background.
- Internal negative spaces such as between legs, arm gaps, bag strap gaps, hair gaps, and clothing gaps must also be alpha-transparent.
- No text, no watermark.
- Same canvas size as Customer_Female_A_Base.png.
- Mobile cozy idle shop game style, readable on phone screen.

Do not create new character designs.
Do not change hairstyle, clothing, colors, face, proportions, or lighting.
```

## Acceptance Checklist

- Hair shape and length match the master.
- Cardigan, top, skirt, bag, and shoes match the master.
- Canvas size matches the master.
- Real PNG alpha channel exists.
- No background pixels exist.
- No checkerboard, black, white, gray, or colored background pixels.
- No white or gray pixels in internal cutout spaces such as between legs, arm gaps, bag strap gaps, or hair gaps.
- Character remains readable at 128px.
- Each action reads clearly without changing identity.
