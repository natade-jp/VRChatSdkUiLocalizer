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
        InstallHandlePrefixLabelPatch(harmony);
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
    /// EditorGUI.HandlePrefixLabelInternalへのフックを設定
    /// </summary>
    private static void InstallHandlePrefixLabelPatch(
        Harmony harmony)
    {
        MethodInfo target = typeof(EditorGUI).GetMethod(
            "HandlePrefixLabelInternal",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(Rect),
                typeof(Rect),
                typeof(GUIContent),
                typeof(int),
                typeof(GUIStyle)
            },
            null);

        if (target == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "UnityEditor.EditorGUI.HandlePrefixLabelInternalが" +
                "見つかりません。");

            return;
        }

        MethodInfo prefix =
            typeof(InspectorLocalization).GetMethod(
                nameof(HandlePrefixLabelInternalPrefix),
                BindingFlags.Static | BindingFlags.NonPublic);

        if (prefix == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                $"{nameof(HandlePrefixLabelInternalPrefix)}が" +
                "見つかりません。");

            return;
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(prefix));
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
    /// GUIContentの表示文字列を翻訳
    /// </summary>
    private static void TranslateContent(
        GUIContent content,
        SystemLanguage language)
    {
        if (content == null)
        {
            return;
        }

        string text;
        string tooltip;

        try
        {
            text = content.text;
            tooltip = content.tooltip;
        }
        catch
        {
            return;
        }

        if (!string.IsNullOrEmpty(text))
        {
            content.text =
                TranslationDictionary.Translate(
                    language,
                    text);
        }

        if (!string.IsNullOrEmpty(tooltip))
        {
            content.tooltip =
                TranslationDictionary.Translate(
                    language,
                    tooltip);
        }
    }

    /// <summary>
    /// GUI描画文字列を翻訳
    /// </summary>
    private static void GuiStyleDrawPrefix(
        GUIContent content)
    {
        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return;
        }

        TranslateContent(content, language);
    }

    /// <summary>
    /// PropertyFieldの表示文字列を翻訳
    /// </summary>
    private static void BeginPropertyPostfix(
        ref GUIContent __result)
    {
        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return;
        }

        TranslateContent(__result, language);
    }

    /// <summary>
    /// Prefix Labelの表示文字列を翻訳
    /// </summary>
    private static void HandlePrefixLabelInternalPrefix(
        GUIContent label)
    {
        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return;
        }

        TranslateContent(label, language);
    }
}
