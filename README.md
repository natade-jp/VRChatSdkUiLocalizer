# VRChat SDK UI Localizer

VRChat SDKのInspectorやSDK Control Panelなど、Unity Editor上に表示されるVRChat SDK独自のUIを、Unity Editorの言語設定に応じて翻訳するUnity Editor拡張です。

Unity標準のローカライズでは翻訳されないVRChat SDK独自の表示を補完します。

## Features

- Unity Editorの言語設定に応じてVRChat SDKのUIを翻訳
- VRChat SDKのInspectorに対応
- VRChat SDK Control Panelに対応
- Unity本体やVRChat SDKのファイルを変更せずに動作
- CSVファイルによる翻訳データの管理
- Inspector用とSDK UI用の翻訳データを分離
- 言語ごとに翻訳データを分離
- 複数言語への拡張に対応

## Usage

### インストール

あらかじめUnityプロジェクトへVRChat SDKをインストールしてください。

その後、本パッケージをUnityプロジェクトへインストールします。

本ツールはVRChat SDKに含まれるHarmonyを利用するため、VRChat SDKがインストールされている必要があります。

Avatar用SDKなどの個別パッケージに含まれる型を直接依存関係として参照せず、利用可能なUIに対して翻訳処理を適用します。

### Unity Editorの言語設定

翻訳に使用する言語は、Unity Editorの言語設定に連動します。

Unity Editorの言語を変更するには、次の設定を開きます。

```text
Edit
└─ Preferences
   └─ Languages
```

`Editor language`から使用する言語を選択します。

例えば`日本語`を選択すると、`Translations/Japanese/`に配置された翻訳データが使用されます。

Unity Editorを英語に戻した場合は、VRChat SDKの元の英語表示が使用されます。

### 翻訳の確認

言語を変更した後、VRChat SDKのコンポーネントをInspectorで表示するか、VRChat SDK Control Panelを開いて翻訳を確認します。

すでに表示されているInspectorやSDK Control Panelには変更がすぐに反映されない場合があります。その場合は、対象を選択し直すかSDK Control Panelを開き直してください。

## Translations

翻訳データは次のように言語ごとのフォルダへ配置します。

```text
Translations/
├─ Japanese/
│  ├─ VRCPhysBone.csv
│  ├─ VRCContactReceiver.csv
│  ├─ VRCAvatarDescriptor.csv
│  └─ VRCSDKControlPanel.csv
├─ Korean/
└─ ...
```

言語フォルダ名にはUnityの`SystemLanguage`の名前を使用します。

現在は日本語への翻訳を対象としています。

## Translation CSV

翻訳データはCSV形式で記述します。

```csv
Target,Type,Source,Translation
Inspector,Exact,Gravity,重力
SDK,Exact,Prepare Your Content,コンテンツの準備
SDK,Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
```

各列の意味は次のとおりです。

| 列            | 内容                               |
| ------------- | ---------------------------------- |
| `Target`      | 翻訳対象                           |
| `Type`        | 翻訳方法                           |
| `Source`      | VRChat SDKで表示される元のテキスト |
| `Translation` | 翻訳後のテキスト                   |

### Target

`Target`では翻訳を適用するUIの種類を指定します。

現在は次の2種類に対応しています。

| Target      | 対象                                                          |
| ----------- | ------------------------------------------------------------- |
| `Inspector` | VRChat SDKのInspectorなど、UnityのIMGUIを使用して表示されるUI |
| `SDK`       | SDK Control Panelなど、VRChat SDK側で構築されるUI             |

例えば、Inspectorの`Gravity`を翻訳する場合は次のように記述します。

```csv
Inspector,Exact,Gravity,重力
```

SDK Control Panelの表示を翻訳する場合は`SDK`を指定します。

```csv
SDK,Exact,Prepare Your Content,コンテンツの準備
```

翻訳データはTargetごとに分けて処理されるため、`SDK`用の翻訳がInspector側へ適用されることはありません。

### Exact

`Source`と表示テキスト全体が完全に一致した場合に翻訳します。

```csv
Inspector,Exact,Gravity,重力
SDK,Exact,Build,ビルド
```

固定されたラベルやメッセージなど、表示内容が変化しないテキストに使用します。

前後の空白や改行は翻訳処理時に保持されます。

### Partial

表示テキストに`Source`が含まれている場合、その部分だけを置き換えます。

```csv
SDK,Partial,Review Any Alerts,警告を確認
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
SDK,Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
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
SDK,Format,"Mesh Renderers: {0} (Maximum: {1}, Recommended: {2})","Mesh Renderer: {0}（最大: {1}、推奨: {2}）"
```

`Translation`側ではプレースホルダーの順序を変更することもできます。

```csv
SDK,Format,"{0} of {1}","全{1}件中{0}件"
```

そのため、英語と翻訳先の言語で語順が異なる場合にも対応できます。

### Debug

`Debug`は、実際にUnity上で使用されているテキストを確認するための調査用Typeです。

`Source`に指定した文字列が表示テキストに含まれている場合、その表示テキスト全体をUnity Consoleへ出力します。

```csv
SDK,Debug,Mesh Renderers,
```

例えば、実際の表示テキストが次の場合、

```text
Mesh Renderers: 17 (Maximum: 16, Recommended: 8)
```

`Source`の`Mesh Renderers`が含まれているため、このテキスト全体がConsoleへ出力されます。

`Debug`では翻訳は行われません。`Translation`は使用しないため空欄にできます。

また、Debugによる検出後も通常の翻訳処理は継続されます。

同一の表示テキストは繰り返し描画されても1回だけConsoleへ出力されます。翻訳データを再読み込みすると、この出力履歴もリセットされます。

誤って大量のテキストへ一致することを防ぐため、`Debug`の`Source`には5文字以上を指定する必要があります。

### 翻訳の優先順位

表示テキストに対して、まず`Debug`による検出が行われます。

その後、翻訳は次の順序で試行されます。

1. `Exact`
2. `Format`
3. `Partial`

`Exact`または`Format`で一致した場合は、その翻訳結果が使用されます。

どちらにも一致しなかった場合、`Partial`による部分置換が行われます。

`Debug`は翻訳方法ではなく調査用の機能であるため、通常の翻訳処理を妨げません。

### 改行

CSV内では`\n`を使用して改行を記述できます。

```csv
SDK,Exact,"Are you sure?\nSome shaders might use these!","本当によろしいですか？\n一部のShaderでは使用されている可能性があります！"
```

`\n`はCSVの読み込み時に実際の改行へ変換されます。

これにより、CSVの1レコードを複数の物理行に分割せずに、改行を含むメッセージを記述できます。

### CSVの記述

カンマを含むフィールドはダブルクォートで囲みます。

```csv
SDK,Exact,"Hello, World","こんにちは、世界"
```

ダブルクォート自体を含める場合は、CSVの仕様に従って`""`と記述します。

```csv
SDK,Exact,"Click ""Build"" to continue.","「Build」をクリックして続行してください。"
```

UTF-8のBOMあり・BOMなしの両方に対応しています。

改行コードはLFとCRLFの両方に対応しています。

### コメント

行の先頭（空白を除く）が`#`の行はコメントとして扱われます。

```csv
# VRCPhysBone
# Reference source:
# Packages/com.vrchat.base/Editor/...

Target,Type,Source,Translation
Inspector,Exact,Gravity,重力
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

ファイル名そのものは翻訳処理には使用されないため、コンポーネントや機能ごとに自由に分割できます。

翻訳対象はファイル名ではなく、各レコードの`Target`によって決まります。

同じ`Target`および`Type`で同じ`Source`が複数のCSVファイルに存在する場合は、先に読み込まれた翻訳が使用されます。

### 翻訳の再読み込み

CSVを編集した場合は、Unityのメニューから翻訳データを再読み込みできます。

```text
VRChat SDK
└─ Localization
   └─ Reload Translations
```

CSVを変更するたびにUnityを再起動する必要はありません。

表示済みのInspectorやSDK Control Panelについては、再表示や再構築が必要になる場合があります。

`Debug`で一度Consoleへ出力されたテキストの記録も、翻訳データの再読み込み時にリセットされます。

## Requirements

- Unity 2022.3
- VRChat SDK

本パッケージはVRChat SDKに含まれるHarmonyを利用します。

Harmonyによる実行時の処理を利用しているため、Unity本体やVRChat SDKのファイルを直接変更する必要はありません。

Avatar用SDKなどの個別パッケージに含まれるUIについては、そのパッケージがインストールされている場合に翻訳対象となります。

## License

MIT License
