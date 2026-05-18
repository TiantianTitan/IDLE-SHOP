# Codex and ChatGPT Workflow

## Division of Labor

Use Codex for engineering and asset operations:

- Define asset folders.
- Maintain manifests.
- Add Unity sprite indexes and loaders.
- Connect sprites to gameplay states.
- Add fallback behavior.
- Validate JSON, naming, and missing files.

Use ChatGPT for image production:

- Generate master assets.
- Generate pose/state variants from approved masters.
- Repair consistency problems.
- Produce FX and UI art against the style guide.

## Handoff Format from Codex to ChatGPT

Codex should give ChatGPT a batch request like this:

```text
Batch: MVP30 Customer Set
Reference master: Customer_Female_A_Base.png
Style bible: SHOP/Docs/ArtBible/STYLE_GUIDE.md
Output size: 512x512
Background: real alpha-transparent PNG, no checkerboard background

Generate:
1. customer_enter_01.png
2. customer_idle_01.png
3. customer_pay_01.png
4. customer_happy_01.png
5. customer_leave_01.png

Consistency requirements:
- Same identity, outfit, colors, outline, lighting, proportions.
- Only action changes.
- Side/slight 3/4 side view.
- Real alpha-transparent PNG.
- No gray/white checkerboard pattern or fake transparent background.
```

## Handoff Format from ChatGPT to Codex

After images are generated, provide:

```text
Batch name:
Approved files:
Files needing revision:
Notes:
```

Then Codex should:

1. Place files in the correct folders.
2. Update the manifest status.
3. Wire runtime names into Unity.
4. Run validation.
5. Report missing or inconsistent assets.

## Rule

Never request a random standalone asset if it belongs to a character, shelf, UI, or FX family. Start from the master or from the family style rule.
