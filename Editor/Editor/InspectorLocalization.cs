using System;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Inspectorの表示文字列を翻訳
/// </summary>
[InitializeOnLoad]
internal static class InspectorLocalization
{
    private const string HarmonyId =
        "net.natade.vrchat-sdk-inspector-jp-localization";

    private static readonly PropertyInfo EditorLanguageProperty =
        FindEditorLanguageProperty();
    
    /// <summary>
    /// Inspector翻訳を初期化
    /// </summary>
    static InspectorLocalization()
    {
        try
        {
            TranslationDictionary.Load();
            InstallPatches();

            Debug.Log(
                $"[InspectorLocalization] 初期化しました " +
                $"({TranslationDictionary.Count}件の翻訳)");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[InspectorLocalization] 初期化に失敗しました\n{ex}");
        }
    }

    /// <summary>
    /// Harmonyフックを設定
    /// </summary>
    private static void InstallPatches()
    {
        var harmony = new Harmony(HarmonyId);

        harmony.UnpatchAll(HarmonyId);

        InstallGuiStyleDrawPatch(harmony);
        InstallBeginPropertyPatch(harmony);
    }

    /// <summary>
    /// GUIStyle.Drawへのフックを設定
    /// </summary>
    private static void InstallGuiStyleDrawPatch(
        Harmony harmony)
    {
        MethodInfo target = typeof(GUIStyle).GetMethod(
            "Draw",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(Rect),
                typeof(GUIContent),
                typeof(int),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            },
            null);

        if (target == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "UnityEngine.GUIStyle.Drawが見つかりません。");

            return;
        }

        MethodInfo prefix =
            typeof(InspectorLocalization).GetMethod(
                nameof(GuiStyleDrawPrefix),
                BindingFlags.Static | BindingFlags.NonPublic);

        if (prefix == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                $"{nameof(GuiStyleDrawPrefix)}が見つかりません。");

            return;
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(prefix));
    }

    /// <summary>
    /// EditorGUI.BeginPropertyInternalへのフックを設定
    /// </summary>
    private static void InstallBeginPropertyPatch(
        Harmony harmony)
    {
        MethodInfo target = typeof(EditorGUI).GetMethod(
            "BeginPropertyInternal",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(Rect),
                typeof(GUIContent),
                typeof(SerializedProperty)
            },
            null);

        if (target == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "UnityEditor.EditorGUI.BeginPropertyInternalが" +
                "見つかりません。");

            return;
        }

        MethodInfo postfix =
            typeof(InspectorLocalization).GetMethod(
                nameof(BeginPropertyPostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

        if (postfix == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                $"{nameof(BeginPropertyPostfix)}が見つかりません。");

            return;
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(postfix));
    }

    /// <summary>
    /// Editor Languageプロパティを検索
    /// </summary>
    private static PropertyInfo FindEditorLanguageProperty()
    {
        Type localizationDatabaseType =
            typeof(Editor).Assembly.GetType(
                "UnityEditor.LocalizationDatabase");

        if (localizationDatabaseType == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "LocalizationDatabaseが見つかりません。");

            return null;
        }

        PropertyInfo property =
            localizationDatabaseType.GetProperty(
                "currentEditorLanguage",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);

        if (property == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "currentEditorLanguageが見つかりません。");
        }

        return property;
    }

    /// <summary>
    /// 現在のEditor Languageを取得
    /// </summary>
    private static bool TryGetEditorLanguage(
        out SystemLanguage language)
    {
        language = default;

        if (EditorLanguageProperty == null)
        {
            return false;
        }

        try
        {
            object value =
                EditorLanguageProperty.GetValue(null);

            if (value is SystemLanguage systemLanguage)
            {
                language = systemLanguage;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// GUI描画文字列を翻訳
    /// </summary>
    private static void GuiStyleDrawPrefix(
        GUIContent content)
    {
        if (content == null)
        {
            return;
        }

        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return;
        }

        string text;

        try
        {
            text = content.text;
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        content.text =
            TranslationDictionary.Translate(
                language,
                text);
    }

    /// <summary>
    /// PropertyFieldの表示文字列を翻訳
    /// </summary>
    private static void BeginPropertyPostfix(
        ref GUIContent __result)
    {
        if (__result == null)
        {
            return;
        }

        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return;
        }

        if (!string.IsNullOrEmpty(__result.text))
        {
            __result.text =
                TranslationDictionary.Translate(
                    language,
                    __result.text);
        }

        if (!string.IsNullOrEmpty(__result.tooltip))
        {
            __result.tooltip =
                TranslationDictionary.Translate(
                    language,
                    __result.tooltip);
        }
    }
}
