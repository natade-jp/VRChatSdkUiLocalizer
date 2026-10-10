# VRChat SDK UI Localizer

VRChat SDKのInspectorやSDK Control Panelなど、Unity Editor上に表示されるVRChat SDK独自のUIを、Unity Editorの言語設定に応じて翻訳するUnity Editor拡張です。

Unity標準のローカライズでは翻訳されないVRChat SDKのUIを補完します。

## Features

- Unity Editorの言語設定に応じてVRChat SDKのUIを翻訳
- Avatar・World関連のInspector / SDK Control Panelに対応
- Unity本体やVRChat SDKのファイルを変更せずに動作
- CSVによる翻訳データの管理
- UI・言語ごとに翻訳データを分離
- 複数言語へ拡張可能

## Translation Coverage

現在は**Avatar 3.0およびWorld関連のUIを中心に翻訳**しています。

Avatar関連では、VRC Phys BoneやVRC Avatar DescriptorなどのInspector、SDK Control PanelのAvatar Builderなどに対応しています。

World関連では、VRC Scene DescriptorやUdon BehaviourなどのInspector、SDK Control PanelのWorld Builder、ClientSim Settingsなどに対応しています。

ただし、すべてのUIを網羅しているわけではありません。一部のダイアログや入力欄、動的に生成される文字列などは、英語のまま表示される場合があります。

翻訳されていないUIや追加してほしい翻訳がある場合は、[GitHub Issues](https://github.com/natade-jp/VRChatSdkUiLocalizer/issues) または [X](https://x.com/natadea) からご連絡ください。

## Installation

あらかじめUnityプロジェクトへVRChat SDKをインストールしてください。

Unityの `Window > Package Manager` を開き、左上の `+` から `Add package from git URL...` を選択して、次のURLを入力します。

```text
https://github.com/natade-jp/VRChatSdkUiLocalizer.git
```

本ツールはVRChat SDKに含まれるHarmonyを利用するため、VRChat SDKがインストールされている必要があります。

アンインストールする場合は、Package Managerから `VRChat SDK UI Localizer` を削除してください。

## Usage

Unity Hubで対象Editorの日本語Language Packを追加し、Unity Editorの次の設定から使用する言語を選択します。

```text
Edit
└─ Preferences
   └─ Languages
      └─ Editor language
```

分からない場合は[Unity Editorを日本語化する手順](https://blog.natade.net/2026/10/03/unity-editor-japanese/)を確認してください。

`日本語` を選択すると、`Translations/Japanese/` の翻訳データが使用されます。英語の場合はVRChat SDK本来の表示になります。

言語変更や翻訳データの再読み込み後、表示中のInspectorやSDK Control Panelへすぐに反映されない場合は、対象の選択やウィンドウの表示をやり直してください。

## Translations

翻訳データはCSVファイルで管理しています。

現在は日本語に対応しており、翻訳データの追加・修正によって他の言語にも拡張できます。

翻訳CSVの作成方法や `Exact`、`Format`、`PartialFormat` などの仕様については、[TRANSLATION.md](TRANSLATION.md)を参照してください。

翻訳されていないUIや追加してほしい翻訳がある場合は、[GitHub Issues](https://github.com/natade-jp/VRChatSdkUiLocalizer/issues)からご連絡ください。

## Requirements

- Unity 2022.3
- VRChat SDK

Avatar用SDKやWorlds用SDKなど、個別パッケージのUIは、そのパッケージがインストールされている場合に翻訳対象となります。
