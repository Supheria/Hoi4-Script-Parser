﻿using Parser.data;

namespace Parser;

internal class Exceptions
{
    internal static string LogPath
    {
        get => _logPath;
        set
        {
            _logPath = value;
            using var file = new FileStream(value, FileMode.Create);
            using var logger = new StreamWriter(file);
            logger.Close();
        }
    }

    private static string _logPath = "";

    internal static void UnknownError(Element element)
    {
        Append($"unknown error at line({element.Line}), column({element.Column})");
    }
    internal static void UnexpectedKey(Element element)
    {
        Append($"unexpected key at line({element.Line}), column({element.Column})");
    }
    internal static void UnexpectedOperator(Element element)
    {
        Append($"unexpected operator at line({element.Line}), column({element.Column})");
    }
    internal static void UnexpectedValue(Element element)
    {
        Append($"unexpected value at line({element.Line}), column({element.Column})");
    }
    internal static void UnexpectedArrayType(Element element)
    {
        Append($"unexpected array type at line({element.Line}), column({element.Column})");
    }
    internal static void UnexpectedArraySyntax(Element element)
    {
        Append($"unexpected array syntax at line({element.Line}), column({element.Column})");
    }
    internal static void Exception(string message)
    {
        Append(message);
    }

    private static void Append(string message)
    {
        if (!File.Exists(LogPath))
            return;
        using var file = new FileStream(LogPath, FileMode.Append);
        using var logger = new StreamWriter(file);
        logger.WriteLine(message);
    }
}