using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// 翻訳CSVの読み込み
/// </summary>
internal static class TranslationCsvReader
{
    /// <summary>
    /// 翻訳種別
    /// </summary>
    internal enum TranslationType
    {
        Exact,
        Partial
    }

    /// <summary>
    /// 翻訳CSVの1レコード
    /// </summary>
    internal sealed class Translation
    {
        internal TranslationType Type { get; }
        internal string Source { get; }
        internal string TranslationText { get; }
        internal int Line { get; }

        /// <summary>
        /// 翻訳レコードを初期化
        /// </summary>
        internal Translation(
            TranslationType type,
            string source,
            string translationText,
            int line)
        {
            Type = type;
            Source = source;
            TranslationText = translationText;
            Line = line;
        }
    }

    /// <summary>
    /// 翻訳CSVを解析
    /// </summary>
    internal static IReadOnlyList<Translation> Read(
        TextReader reader,
        Action<string> warning = null)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        string text = Normalize(reader.ReadToEnd());

        using IEnumerator<CsvRecord> records =
            ReadRecords(text).GetEnumerator();

        if (!MoveNextRecord(records, warning))
        {
            warning?.Invoke(
                "CSVにヘッダーがありません。");

            return Array.Empty<Translation>();
        }

        CsvRecord header = records.Current;

        if (!header.Fields.SequenceEqual(
                new[] { "Type", "Source", "Translation" }))
        {
            warning?.Invoke(
                $"CSVの{header.Line}行目のヘッダーが不正です。" +
                "ヘッダーは Type,Source,Translation にしてください。");

            return Array.Empty<Translation>();
        }

        var result = new List<Translation>();

        while (MoveNextRecord(records, warning))
        {
            CsvRecord record = records.Current;

            if (record.Fields.Length != 3)
            {
                warning?.Invoke(
                    $"CSVの{record.Line}行目の列数が不正です。" +
                    "このレコードを無視します。");

                continue;
            }

            string typeText = record.Fields[0];
            string source = record.Fields[1];
            string translation = record.Fields[2];

            if (string.IsNullOrWhiteSpace(typeText) ||
                string.IsNullOrWhiteSpace(source) ||
                string.IsNullOrWhiteSpace(translation))
            {
                warning?.Invoke(
                    $"CSVの{record.Line}行目に空欄があります。" +
                    "このレコードを無視します。");

                continue;
            }

            if (!TryParseType(
                    typeText,
                    out TranslationType type))
            {
                warning?.Invoke(
                    $"CSVの{record.Line}行目のType " +
                    $"\"{typeText}\" は不正です。" +
                    "Exact または Partial を指定してください。" +
                    "このレコードを無視します。");

                continue;
            }

            result.Add(
                new Translation(
                    type,
                    source,
                    translation,
                    record.Line));
        }

        return result;
    }

    /// <summary>
    /// 次のCSVレコードを取得
    /// </summary>
    private static bool MoveNextRecord(
        IEnumerator<CsvRecord> records,
        Action<string> warning)
    {
        try
        {
            return records.MoveNext();
        }
        catch (InvalidDataException ex)
        {
            warning?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 翻訳種別を解析
    /// </summary>
    private static bool TryParseType(
        string value,
        out TranslationType type)
    {
        if (string.Equals(
                value,
                "Exact",
                StringComparison.Ordinal))
        {
            type = TranslationType.Exact;
            return true;
        }

        if (string.Equals(
                value,
                "Partial",
                StringComparison.Ordinal))
        {
            type = TranslationType.Partial;
            return true;
        }

        type = default;
        return false;
    }

    /// <summary>
    /// CSVレコードを解析
    /// </summary>
    private static IEnumerable<CsvRecord> ReadRecords(
        string text)
    {
        int position = 0;
        int line = 1;

        while (position < text.Length)
        {
            int first = position;

            while (first < text.Length &&
                   text[first] != '\n' &&
                   char.IsWhiteSpace(text[first]))
            {
                first++;
            }

            if (first >= text.Length)
            {
                yield break;
            }

            if (text[first] == '\n')
            {
                position = first + 1;
                line++;
                continue;
            }

            if (text[first] == '#')
            {
                int end = text.IndexOf('\n', first);

                if (end < 0)
                {
                    yield break;
                }

                position = end + 1;
                line++;
                continue;
            }

            int startLine = line;
            var fields = new List<string>();
            var field = new StringBuilder();

            bool quoted = false;
            bool closedQuote = false;

            while (position < text.Length)
            {
                char c = text[position++];

                if (quoted)
                {
                    if (c == '"')
                    {
                        if (position < text.Length &&
                            text[position] == '"')
                        {
                            field.Append('"');
                            position++;
                        }
                        else
                        {
                            quoted = false;
                            closedQuote = true;
                        }
                    }
                    else
                    {
                        field.Append(c);

                        if (c == '\n')
                        {
                            line++;
                        }
                    }

                    continue;
                }

                if (c == ',' || c == '\n')
                {
                    if (c == '\n')
                    {
                        line++;
                        break;
                    }

                    fields.Add(field.ToString());
                    field.Clear();
                    closedQuote = false;
                    continue;
                }

                if (closedQuote)
                {
                    if (c != ' ' && c != '\t')
                    {
                        throw new InvalidDataException(
                            $"CSVの{line}行目で閉じ引用符の後に" +
                            "不正な文字があります。");
                    }

                    continue;
                }

                if (c == '"')
                {
                    bool hasNonWhitespace =
                        field
                            .ToString()
                            .Any(character =>
                                character != ' ' &&
                                character != '\t');

                    if (hasNonWhitespace)
                    {
                        throw new InvalidDataException(
                            $"CSVの{line}行目で引用符の位置が不正です。");
                    }

                    field.Clear();
                    quoted = true;
                    continue;
                }

                field.Append(c);
            }

            if (quoted)
            {
                throw new InvalidDataException(
                    $"CSVの{startLine}行目から始まる" +
                    "引用符が閉じられていません。");
            }

            fields.Add(field.ToString());

            yield return new CsvRecord(
                startLine,
                fields.ToArray());
        }
    }

    /// <summary>
    /// CSV内の改行コードをLFへ統一
    /// </summary>
    private static string Normalize(string value)
    {
        return value
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimStart('\uFEFF');
    }

    /// <summary>
    /// CSV解析用レコード
    /// </summary>
    private sealed class CsvRecord
    {
        internal int Line { get; }
        internal string[] Fields { get; }

        /// <summary>
        /// CSV解析用レコードを初期化
        /// </summary>
        internal CsvRecord(
            int line,
            string[] fields)
        {
            Line = line;
            Fields = fields;
        }
    }
}