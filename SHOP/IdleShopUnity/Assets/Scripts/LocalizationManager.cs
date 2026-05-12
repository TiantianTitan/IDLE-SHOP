using System;
using System.Collections.Generic;
using UnityEngine;

public static class LocalizationManager
{
    [Serializable]
    private sealed class Entry
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    private sealed class Table
    {
        public string languageCode = "zh-CN";
        public string languageName = "简体中文";
        public Entry[] entries = Array.Empty<Entry>();
    }

    public readonly struct LanguageInfo
    {
        public readonly string Code;
        public readonly string Name;

        public LanguageInfo(string code, string name)
        {
            Code = code;
            Name = name;
        }
    }

    private const string LanguageKey = "PocketShop.Language";
    private const string DefaultLanguage = "zh-CN";
    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
    private static readonly List<LanguageInfo> Languages = new List<LanguageInfo>();
    private static bool loaded;

    public static string CurrentLanguage { get; private set; } = DefaultLanguage;

    public static IReadOnlyList<LanguageInfo> AvailableLanguages
    {
        get
        {
            EnsureLoaded();
            return Languages;
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        Tables.Clear();
        Languages.Clear();
        TextAsset[] assets = Resources.LoadAll<TextAsset>("Localization");
        for (int i = 0; i < assets.Length; i++)
        {
            string text = assets[i].text.TrimStart();
            if (!text.StartsWith("{", StringComparison.Ordinal))
            {
                continue;
            }

            Table table;
            try
            {
                table = JsonUtility.FromJson<Table>(text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Skipping invalid localization asset '{assets[i].name}': {exception.Message}");
                continue;
            }

            if (table == null || string.IsNullOrWhiteSpace(table.languageCode))
            {
                continue;
            }

            var map = new Dictionary<string, string>();
            for (int j = 0; j < table.entries.Length; j++)
            {
                Entry entry = table.entries[j];
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    map[entry.key] = entry.value;
                }
            }

            Tables[table.languageCode] = map;
            Languages.Add(new LanguageInfo(table.languageCode, table.languageName));
        }

        Languages.Sort((left, right) => LanguageOrder(left.Code).CompareTo(LanguageOrder(right.Code)));

        if (!Tables.ContainsKey(DefaultLanguage) && Languages.Count > 0)
        {
            CurrentLanguage = Languages[0].Code;
        }

        string saved = PlayerPrefs.GetString(LanguageKey, CurrentLanguage);
        if (Tables.ContainsKey(saved))
        {
            CurrentLanguage = saved;
        }

        loaded = true;
    }

    private static int LanguageOrder(string code)
    {
        switch (code)
        {
            case "zh-CN": return 0;
            case "en-US": return 1;
            case "ja-JP": return 2;
            case "ko-KR": return 3;
            case "fr-FR": return 4;
            case "de-DE": return 5;
            case "es-ES": return 6;
            case "it-IT": return 7;
            case "pt-PT": return 8;
            case "ru-RU": return 9;
            case "ar-SA": return 10;
            default: return 100;
        }
    }

    public static void SetLanguage(string languageCode)
    {
        EnsureLoaded();
        if (!Tables.ContainsKey(languageCode))
        {
            return;
        }

        CurrentLanguage = languageCode;
        PlayerPrefs.SetString(LanguageKey, languageCode);
        PlayerPrefs.Save();
    }

    public static string T(string key)
    {
        EnsureLoaded();
        if (Tables.TryGetValue(CurrentLanguage, out Dictionary<string, string> current) && current.TryGetValue(key, out string value))
        {
            return value;
        }

        if (Tables.TryGetValue(DefaultLanguage, out Dictionary<string, string> fallback) && fallback.TryGetValue(key, out string fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public static string F(string key, params object[] args)
    {
        string template = T(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
