﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tommy
{
    public class TomlNode
    {
        private Dictionary<string, TomlNode> children;
        public string RawValue { get; set; }

        public bool IsTable { get; protected set; }
        public Dictionary<string, TomlNode> Children => children ?? (children = new Dictionary<string, TomlNode>());

        public TomlNode this[string key]
        {
            get => Children[key];
            set => Children[key] = value;
        }

        public static implicit operator TomlNode(string str) => new TomlNode
        {
            RawValue = str
        };
    }

    public class TomlTable : TomlNode
    {
        public TomlTable() => IsTable = true;
    }

    public static class TOML
    {
        private const char COMMENT_SYMBOL = '#';
        private const char KEY_VALUE_SEPARATOR = '=';
        private const char NEWLINE_CHARACTER = '\n';
        private const char NEWLINE_CARRIAGE_RETURN_CHARACTER = '\r';
        private const char SUBKEY_SEPARATOR = '.';
        private const char ESCAPE_SYMBOL = '\\';
        private const char BASIC_STRING_SYMBOL = '\"';

        private static bool IsQuoted(char c) => c == BASIC_STRING_SYMBOL || c == '\'';

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private static bool IsNewLine(char c) => c == NEWLINE_CHARACTER || c == NEWLINE_CARRIAGE_RETURN_CHARACTER;

        private static bool IsEmptySpace(char c) => IsWhiteSpace(c) || IsNewLine(c);

        private static bool IsBareKey(char c) => 'A' <= c && c <= 'Z' ||
                                                 'a' <= c && c <= 'z' ||
                                                 '0' <= c && c <= '9' ||
                                                 c == '_' ||
                                                 c == '-';

        private static bool IsTripleQuote(char quote, TextReader reader, out string excess)
        {
            var buffer = new char[2];
            var read = reader.ReadBlock(buffer, 0, 2);

            if (read == 2 && buffer[0] == quote && buffer[1] == quote)
            {
                excess = null;
                return true;
            }

            excess = new string(buffer);
            return false;
        }

        private static bool ProcessQuotedValueCharacter(char quote,
                                                        bool isBasic,
                                                        char c,
                                                        int next,
                                                        StringBuilder sb,
                                                        ref bool escaped)
        {
            if (escaped)
            {
                sb.Append(c);
                escaped = false;
                return false;
            }

            if (c == quote)
                return true;

            if (isBasic && c == ESCAPE_SYMBOL)
                if (next >= 0 && (char) next == quote)
                    escaped = true;

            if (c == NEWLINE_CHARACTER)
                throw new Exception("Encountered newline in single line quote");

            sb.Append(c);
            return false;
        }

        private static string ReadQuotedValueSingleLine(char quote, TextReader reader, string initialData)
        {
            var isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            var escaped = false;

            // Catch up with possible initial data
            for (var i = 0; i < initialData.Length; i++)
                if (ProcessQuotedValueCharacter(quote,
                                                isBasic,
                                                initialData[i],
                                                i < initialData.Length - 1 ? initialData[i + 1] : -1,
                                                sb,
                                                ref escaped))
                    return isBasic ? sb.ToString().Unescape() : sb.ToString();

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                var c = (char) cur;
                if (ProcessQuotedValueCharacter(quote, isBasic, c, reader.Peek(), sb, ref escaped))
                    break;
            }

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        private static string ReadQuotedValueMultiLine(char quote, TextReader reader)
        {
            var isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            var escaped = false;
            var skipWhitespace = false;
            var quotesEncountered = 0;
            var first = true;

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                var c = (char) cur;

                // Trim the first newline
                if (first && IsNewLine(c))
                {
                    if (c != NEWLINE_CARRIAGE_RETURN_CHARACTER)
                        first = false;
                    continue;
                }

                // Skip the current character if it is going to be escaped later
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                // If we are currently skipping empty spaces, skip
                if (skipWhitespace)
                {
                    if (IsEmptySpace(c))
                        continue;
                    skipWhitespace = false;
                }

                // If we encounter an escape sequence...
                if (c == ESCAPE_SYMBOL)
                {
                    var next = reader.Peek();
                    if (next >= 0)
                    {
                        // ...and the next char is empty space, we must skip all whitespaces
                        if (IsEmptySpace((char) next))
                        {
                            skipWhitespace = true;
                            continue;
                        }

                        // ...and we are in basic mode with \", skip the character
                        if (isBasic && (char) next == quote)
                            escaped = true;
                    }
                }

                // Count the consecutive quotes
                if (c == quote)
                    quotesEncountered++;
                else
                    quotesEncountered = 0;

                // If the are three quotes, count them as closing quotes
                if (quotesEncountered == 3)
                    break;

                sb.Append(c);
            }

            // Remove last two quotes (third one wasn't included by default
            sb.Length -= 2;

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        public static TomlNode Parse(TextReader reader)
        {
            var result = new TomlNode();

            var currentNode = result;

            var state = ParseState.None;

            var buffer = new StringBuilder();
            var key = string.Empty;

            int currentChar;
            while ((currentChar = reader.Read()) >= 0)
            {
                var c = (char) currentChar;

                if (state == ParseState.None)
                {
                    // Skip white space
                    if (IsWhiteSpace(c) || IsNewLine(c))
                        continue;

                    // Start of a comment; ignore until newline
                    if (c == COMMENT_SYMBOL)
                    {
                        reader.ReadLine();
                        continue;
                    }

                    //TODO: Tables

                    //TODO: Array tables

                    if (IsBareKey(c))
                    {
                        state = ParseState.Key;
                        buffer.Append(c);
                        continue;
                    }

                    throw new Exception($"Unexpected character \"{c}\"");
                }

                if (state == ParseState.Key)
                {
                    // TODO: Subkey

                    if (IsQuoted(c))
                    {
                        //TODO: Quoted key
                    }

                    if (c == SUBKEY_SEPARATOR)
                    {
                        //TODO: Separator
                    }

                    if (IsBareKey(c))
                    {
                        buffer.Append(c);
                        continue;
                    }

                    if (IsWhiteSpace(c))
                        continue;

                    if (c == KEY_VALUE_SEPARATOR)
                    {
                        state = ParseState.Value;

                        key = buffer.ToString();
                        buffer.Length = 0;

                        continue;
                    }

                    throw new Exception("Invalid character in key!");
                }

                if (state == ParseState.Value)
                {
                    if (IsWhiteSpace(c))
                        continue;

                    if (IsQuoted(c))
                    {
                        var value = IsTripleQuote(c, reader, out var excess)
                            ? ReadQuotedValueMultiLine(c, reader)
                            : ReadQuotedValueSingleLine(c, reader, excess);
                        currentNode[key] = new TomlNode
                        {
                            RawValue = value
                        };

                        key = string.Empty;
                    }

                    if (c == COMMENT_SYMBOL)
                        throw new Exception("The key has no value!");

                    state = ParseState.None;
                }
            }

            return result;
        }

        private enum ParseState
        {
            None,
            Key,
            Value
        }
    }

    internal static class ParseUtils
    {
        public static string Unescape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length);
            for (var i = 0; i < txt.Length;)
            {
                var num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                var c = txt[num + 1];
                switch (c)
                {
                    case 'b':
                        stringBuilder.Append('\b');
                        break;
                    case 't':
                        stringBuilder.Append('\t');
                        break;
                    case 'n':
                        stringBuilder.Append('\n');
                        break;
                    case 'f':
                        stringBuilder.Append('\f');
                        break;
                    case 'r':
                        stringBuilder.Append('\r');
                        break;
                    case '\'':
                        stringBuilder.Append('\'');
                        break;
                    case '\"':
                        stringBuilder.Append('\"');
                        break;
                    case '\\':
                        stringBuilder.Append('\\');
                        break;
                    default:
                        // TODO: Add Unicode codepoint support
                        throw new Exception("Undefined escape sequence!");
                        break;
                }

                i = num + 2;
            }

            return stringBuilder.ToString();
        }
    }
}