# Style Guide

## Visual Direction

- Cozy mobile idle shop game.
- Warm corner shop, polished casual illustration.
- Clear silhouettes for small phone screens.
- Slightly soft shapes, not realistic photography.
- No text inside art assets unless the filename explicitly starts with `ui_` and the UI spec allows it.

## Palette

- Main warm base: `#F6D7A7`
- Wood: `#A56A43`
- Dark brown outline: `#4B2F24`
- UI orange: `#F4A340`
- Coral action: `#DB5142`
- Deep shop green: `#14382E`
- Cool stock blue: `#2E748F`
- Success green: `#75A96E`
- Warning red: `#B7382E`

## Character Rules

- Character art is imported into Unity Hub / Unity Editor as sprites.
- Head-to-body ratio: `1 : 1.8`.
- View: side or slight 3/4 side, facing the counter direction.
- Outline: 4px soft dark brown.
- Lighting: warm top-left light.
- Shadow: soft oval shadow only, never hard cast shadows.
- Background: real alpha-transparent PNG.
- Do not include any background pixels: no gray/white checkerboard, pure black background, pure white background, white card, gray canvas, colored backdrop, or fake transparency pattern inside the image.
- Internal negative spaces such as between legs, arm gaps, bag strap gaps, hair gaps, and clothing gaps must also be alpha-transparent.
- Keep proportions, face, hair, clothing, color, and outline identical across states.

## UI Rules

- UI panels must have clean readable center zones.
- Use warm paper and wood tones for order cards.
- Use orange/coral for money and checkout actions.
- Use blue for stock/restock actions.
- Use green for completed states.
- Avoid decorative text in exported art; text is rendered by Unity.

## FX Rules

- FX art is imported into Unity Hub / Unity Editor as sprites.
- FX must be real alpha-transparent PNG.
- Do not include checkerboard, pure black, pure white, gray, colored, or any other solid-color backgrounds.
- FX should read at 128px and 256px.
- FX should not hide product or order text.
- Use short-lived visual intent:
  - Cash: upward warm float.
  - Restock: circular success ring or spark around shelf/item.
  - Order complete: broad glow behind card or stamp.
  - Upgrade: celebratory burst around product/upgrade target.
