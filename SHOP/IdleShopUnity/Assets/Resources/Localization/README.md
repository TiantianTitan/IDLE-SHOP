# Localization

Runtime localization files live here because Unity can load them with:

```csharp
Resources.LoadAll<TextAsset>("Localization")
```

Built-in languages:

- `zh-CN` 简体中文
- `en-US` English
- `ja-JP` 日本語
- `fr-FR` Français
- `ko-KR` 한국어
- `es-ES` Español
- `ru-RU` Русский
- `ar-SA` العربية
- `de-DE` Deutsch
- `it-IT` Italiano
- `pt-PT` Português

To add a new language:

1. Copy an existing language file such as `zh-CN.json` or `en-US.json`.
2. Rename it to an IETF-style language code, for example `ja-JP.json`, `ko-KR.json`, `es-ES.json`.
3. Change `languageCode` and `languageName`.
4. Keep every `key` unchanged.
5. Translate only `value`.

Text policy:

- Every new user-facing string must be added to every JSON file in this folder.
- Use `zh-CN.json` as the reference list of required keys.
- Do not leave placeholder or empty values in any language file.
- Run `Pocket Shop/Validate Localization` before committing localization changes.
- Android builds call the same validation automatically, so missing translations block the build.

The game falls back to `zh-CN` at runtime if a key is missing, but build validation should prevent that case.
