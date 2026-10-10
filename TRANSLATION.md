# Translations

翻訳データはUnityの`SystemLanguage`名ごとのフォルダに配置します。

```text
Translations/
├─ Japanese/
│  ├─ VRCPhysBone.csv
│  ├─ VRCContactReceiver.csv
│  ├─ VRCAvatarDescriptor.csv
│  ├─ VRCSceneDescriptor.csv
│  ├─ VRCPipelineManager.csv
│  ├─ VRCUdonBehaviour.csv
│  ├─ VRCClientSim.csv
│  └─ VRCSDKControlPanel.csv
├─ Korean/
└─ ...
```

現在は日本語への翻訳を対象としています。

同じ言語フォルダには複数のCSVを配置でき、ファイル名は自由です。翻訳対象はファイル名ではなくCSVの`Target`で決まります。

## Translation CSV

CSVは次の形式で記述します。

```csv
Target,Type,Source,Translation
Inspector,Exact,Gravity,重力
SDK,Exact,Prepare Your Content,コンテンツの準備
SDK,Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
```

### Columns

| 列            | 内容             |
| ------------- | ---------------- |
| `Target`      | 翻訳対象のUI     |
| `Type`        | 翻訳方法         |
| `Source`      | 元のテキスト     |
| `Translation` | 翻訳後のテキスト |

### Target

| Target      | 対象                                                                     |
| ----------- | ------------------------------------------------------------------------ |
| `Inspector` | InspectorやIMGUI形式のEditorWindow、UI Toolkit形式のVRChat Inspectorなど |
| `SDK`       | SDK Control PanelなどのVRChat SDK UI                                     |

`Inspector`では、UnityのIMGUI描画処理を共通でフックしているため、VRChat以外のInspectorやEditorWindowにも翻訳が適用される場合があります。

また、VRChat SDKの`VRCInspectorBase`を使用したUI Toolkit形式のInspectorにも対応しています。

Targetごとに翻訳データを分けて処理するため、`SDK`用の翻訳が`Inspector`へ適用されることはありません。

### Type

| Type            | 動作                                      | 主な用途                         |
| --------------- | ----------------------------------------- | -------------------------------- |
| `Exact`         | テキスト全体が一致した場合に翻訳          | 固定ラベル・メッセージ           |
| `Format`        | `{0}`などを可変部分として全体一致         | 数値などを含むテキスト           |
| `PartialFormat` | `{0}`などを含むパターンを部分一致で置換   | 可変部分を含む長いテキストの一部 |
| `Partial`       | 一致した部分だけを置換                    | 固定文字列を含むテキスト         |
| `Debug`         | 一致した実際の表示テキストをConsoleへ出力 | 翻訳対象の調査                   |

#### Exact

```csv
Inspector,Exact,Gravity,重力
```

前後の空白や改行は保持されます。

#### Format

`{0}`、`{1}`などを可変部分として扱います。

```csv
SDK,Format,"Triangles: {0} (Recommended: {1})","ポリゴン数: {0}（推奨: {1}）"
```

```text
Triangles: 35836 (Recommended: 32000)
↓
ポリゴン数: 35836（推奨: 32000）
```

Translation側ではプレースホルダーの順序も変更できます。

```csv
SDK,Format,"{0} of {1}","全{1}件中{0}件"
```

#### PartialFormat

`Format`と同様に可変部分を扱いますが、テキスト全体ではなく一致した部分だけを置換します。

```csv
SDK,PartialFormat,"Overall Performance Estimate: {0} - ","総合パフォーマンス推定: {0} - "
```

`PartialFormat`では可変部分の範囲を特定できるよう、最後のプレースホルダーの後にも固定文字を含めることを推奨します。

#### Partial

```csv
SDK,Partial,Review Any Alerts,警告を確認
```

```text
Review Any Alerts (10)
↓
警告を確認 (10)
```

一致するすべての箇所を置換するため、短すぎる単語や一般的な表現の指定には注意してください。

#### Debug

実際にUnity上で使用されているテキストを調査できます。

通常形式：

```csv
,Debug,Mesh Renderers,
```

短縮形式：

```csv
D,,,Mesh Renderers
```

どちらも同じDebug条件として扱います。

`Source`を含む表示テキスト全体をUnity Consoleへ出力し、翻訳処理自体はそのまま継続します。

```text
[VRChatSdkUiLocalizer] Debug [Inspector → GUIStyle.Draw]: "Mesh Renderers: 17 (Maximum: 16, Recommended: 8)"
```

- `Target`は使用しないため空欄
- 通常形式では`Translation`を空欄にする
- 短縮形式では4列目に検索文字列を指定する
- 検索文字列は5文字以上必要
- 同じ言語・Target・フック元・原文の組み合わせは1回だけ出力
- 出力件数の上限は設けない
- Debug条件がない場合はDebug判定をスキップ
- 翻訳データの再読み込みで出力履歴をリセット

### Translation Priority

翻訳は次の順序で処理されます。

```text
Debug
  ↓
Exact
  ↓
Format
  ↓
PartialFormat
  ↓
Partial
```

`Debug`は翻訳処理の前に元の文字列を調査します。

`Exact`または`Format`で一致した場合はその結果を使用します。

`Format`と`PartialFormat`では、より具体的なパターンが優先されるよう、固定文字列部分の長いものから評価されます。

`PartialFormat`と`Partial`は順番に部分置換されます。

## CSV Rules

| 項目        | 仕様                   |
| ----------- | ---------------------- |
| 文字コード  | UTF-8（BOMあり・なし） |
| 改行コード  | LF / CRLF              |
| CSV内の改行 | `\n`                   |
| カンマ      | フィールドを`"`で囲む  |
| `"`         | `""`と記述             |
| コメント    | 空白を除く行頭が`#`    |
| `#`         | 引用符内では通常の文字 |

例：

```csv
# VRCPhysBone
Inspector,Exact,Gravity,重力
SDK,Exact,"Hello, World","こんにちは、世界"
SDK,Exact,"Click ""Build"" to continue.","「Build」をクリックして続行してください。"
SDK,Exact,"Are you sure?\nContinue?","本当によろしいですか？\n続行しますか？"
```

## Reload Translations

CSVを編集した場合、Unityを再起動せずに次のメニューから再読み込みできます。

```text
VRChat SDK
└─ Localization
   └─ Reload Translations
```

表示済みのInspectorやSDK Control Panelは、再表示が必要になる場合があります。

`Debug`の出力履歴も再読み込み時にリセットされます。
