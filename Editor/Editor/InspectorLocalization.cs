using System;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Unity InspectorおよびVRChat SDK UIの表示文字列を翻訳
/// </summary>
[InitializeOnLoad]
internal static class InspectorLocalization
{
    private const string HarmonyId =
        "net.natade.vrchat-sdk-inspector-jp-localization";

    private static readonly PropertyInfo EditorLanguageProperty =
        FindEditorLanguageProperty();

    /// <summary>
    /// 翻訳処理を初期化
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

    [MenuItem("VRChat SDK/Localization/Reload Translations")]
    private static void ReloadTranslations()
    {
        TranslationDictionary.Load();
    }

    /// <summary>
    /// Harmonyフックを設定
    /// </summary>
    private static void InstallPatches()
    {
        var harmony = new Harmony(HarmonyId);

        harmony.UnpatchAll(HarmonyId);

        InstallInspectorPatches(harmony);
        InstallControlPanelPatches(harmony);
    }

    /// <summary>
    /// Inspector用フックを設定
    /// </summary>
    private static void InstallInspectorPatches(
        Harmony harmony)
    {
        // Inspectorのセクション名やHelpBoxなど、
        // GUIStyle.Drawへ直接渡されるGUIContentを翻訳するためのフック
        PatchPrefix(
            harmony,
            AccessTools.Method(
                typeof(GUIStyle),
                "Draw",
                new[]
                {
                    typeof(Rect),
                    typeof(GUIContent),
                    typeof(int),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool)
                }),
            nameof(GuiStyleDrawPrefix),
            "UnityEngine.GUIStyle.Draw");

        // SerializedPropertyから生成されるInspectorの
        // プロパティ名やTooltipを翻訳するためのフック
        // Pull、Gravity、Integration Typeなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                typeof(EditorGUI),
                "BeginPropertyInternal",
                new[]
                {
                    typeof(Rect),
                    typeof(GUIContent),
                    typeof(SerializedProperty)
                }),
            nameof(BeginPropertyPostfix),
            "UnityEditor.EditorGUI.BeginPropertyInternal");

        // Vector3Fieldなどで使用されるPrefix Labelを
        // 翻訳するためのフック
        // ContactやPhysBone ColliderのRotationなどで使用
        PatchPrefix(
            harmony,
            AccessTools.Method(
                typeof(EditorGUI),
                "HandlePrefixLabelInternal",
                new[]
                {
                    typeof(Rect),
                    typeof(Rect),
                    typeof(GUIContent),
                    typeof(int),
                    typeof(GUIStyle)
                }),
            nameof(HandlePrefixLabelInternalPrefix),
            "UnityEditor.EditorGUI.HandlePrefixLabelInternal");
    }

    /// <summary>
    /// SDK Control Panel用フックを設定
    /// </summary>
    private static void InstallControlPanelPatches(
        Harmony harmony)
    {
        Type controlPanelType =
            AccessTools.TypeByName(
                "VRCSdkControlPanel");

        // SDK Control PanelのBuilder画面を最初に構築した直後に
        // Visual Tree内の固定文字列を翻訳するためのフック
        // Prepare Your Content、Buildなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                controlPanelType,
                "ShowBuilders"),
            nameof(ShowBuildersPostfix),
            "VRCSdkControlPanel.ShowBuilders");

        // Validation UIの構築完了後に翻訳
        // Error、Warning、Performance、Info、Linkなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                controlPanelType,
                "CreateIssuesGUI"),
            nameof(CreateIssuesGuiPostfix),
            "VRCSdkControlPanel.CreateIssuesGUI");

        Type stepFoldoutType =
            AccessTools.TypeByName(
                "VRC.SDKBase.Editor.Elements.StepFoldout");

        // StepFoldoutのタイトルが後から変更される場合に
        // 新しいタイトルを翻訳するためのフック
        // Review Any Alerts (10)などで使用
        PatchPrefix(
            harmony,
            AccessTools.Method(
                stepFoldoutType,
                "SetTitle",
                new[] { typeof(string) }),
            nameof(StepFoldoutSetTitlePrefix),
            "VRC.SDKBase.Editor.Elements.StepFoldout.SetTitle");

        Type avatarBuilderType =
            AccessTools.TypeByName(
                "VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder");

        // Avatar情報UIの構築完了後に翻訳
        // Name、Visibility、Primary Styleなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                avatarBuilderType,
                "CreateContentInfoGUI",
                new[] { typeof(VisualElement) }),
            nameof(AvatarBuilderGuiPostfix),
            "VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.CreateContentInfoGUI");

        // AvatarビルドUIの構築完了後に翻訳
        // Build Type、Platform(s)、Build & Publishなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                avatarBuilderType,
                "CreateBuildGUI",
                new[] { typeof(VisualElement) }),
            nameof(AvatarBuilderGuiPostfix),
            "VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.CreateBuildGUI");
    }

    /// <summary>
    /// Harmony Prefixを登録
    /// </summary>
    private static void PatchPrefix(
        Harmony harmony,
        MethodBase target,
        string patchMethodName,
        string targetName)
    {
        if (target == null)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"{targetName}が見つかりません。");

            return;
        }

        MethodInfo patch =
            AccessTools.Method(
                typeof(InspectorLocalization),
                patchMethodName);

        if (patch == null)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"{patchMethodName}が見つかりません。");

            return;
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(patch));
    }

    /// <summary>
    /// Harmony Postfixを登録
    /// </summary>
    private static void PatchPostfix(
        Harmony harmony,
        MethodBase target,
        string patchMethodName,
        string targetName)
    {
        if (target == null)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"{targetName}が見つかりません。");

            return;
        }

        MethodInfo patch =
            AccessTools.Method(
                typeof(InspectorLocalization),
                patchMethodName);

        if (patch == null)
        {
            Debug.LogWarning(
                $"[InspectorLocalization] " +
                $"{patchMethodName}が見つかりません。");

            return;
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(patch));
    }

    /// <summary>
    /// SDK Builder初期UIを翻訳
    /// </summary>
    private static void ShowBuildersPostfix(
        object __instance)
    {
        FieldInfo builderPanelField =
            AccessTools.Field(
                __instance.GetType(),
                "_builderPanel");

        if (builderPanelField == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "_builderPanelが見つかりません。");

            return;
        }

        VisualElement builderPanel =
            builderPanelField.GetValue(__instance)
                as VisualElement;

        if (builderPanel == null)
        {
            Debug.LogWarning(
                "[InspectorLocalization] " +
                "_builderPanelが取得できません。");

            return;
        }

        TranslateVisualElement(builderPanel);
    }

    /// <summary>
    /// Validation UIを翻訳
    /// </summary>
    private static void CreateIssuesGuiPostfix(
        VisualElement __result)
    {
        TranslateVisualElement(__result);
    }

    /// <summary>
    /// StepFoldoutタイトルを翻訳
    /// </summary>
    private static void StepFoldoutSetTitlePrefix(
        ref string title)
    {
        title =
            Translate(
                title,
                TranslationCsvReader.TranslationTarget.SDK);
    }

    /// <summary>
    /// Avatar BuilderのUIを翻訳
    /// </summary>
    private static void AvatarBuilderGuiPostfix(
        VisualElement root)
    {
        TranslateVisualElement(root);
    }

    /// <summary>
    /// VisualElement配下の表示文字列を翻訳
    /// </summary>
    private static void TranslateVisualElement(
        VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Label label
                 in root.Query<Label>().ToList())
        {
            label.text =
                Translate(
                    label.text,
                    TranslationCsvReader.TranslationTarget.SDK);

            label.tooltip =
                Translate(
                    label.tooltip,
                    TranslationCsvReader.TranslationTarget.SDK);
        }

        foreach (Button button
                 in root.Query<Button>().ToList())
        {
            button.text =
                Translate(
                    button.text,
                    TranslationCsvReader.TranslationTarget.SDK);

            button.tooltip =
                Translate(
                    button.tooltip,
                    TranslationCsvReader.TranslationTarget.SDK);
        }

        foreach (VisualElement element
                 in root.Query<VisualElement>().ToList())
        {
            element.tooltip =
                Translate(
                    element.tooltip,
                    TranslationCsvReader.TranslationTarget.SDK);
        }
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
    /// 表示文字列を翻訳
    /// </summary>
    private static string Translate(
        string text,
        TranslationCsvReader.TranslationTarget target)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (!TryGetEditorLanguage(
                out SystemLanguage language))
        {
            return text;
        }

        return TranslationDictionary.Translate(
            language,
            target,
            text);
    }

    /// <summary>
    /// GUIContentの表示文字列を翻訳
    /// </summary>
    private static void TranslateContent(
        GUIContent content)
    {
        if (content == null)
        {
            return;
        }

        try
        {
            content.text =
                Translate(
                    content.text,
                    TranslationCsvReader.TranslationTarget.Inspector);

            content.tooltip =
                Translate(
                    content.tooltip,
                    TranslationCsvReader.TranslationTarget.Inspector);
        }
        catch
        {
            // Unity内部のGUIContent取得失敗は無視
        }
    }

    /// <summary>
    /// GUI描画文字列を翻訳
    /// </summary>
    private static void GuiStyleDrawPrefix(
        GUIContent content)
    {
        TranslateContent(content);
    }

    /// <summary>
    /// PropertyFieldの表示文字列を翻訳
    /// </summary>
    private static void BeginPropertyPostfix(
        ref GUIContent __result)
    {
        TranslateContent(__result);
    }

    /// <summary>
    /// Prefix Labelの表示文字列を翻訳
    /// </summary>
    private static void HandlePrefixLabelInternalPrefix(
        GUIContent label)
    {
        TranslateContent(label);
    }
}
