using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector用翻訳辞書
/// </summary>
internal static class TranslationDictionary
{
    private const string TranslationDirectory =
        "Translations";

    private static readonly Dictionary<SystemLanguage, TranslationSet>
        translations =
            new Dictionary<SystemLanguage, TranslationSet>();

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

        SplitWhitespace(
            text,
            out string prefix,
            out string body,
            out string suffix);

        if (string.IsNullOrEmpty(body))
        {
            return text;
        }

        string translated =
            TranslateBody(
                translationSet,
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
                switch (translation.Type)
                {
                    case TranslationCsvReader.TranslationType.Exact:
                        AddExact(
                            translationSet,
                            translation.Source,
                            translation.TranslationText);
                        break;

                    case TranslationCsvReader.TranslationType.Partial:
                        AddPartial(
                            translationSet,
                            translation.Source,
                            translation.TranslationText);
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
    /// 本文を翻訳
    /// </summary>
    private static string TranslateBody(
        TranslationSet translationSet,
        string text)
    {
        if (translationSet.Exact.TryGetValue(
                text,
                out string translated))
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
        TranslationSet translationSet,
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
    /// 部分一致翻訳を追加
    /// </summary>
    private static void AddPartial(
        TranslationSet translationSet,
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
    /// 言語別翻訳辞書
    /// </summary>
    private sealed class TranslationSet
    {
        internal Dictionary<string, string> Exact { get; } =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        internal List<TranslationEntry> Partial { get; } =
            new List<TranslationEntry>();

        internal int Count =>
            Exact.Count + Partial.Count;
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