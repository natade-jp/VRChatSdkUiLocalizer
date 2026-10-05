using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 翻訳辞書
/// </summary>
internal static class TranslationDictionary
{
    private const string TranslationDirectory =
        "Translations";

    private static readonly Dictionary<SystemLanguage, TranslationSet>
        translations =
            new Dictionary<SystemLanguage, TranslationSet>();

    private static readonly HashSet<string> DebugLoggedTexts =
        new HashSet<string>(
            StringComparer.Ordinal);

    /// <summary>
    /// 翻訳件数
    /// </summary>
    internal static int Count =>
        translations.Values.Sum(set => set.Count);

    /// <summary>
    /// 翻訳辞書を読み込み
    /// </summary>
    internal static void Load()
    {
        translations.Clear();
        DebugLoggedTexts.Clear();

        string directory = FindTranslationDirectory();

        if (directory == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] 翻訳フォルダが見つかりません。");
            return;
        }

        string[] languageDirectories =
            AssetDatabase.GetSubFolders(directory)
                .OrderBy(
                    path => path,
                    StringComparer.Ordinal)
                .ToArray();

        foreach (string languageDirectory in languageDirectories)
        {
            LoadLanguageDirectory(languageDirectory);
        }
    }

    /// <summary>
    /// 指定言語の文字列を翻訳
    /// </summary>
    internal static string Translate(
        SystemLanguage language,
        TranslationCsvReader.TranslationTarget target,
        string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (!translations.TryGetValue(
                language,
                out TranslationSet translationSet))
        {
            return text;
        }

        if (!translationSet.Targets.TryGetValue(
                target,
                out TargetTranslationSet targetSet))
        {
            return text;
        }

        SplitWhitespace(
            text,
            out string prefix,
            out string body,
            out string suffix);

        if (string.IsNullOrEmpty(body))
        {
            return text;
        }

        LogDebugText(
            target,
            targetSet,
            body);

        string translated =
            TranslateBody(
                targetSet,
                body);

        if (translated == body)
        {
            return text;
        }

        return prefix + translated + suffix;
    }

    /// <summary>
    /// 翻訳フォルダを検索
    /// </summary>
    private static string FindTranslationDirectory()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "TranslationDictionary t:MonoScript");

        foreach (string guid in guids)
        {
            string scriptPath =
                AssetDatabase.GUIDToAssetPath(guid);

            MonoScript script =
                AssetDatabase.LoadAssetAtPath<MonoScript>(
                    scriptPath);

            if (script == null ||
                script.GetClass() != typeof(TranslationDictionary))
            {
                continue;
            }

            string editorDirectory =
                Path.GetDirectoryName(scriptPath);

            if (string.IsNullOrEmpty(editorDirectory))
            {
                continue;
            }

            string translationDirectory =
                Path.Combine(
                        editorDirectory,
                        TranslationDirectory)
                    .Replace('\\', '/');

            if (AssetDatabase.IsValidFolder(
                    translationDirectory))
            {
                return translationDirectory;
            }
        }

        return null;
    }

    /// <summary>
    /// 言語フォルダを読み込み
    /// </summary>
    private static void LoadLanguageDirectory(
        string directory)
    {
        string languageName =
            Path.GetFileName(directory);

        if (!Enum.TryParse(
                languageName,
                false,
                out SystemLanguage language))
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"不明な言語フォルダです: {languageName}");
            return;
        }

        if (translations.ContainsKey(language))
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"言語フォルダが重複しています: {languageName}");
            return;
        }

        var translationSet =
            new TranslationSet();

        string[] csvPaths =
            AssetDatabase
                .FindAssets(
                    "t:TextAsset",
                    new[] { directory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path =>
                    string.Equals(
                        Path.GetExtension(path),
                        ".csv",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    path => path,
                    StringComparer.Ordinal)
                .ToArray();

        foreach (string csvPath in csvPaths)
        {
            LoadCsv(
                csvPath,
                translationSet);
        }

        translations.Add(
            language,
            translationSet);
    }

    /// <summary>
    /// 翻訳CSVを読み込み
    /// </summary>
    private static void LoadCsv(
        string path,
        TranslationSet translationSet)
    {
        TextAsset asset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(path);

        if (asset == null)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"CSVを読み込めません: {path}");
            return;
        }

        try
        {
            using var reader =
                new StringReader(asset.text);

            IReadOnlyList<TranslationCsvReader.Translation>
                csvTranslations =
                    TranslationCsvReader.Read(
                        reader,
                        message =>
                            Debug.LogWarning(
                                $"[InspectorLocalization] " +
                                $"{path}: {message}"));

            foreach (
                TranslationCsvReader.Translation translation
                in csvTranslations)
            {
                TargetTranslationSet targetSet =
                    translationSet.GetOrCreate(
                        translation.Target);

                switch (translation.Type)
                {
                    case TranslationCsvReader.TranslationType.Exact:
                        AddExact(
                            targetSet,
                            translation.Source,
                            translation.TranslationText);
                        break;

                    case TranslationCsvReader.TranslationType.Format:
                        AddFormat(
                            targetSet,
                            translation.Source,
                            translation.TranslationText);
                        break;

                    case TranslationCsvReader.TranslationType.Partial:
                        AddPartial(
                            targetSet,
                            translation.Source,
                            translation.TranslationText);
                        break;

                    case TranslationCsvReader.TranslationType.Debug:
                        AddDebug(
                            targetSet,
                            translation.Source);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"CSVの読み込みに失敗しました: {path}\n" +
                ex.Message);
        }
    }

    /// <summary>
    /// デバッグ対象の文字列を出力
    /// </summary>
    private static void LogDebugText(
        TranslationCsvReader.TranslationTarget target,
        TargetTranslationSet translationSet,
        string text)
    {
        foreach (string source in translationSet.Debug)
        {
            if (!text.Contains(source))
            {
                continue;
            }

            string key =
                target + "\n" + text;

            if (DebugLoggedTexts.Add(key))
            {
                Debug.Log(
                    $"[InspectorLocalization] Debug ({target}): " +
                    text);
            }

            return;
        }
    }

    /// <summary>
    /// 本文を翻訳
    /// </summary>
    private static string TranslateBody(
        TargetTranslationSet translationSet,
        string text)
    {
        if (translationSet.Exact.TryGetValue(
                text,
                out string translated))
        {
            return translated;
        }

        if (TryTranslateFormat(
                translationSet,
                text,
                out translated))
        {
            return translated;
        }

        string result = text;

        foreach (
            TranslationEntry entry
            in translationSet.Partial)
        {
            if (result.Contains(entry.Source))
            {
                result = result.Replace(
                    entry.Source,
                    entry.Translation);
            }
        }

        return result;
    }

    /// <summary>
    /// Format翻訳を検索
    /// </summary>
    private static bool TryTranslateFormat(
        TargetTranslationSet translationSet,
        string text,
        out string translated)
    {
        foreach (
            FormatTranslationEntry entry
            in translationSet.Format)
        {
            if (!string.IsNullOrEmpty(entry.Prefix) &&
                !text.StartsWith(
                    entry.Prefix,
                    StringComparison.Ordinal))
            {
                continue;
            }

            Match match =
                entry.Pattern.Match(text);

            if (!match.Success)
            {
                continue;
            }

            translated =
                ApplyFormatTranslation(
                    entry,
                    match);

            return true;
        }

        translated = null;
        return false;
    }

    /// <summary>
    /// Format翻訳を適用
    /// </summary>
    private static string ApplyFormatTranslation(
        FormatTranslationEntry entry,
        Match match)
    {
        return PlaceholderPattern.Replace(
            entry.Translation,
            placeholderMatch =>
            {
                if (!int.TryParse(
                        placeholderMatch.Groups[1].Value,
                        out int index))
                {
                    return placeholderMatch.Value;
                }

                string groupName =
                    GetFormatGroupName(index);

                Group group =
                    match.Groups[groupName];

                if (!group.Success)
                {
                    return placeholderMatch.Value;
                }

                return group.Value;
            });
    }

    /// <summary>
    /// 前後の空白文字を分離
    /// </summary>
    private static void SplitWhitespace(
        string text,
        out string prefix,
        out string body,
        out string suffix)
    {
        int start = 0;

        while (start < text.Length &&
               char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        if (start == text.Length)
        {
            prefix = text;
            body = string.Empty;
            suffix = string.Empty;
            return;
        }

        int end = text.Length - 1;

        while (end >= start &&
               char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        prefix = text.Substring(0, start);
        body = text.Substring(
            start,
            end - start + 1);
        suffix = text.Substring(end + 1);
    }

    /// <summary>
    /// 完全一致翻訳を追加
    /// </summary>
    private static void AddExact(
        TargetTranslationSet translationSet,
        string source,
        string translation)
    {
        if (translationSet.Exact.ContainsKey(source))
        {
            return;
        }

        translationSet.Exact.Add(
            source,
            translation);
    }

    /// <summary>
    /// Format翻訳を追加
    /// </summary>
    private static void AddFormat(
        TargetTranslationSet translationSet,
        string source,
        string translation)
    {
        foreach (
            FormatTranslationEntry entry
            in translationSet.Format)
        {
            if (entry.Source == source)
            {
                return;
            }
        }

        Regex pattern =
            CreateFormatPattern(source);

        string prefix =
            GetFormatPrefix(source);

        translationSet.Format.Add(
            new FormatTranslationEntry(
                source,
                translation,
                prefix,
                pattern));
    }

    /// <summary>
    /// 部分一致翻訳を追加
    /// </summary>
    private static void AddPartial(
        TargetTranslationSet translationSet,
        string source,
        string translation)
    {
        foreach (
            TranslationEntry entry
            in translationSet.Partial)
        {
            if (entry.Source == source)
            {
                return;
            }
        }

        translationSet.Partial.Add(
            new TranslationEntry(
                source,
                translation));
    }

    /// <summary>
    /// デバッグ対象を追加
    /// </summary>
    private static void AddDebug(
        TargetTranslationSet translationSet,
        string source)
    {
        if (translationSet.Debug.Contains(source))
        {
            return;
        }

        translationSet.Debug.Add(source);
    }

    /// <summary>
    /// Format用正規表現を生成
    /// </summary>
    private static Regex CreateFormatPattern(
        string source)
    {
        var pattern =
            new StringBuilder("^");

        int position = 0;

        foreach (
            Match match
            in PlaceholderPattern.Matches(source))
        {
            if (match.Index > position)
            {
                pattern.Append(
                    Regex.Escape(
                        source.Substring(
                            position,
                            match.Index - position)));
            }

            int index =
                int.Parse(match.Groups[1].Value);

            pattern.Append("(?<");
            pattern.Append(GetFormatGroupName(index));
            pattern.Append(">.*?)");

            position =
                match.Index + match.Length;
        }

        if (position < source.Length)
        {
            pattern.Append(
                Regex.Escape(
                    source.Substring(position)));
        }

        pattern.Append("$");

        return new Regex(
            pattern.ToString(),
            RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Formatの固定接頭辞を取得
    /// </summary>
    private static string GetFormatPrefix(
        string source)
    {
        Match match =
            PlaceholderPattern.Match(source);

        if (!match.Success)
        {
            return source;
        }

        if (match.Index == 0)
        {
            return string.Empty;
        }

        return source.Substring(
            0,
            match.Index);
    }

    /// <summary>
    /// Formatのグループ名を取得
    /// </summary>
    private static string GetFormatGroupName(
        int index)
    {
        return "value" + index;
    }

    private static readonly Regex PlaceholderPattern =
        new Regex(
            @"\{(\d+)\}",
            RegexOptions.CultureInvariant);

    /// <summary>
    /// 言語別翻訳辞書
    /// </summary>
    private sealed class TranslationSet
    {
        internal Dictionary<
            TranslationCsvReader.TranslationTarget,
            TargetTranslationSet> Targets { get; } =
                new Dictionary<
                    TranslationCsvReader.TranslationTarget,
                    TargetTranslationSet>();

        internal int Count =>
            Targets.Values.Sum(set => set.Count);

        /// <summary>
        /// 対象別翻訳辞書を取得
        /// </summary>
        internal TargetTranslationSet GetOrCreate(
            TranslationCsvReader.TranslationTarget target)
        {
            if (!Targets.TryGetValue(
                    target,
                    out TargetTranslationSet translationSet))
            {
                translationSet =
                    new TargetTranslationSet();

                Targets.Add(
                    target,
                    translationSet);
            }

            return translationSet;
        }
    }

    /// <summary>
    /// 対象別翻訳辞書
    /// </summary>
    private sealed class TargetTranslationSet
    {
        internal Dictionary<string, string> Exact { get; } =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        internal List<FormatTranslationEntry> Format { get; } =
            new List<FormatTranslationEntry>();

        internal List<TranslationEntry> Partial { get; } =
            new List<TranslationEntry>();

        internal List<string> Debug { get; } =
            new List<string>();

        internal int Count =>
            Exact.Count +
            Format.Count +
            Partial.Count +
            Debug.Count;
    }

    /// <summary>
    /// Format翻訳項目
    /// </summary>
    private sealed class FormatTranslationEntry
    {
        internal string Source { get; }
        internal string Translation { get; }
        internal string Prefix { get; }
        internal Regex Pattern { get; }

        /// <summary>
        /// Format翻訳項目を初期化
        /// </summary>
        internal FormatTranslationEntry(
            string source,
            string translation,
            string prefix,
            Regex pattern)
        {
            Source = source;
            Translation = translation;
            Prefix = prefix;
            Pattern = pattern;
        }
    }

    /// <summary>
    /// 部分一致翻訳項目
    /// </summary>
    private sealed class TranslationEntry
    {
        internal string Source { get; }
        internal string Translation { get; }

        /// <summary>
        /// 翻訳項目を初期化
        /// </summary>
        internal TranslationEntry(
            string source,
            string translation)
        {
            Source = source;
            Translation = translation;
        }
    }
}
