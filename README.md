# VRChat SDK Inspector Localization

VRChat SDKのInspectorやSDK Control Panelに表示されるテキストを、Unity Editorの言語設定に応じて翻訳するUnity Editor拡張です。

Unity標準のローカライズでは翻訳されないVRChat SDK独自の表示を補完します。

## Features

- Unity Editorの言語設定に応じてVRChat SDKの表示を翻訳
- VRChat SDKのInspectorに対応
- VRChat SDK Control Panelに対応
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

## Translation CSV

翻訳データはCSV形式で記述します。

```csv
Type,Source,Translation
Exact,Gravity,重力
Partial,Review Any Alerts,警告を確認
Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
```

各列の意味は次のとおりです。

| 列 | 内容 |
| --- | --- |
| `Type` | 翻訳方法 |
| `Source` | VRChat SDKで表示される元のテキスト |
| `Translation` | 翻訳後のテキスト |

### Exact

`Source`と表示テキスト全体が完全に一致した場合に翻訳します。

```csv
Exact,Gravity,重力
Exact,Build,ビルド
```

固定されたラベルやメッセージなど、表示内容が変化しないテキストに使用します。

前後の空白や改行は翻訳処理時に保持されます。

### Partial

表示テキストに`Source`が含まれている場合、その部分だけを置き換えます。

```csv
Partial,Review Any Alerts,警告を確認
```

例えば、実際の表示が次の場合、

```text
Review Any Alerts (10)
```

翻訳後は次のようになります。

```text
警告を確認 (10)
```

数値など一部だけが変化するテキストにも利用できますが、文章中の可変部分を扱う場合は、可能であれば後述する`Format`の使用を推奨します。

`Partial`は部分一致したすべての箇所を置き換えるため、短すぎる単語や一般的な表現を指定すると、意図しない場所まで翻訳される可能性があります。

### Format

`{0}`、`{1}`などを可変部分として扱う翻訳方法です。

```csv
Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
```

例えば、

```text
Triangles: 35836 (Recommended: 32000)
```

と表示された場合、

```text
ポリゴン数: 35836（推奨: 32000）
```

へ翻訳されます。

複数の可変部分を使用できます。

```csv
Format,"Mesh Renderers: {0} (Maximum: {1}, Recommended: {2})","Mesh Renderer: {0}（最大: {1}、推奨: {2}）"
```

`Translation`側ではプレースホルダーの順序を変更することもできます。

```csv
Format,"{0} of {1}","全{1}件中{0}件"
```

そのため、英語と翻訳先の言語で語順が異なる場合にも対応できます。

### 翻訳の優先順位

翻訳は次の順序で試行されます。

1. `Exact`
2. `Format`
3. `Partial`

`Exact`または`Format`で一致した場合は、その翻訳結果が使用されます。

どちらにも一致しなかった場合、`Partial`による部分置換が行われます。

### 改行

CSV内では`\n`を使用して改行を記述できます。

```csv
Exact,"Are you sure?\nSome shaders might use these!","本当によろしいですか？\n一部のShaderでは使用されている可能性があります！"
```

`\n`はCSVの読み込み時に実際の改行へ変換されます。

これにより、CSVの1レコードを複数の物理行に分割せずに、改行を含むメッセージを記述できます。

### CSVの記述

カンマを含むフィールドはダブルクォートで囲みます。

```csv
Exact,"Hello, World","こんにちは、世界"
```

ダブルクォート自体を含める場合は、CSVの仕様に従って`""`と記述します。

```csv
Exact,"Click ""Build"" to continue.","「Build」をクリックして続行してください。"
```

UTF-8のBOMあり・BOMなしの両方に対応しています。

改行コードはLFとCRLFの両方に対応しています。

### コメント

行の先頭（空白を除く）が`#`の行はコメントとして扱われます。

```csv
# VRCPhysBone
# Reference source:
# Packages/com.vrchat.base/Editor/...

Type,Source,Translation
Exact,Gravity,重力
```

ダブルクォートで囲まれたフィールド内の`#`は通常の文字として扱われます。

### 翻訳ファイルの分割

同じ言語フォルダ内には複数のCSVファイルを配置できます。

```text
Translations/
└─ Japanese/
   ├─ VRCPhysBone.csv
   ├─ VRCContactReceiver.csv
   ├─ VRCAvatarDescriptor.csv
   └─ VRCSDKControlPanel.csv
```

ファイル名そのものは翻訳処理には使用されないため、コンポーネントや機能ごとに分割できます。

同じ`Source`が複数のCSVファイルに存在する場合は、先に読み込まれた翻訳が使用されます。

### 翻訳の再読み込み

CSVを編集した場合は、Unityのメニューから翻訳データを再読み込みできます。

```text
VRChat SDK
└─ Localization
   └─ Reload Translations
```

CSVを変更するたびにUnityを再起動する必要はありません。

表示済みのInspectorやSDK Control Panelについては、再表示や再構築が必要になる場合があります。

## Requirements

- Unity 2022.3
- VRChat SDK

本パッケージはVRChat SDKに含まれるHarmonyを利用します。

Harmonyによる実行時の処理を利用しているため、Unity本体やVRChat SDKのファイルを直接変更する必要はありません。

## License

MIT License
