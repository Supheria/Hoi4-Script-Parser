﻿using System.Diagnostics.CodeAnalysis;
using Parser.data;
using System.Text;

namespace Parser;

internal class Tokenizer
{
    private static readonly List<char> Delimiter = new() { '\t', ' ', '\n', '\r', '#', '=', '>', '<', '}', '{', '"', '\0' };
    private static readonly List<char> Blank = new() { '\t', ' ', '\n', '\r', '\0' };
    private static readonly List<char> EndLine = new() { '\n', '\r', '\0' };
    private static readonly List<char> Marker = new() { '=', '>', '<', '}', '{' };
    private const char Note = '#';
    private const char Quote = '"';
    private const char Escape = '\\';
    private readonly Exceptions _exceptions;

    private enum States
    {
        None,
        Quotation,
        Escape,
        Word,
        Note
    }

    private States State { get; set; } = States.None;
    private byte[] Buffer { get; set; } = Array.Empty<byte>();
    private uint BufferPosition { get; set; } = 0;
    private uint Line { get; set; } = 1;
    private uint Column { get; set; } = 0;
    private ParseTree Tree { get; set; }
    private Element Composed { get; set; } = new();
    private StringBuilder Composing { get; } = new();
    private List<Token> Tokens { get; } = new();

    public Tokenizer(Exceptions exceptions)
    {
        _exceptions = exceptions;
        Tree = new ParseTree(_exceptions);
    }

    private List<Token> Parse(string filePath)
    {
        ReadBuffer(filePath);
        Tree = new ParseTree(_exceptions);
        while (BufferPosition < Buffer?.Length)
        {
            if (!Compose((char)Buffer[BufferPosition]))
                continue;
            var tree = Tree.Parse(Composed);
            if (tree is null)
            {
                CacheList();
                Tree = new ParseTree(_exceptions);
            }
            else { Tree = tree; }
        }

        EndCheck();
        return Tokens;
    }

    public static List<Token> Tokenize(string filePath, Exceptions exceptions)
    {
        return new Tokenizer(exceptions).Parse(filePath);
    }

    private void ReadBuffer(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _exceptions.Exception($"could not open file: {filePath}");
            return;
        }
        using var file = File.OpenRead(filePath);
        if (file.ReadByte() == 0xEF && file.ReadByte() == 0xBB && file.ReadByte() == 0xBF)
        {
            Buffer = new byte[file.Length - 3];
            _ = file.Read(Buffer, 0, Buffer.Length);
        }
        else
        {
            file.Seek(0, SeekOrigin.Begin);
            Buffer = new byte[file.Length];
            _ = file.Read(Buffer, 0, Buffer.Length);
        }
    }

    private bool Compose(char ch)
    {
        if (!Composed.Submitted) { return true; }
        switch (State)
        {
            case States.Quotation:
                if (ch == Escape)
                {
                    Composing.Append(GetChar());
                    State = States.Escape;
                    return false;
                }
                else if (ch == Quote)
                {
                    Composing.Append(GetChar());
                    Composed = new(Composing.ToString(), Line, Column);
                    State = States.None;
                    return true;
                }
                else if (EndLine.Contains(ch))
                {
                    Composing.Append(Quote);
                    Composed = new(Composing.ToString(), Line, Column);
                    State = States.None;
                    return true;
                }
                Composing.Append(GetChar());
                return false;
            case States.Escape:
                if (EndLine.Contains(ch))
                {
                    Composing.Append(Quote);
                    Composing.Append(Quote);
                    Composed = new(Composing.ToString(), Line, Column);
                    State = States.None;
                    return true;
                }
                else
                {
                    Composing.Append(GetChar());
                    State = States.Quotation;
                    return false;
                }
            case States.Word:
                if (Delimiter.Contains(ch))
                {
                    Composed = new(Composing.ToString(), Line, Column);
                    State = States.None;
                    return true;
                }
                Composing.Append(GetChar());
                return false;
            case States.Note:
                if (EndLine.Contains(ch))
                {
                    State = States.None;
                }
                GetChar();
                return false;
            default:
                if (ch == Quote)
                {
                    Composing.Clear();
                    Composing.Append(GetChar());
                    State = States.Quotation;
                }
                else if (ch == Note)
                {
                    State = States.Note;
                    GetChar();
                }
                else if (Marker.Contains(ch))
                {
                    Composed = new(GetChar().ToString(), Line, Column);
                    return true;
                }
                else if (Blank.Contains(ch))
                {
                    if (ch == '\n')
                    {
                        Line++;
                        Column = 0;
                    }
                    GetChar();
                }
                else
                {
                    Composing.Clear();
                    Composing.Append(GetChar());
                    State = States.Word;
                }
                return false;
        }
    }
    private void CacheList()
    {
        var token = Tree.OnceGet();
        if (token is NullToken)
            return;
        Tokens.Add(token);
    }

    private char GetChar()
    {
        var ch = (char)Buffer[BufferPosition++];
        if (ch == '\t')
            Column += 4;
        else
            Column++;
        return ch;
    }

    private void EndCheck()
    {
        if (Tree.From is not null)
        {
            _exceptions.Exception($"interruption at line({Line}), column({Column})");
            Tree.Done();
            Tree = Tree.From;
            while (Tree.From is not null)
            {
                Tree.Done();
                Tree = Tree.From;
            }
        }
        CacheList();
    }
}