using System;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Unity InspectorおよびVRChat SDK UIの表示文字列を翻訳
/// </summary>
[InitializeOnLoad]
internal static class VRChatSdkUiLocalizer
{
    private const string HarmonyId =
        "net.natade.vrchat-sdk-ui-localizer";

    private static readonly PropertyInfo EditorLanguageProperty =
        FindEditorLanguageProperty();

    /// <summary>
    /// 翻訳処理を初期化
    /// </summary>
    static VRChatSdkUiLocalizer()
    {
        EditorApplication.delayCall += Initialize;
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    private static void Initialize()
    {
        try
        {
            TranslationDictionary.Load();

            InstallPatches();

            Debug.Log(
                $"[VRChatSdkUiLocalizer] " +
                $"初期化完了: {TranslationDictionary.Count} 件");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[VRChatSdkUiLocalizer] " +
                $"初期化に失敗しました\n{ex}");
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

        // VRChat WorldsのUI Toolkit Inspector構築完了を検出
        Type inspectorBaseType =
            AccessTools.TypeByName("VRC.SDK3.Editor.VRCInspectorBase");

        PatchPostfix(
            harmony,
            AccessTools.Method(inspectorBaseType, "CreateInspectorGUI"),
            nameof(VrcInspectorCreateGuiPostfix),
            "VRC.SDK3.Editor.VRCInspectorBase.CreateInspectorGUI");
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
            nameof(BuilderGuiPostfix),
            "VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.CreateContentInfoGUI");

        // AvatarビルドUIの構築完了後に翻訳
        // Build Type、Platform(s)、Build & Publishなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                avatarBuilderType,
                "CreateBuildGUI",
                new[] { typeof(VisualElement) }),
            nameof(BuilderGuiPostfix),
            "VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.CreateBuildGUI");

        Type worldBuilderType =
            AccessTools.TypeByName(
                "VRC.SDK3.Editor.VRCSdkControlPanelWorldBuilder");

        // World情報UIの構築完了後に翻訳
        // Name、Max. Capacity、World Debuggingなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                worldBuilderType,
                "CreateContentInfoGUI",
                new[] { typeof(VisualElement) }),
            nameof(BuilderGuiPostfix),
            "VRC.SDK3.Editor.VRCSdkControlPanelWorldBuilder.CreateContentInfoGUI");

        // WorldビルドUIの構築完了後に翻訳
        // Build Type、Platform(s)、Build & Testなどで使用
        PatchPostfix(
            harmony,
            AccessTools.Method(
                worldBuilderType,
                "CreateBuildGUI",
                new[] { typeof(VisualElement) }),
            nameof(BuilderGuiPostfix),
            "VRC.SDK3.Editor.VRCSdkControlPanelWorldBuilder.CreateBuildGUI");
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
                $"[VRChatSdkUiLocalizer] " +
                $"{targetName}が見つかりません。");

            return;
        }

        MethodInfo patch =
            AccessTools.Method(
                typeof(VRChatSdkUiLocalizer),
                patchMethodName);

        if (patch == null)
        {
            Debug.LogWarning(
                $"[VRChatSdkUiLocalizer] " +
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
                $"[VRChatSdkUiLocalizer] " +
                $"{targetName}が見つかりません。");

            return;
        }

        MethodInfo patch =
            AccessTools.Method(
                typeof(VRChatSdkUiLocalizer),
                patchMethodName);

        if (patch == null)
        {
            Debug.LogWarning(
                $"[VRChatSdkUiLocalizer] " +
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
                "[VRChatSdkUiLocalizer] " +
                "_builderPanelが見つかりません。");

            return;
        }

        VisualElement builderPanel =
            builderPanelField.GetValue(__instance)
                as VisualElement;

        if (builderPanel == null)
        {
            Debug.LogWarning(
                "[VRChatSdkUiLocalizer] " +
                "_builderPanelが取得できません。");

            return;
        }

        TranslateVisualElement(builderPanel, "VRCSdkControlPanel.ShowBuilders",
            TranslationCsvReader.TranslationTarget.SDK);
    }

    /// <summary>
    /// Validation UIを翻訳
    /// </summary>
    private static void CreateIssuesGuiPostfix(
        VisualElement __result)
    {
        TranslateVisualElement(__result, "VRCSdkControlPanel.CreateIssuesGUI",
            TranslationCsvReader.TranslationTarget.SDK);
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
                TranslationCsvReader.TranslationTarget.SDK,
                "StepFoldout.SetTitle");
    }

    /// <summary>
    /// Avatar / World BuilderのUIを翻訳
    /// </summary>
    private static void BuilderGuiPostfix(
        VisualElement root,
        MethodBase __originalMethod)
    {
        string hook =
            __originalMethod.DeclaringType.Name +
            "." + __originalMethod.Name;
        TranslateVisualElement(root, hook,
            TranslationCsvReader.TranslationTarget.SDK);
    }

    /// <summary>
    /// VisualElement配下の表示文字列を翻訳
    /// </summary>
    private static void TranslateVisualElement(
        VisualElement root,
        string hook,
        TranslationCsvReader.TranslationTarget target)
    {
        if (root == null)
        {
            return;
        }

        foreach (VisualElement element
                 in root.Query<VisualElement>().ToList())
        {
            // UI Toolkitの共通Tooltip
            element.tooltip = Translate(element.tooltip, target, hook);

            // PropertyFieldはバインド前でもlabelを保持する場合がある
            if (element is PropertyField propertyField)
            {
                propertyField.label =
                    Translate(propertyField.label, target, hook);
            }

            // Label、Button、Foldout、HelpBoxなどの表示文字列
            if (element is Label label)
            {
                label.text = Translate(label.text, target, hook);
            }
            else if (element is Button button)
            {
                button.text = Translate(button.text, target, hook);
            }
            else if (element is Foldout foldout)
            {
                foldout.text = Translate(foldout.text, target, hook);
            }
            else if (element is HelpBox helpBox)
            {
                helpBox.text = Translate(helpBox.text, target, hook);
            }

            // MaskField等、BaseField<T>にあるlabelも対象とする
            // UI Toolkitの型に限定して他の要素への副作用を避ける
            Type type = element.GetType();
            if (type.Namespace != null &&
                type.Namespace.StartsWith("UnityEngine.UIElements", StringComparison.Ordinal) &&
                !(element is PropertyField))
            {
                PropertyInfo labelProperty = type.GetProperty("label",
                    BindingFlags.Instance | BindingFlags.Public);
                if (labelProperty != null &&
                    labelProperty.PropertyType == typeof(string) &&
                    labelProperty.CanRead && labelProperty.CanWrite)
                {
                    string original = labelProperty.GetValue(element) as string;
                    string translated = Translate(original, target, hook);
                    if (translated != original)
                    {
                        labelProperty.SetValue(element, translated);
                    }
                }
            }
        }
    }

    /// <summary>
    /// VRChat UI Toolkit Inspectorの表示文字列を翻訳
    /// </summary>
    private static void VrcInspectorCreateGuiPostfix(
        VisualElement __result)
    {
        const string hook = "VRCInspectorBase.CreateInspectorGUI";
        var target = TranslationCsvReader.TranslationTarget.Inspector;

        TranslateVisualElement(__result, hook, target);

        if (__result == null)
        {
            return;
        }

        // PropertyField内部のラベルはパネルへの追加とバインド後に生成されるため、
        // パネル接続後にも一度翻訳する
        __result.RegisterCallback<AttachToPanelEvent>(evt =>
        {
            __result.schedule.Execute(() =>
                TranslateVisualElement(__result, hook, target)).ExecuteLater(100);
        });
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
                "[VRChatSdkUiLocalizer] " +
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
                "[VRChatSdkUiLocalizer] " +
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
        TranslationCsvReader.TranslationTarget target,
        string hook)
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
            text,
            hook);
    }

    /// <summary>
    /// GUIContentの表示文字列を翻訳
    /// </summary>
    private static void TranslateContent(
        GUIContent content,
        string hook)
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
                    TranslationCsvReader.TranslationTarget.Inspector,
                    hook);

            content.tooltip =
                Translate(
                    content.tooltip,
                    TranslationCsvReader.TranslationTarget.Inspector,
                    hook);
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
        TranslateContent(content, "GUIStyle.Draw");
    }

    /// <summary>
    /// PropertyFieldの表示文字列を翻訳
    /// </summary>
    private static void BeginPropertyPostfix(
        ref GUIContent __result)
    {
        TranslateContent(__result, "EditorGUI.BeginPropertyInternal");
    }

    /// <summary>
    /// Prefix Labelの表示文字列を翻訳
    /// </summary>
    private static void HandlePrefixLabelInternalPrefix(
        GUIContent label)
    {
        TranslateContent(label, "EditorGUI.HandlePrefixLabelInternal");
    }
}
