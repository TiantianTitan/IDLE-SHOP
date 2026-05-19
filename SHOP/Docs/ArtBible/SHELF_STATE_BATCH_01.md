# Shelf State Batch 01

Reference master:

`Assets/Art/MVP30/Scene/Shelf/Base/Shelf_Master.png`

Do not generate new shelf designs. Every output must preserve the approved shelf structure.

## Fixed Identity

- Warm wood display shelf.
- Green top decorative trim.
- Green lower trim.
- Same perspective as the master.
- Cozy mobile idle shop game style.
- Warm top-left lighting.
- 4px soft dark brown outline.
- This is a Unity game sprite for Unity Hub / Unity Editor import, not a presentation image.
- Real alpha-transparent background.
- No background pixels at all: no gray/white checkerboard pattern, pure black background, pure white background, white box, gray canvas, colored backdrop, or fake transparent background.
- Internal shelf gaps must also be alpha-transparent where appropriate.
- Same canvas size as the master asset.

## Outputs

| File | Target Folder | State | Status |
| --- | --- | --- | --- |
| shelf_full_01.png | `Assets/Art/MVP30/Scene/Shelf/States/` | Stock healthy. | integrated |
| shelf_low_01.png | `Assets/Art/MVP30/Scene/Shelf/States/` | Stock low. | integrated |
| shelf_empty_01.png | `Assets/Art/MVP30/Scene/Shelf/States/` | Missing stock. | integrated |
| shelf_restocked_01.png | `Assets/Art/MVP30/Scene/Shelf/States/` | Short restock success state. | integrated |

## Acceptance Checklist

- Shelf structure, perspective, wood color, and green trim match the master.
- Full/low/empty states read clearly at phone size.
- Canvas size matches the master.
- Real PNG alpha channel exists.
- No checkerboard, black, white, gray, or colored background pixels.
- No white/gray pixels in internal shelf gaps.
