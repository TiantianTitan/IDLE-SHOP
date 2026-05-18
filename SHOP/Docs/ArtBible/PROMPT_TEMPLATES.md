# Prompt Templates

Use these prompts with ChatGPT image generation or another image model. Always attach or reference the approved master asset.

## Character State Template

```text
Based on [MASTER_ASSET_NAME], generate [OUTPUT_FILE_NAME].

Requirements:
- Keep the exact same character identity.
- Keep the same hair, face, outfit, color palette, proportions, outline, and lighting.
- Only change the pose/action.
- Mobile cozy idle shop game style.
- Side or slight 3/4 side view.
- Head-to-body ratio 1:1.8.
- 4px soft dark brown outline.
- Warm top-left lighting.
- Soft oval ground shadow only if needed.
- Transparent background.
- No text, no watermark.

Action:
- [ACTION_DESCRIPTION]

Output:
- PNG
- Transparent background
- Same canvas size as the master asset
```

## Customer Pay Example

```text
Based on Customer_Female_A_Base.png, generate customer_pay_01.png.

Requirements:
- Keep the exact same character identity.
- Keep the same hair, face, outfit, color palette, proportions, outline, and lighting.
- Only change the pose/action.
- Mobile cozy idle shop game style.
- Side or slight 3/4 side view, facing the cashier counter.
- Head-to-body ratio 1:1.8.
- 4px soft dark brown outline.
- Warm top-left lighting.
- Transparent background.
- No text, no watermark.

Action:
- Customer is paying at the counter with one hand extended.
- Friendly neutral expression.
- Keep pose readable at small mobile size.

Output:
- PNG
- Transparent background
- Same canvas size as Customer_Female_A_Base.png
```

## FX Template

```text
Generate [OUTPUT_FILE_NAME] for a cozy mobile idle shop game.

Requirements:
- Transparent PNG.
- Clean readable silhouette.
- Warm polished casual game style.
- No text, no watermark.
- Must not obscure UI text.
- Readable at 128px and 256px.

Effect:
- [EFFECT_DESCRIPTION]

Color:
- Primary: [COLOR]
- Secondary: [COLOR]
```
