# VRChat SDK Inspector Localization

VRChat SDKのInspectorに表示されるテキストを、Unity Editorの言語設定に応じて翻訳するUnity Editor拡張です。

Unity標準のローカライズでは翻訳されないVRChat SDK独自のInspector表示を補完します。

## Features

- Unity Editorの言語設定に応じてVRChat SDKのInspectorを翻訳
- Unity本体やVRChat SDKのファイルを変更せずに動作
- CSVファイルによる翻訳データの管理
- 言語ごとに翻訳データを分離
- 複数言語への拡張に対応

翻訳データは次のように言語ごとのフォルダへ配置します。

```text
Translations/
├─ Japanese/
│  ├─ VRCPhysBone.csv
│  ├─ VRCContactReceiver.csv
│  └─ ...
├─ Korean/
└─ ...
```

言語フォルダ名にはUnityの `SystemLanguage` の名前を使用します。

現在は日本語への翻訳を対象としています。

## Requirements

- Unity 2022.3
- VRChat SDK

本パッケージはVRChat SDKに含まれるHarmonyを利用します。

## License

MIT License
