﻿using System.Text;

namespace Parser.Data.TokenTypes;

public class TaggedValue : Token
{
    public Word Operator { get; }

    public Word Tag { get; }

    public List<Word> Value { get; }

    public TaggedValue(Word name, uint level, Word @operator, Word tag)
        : base(name, level)
    {
        Operator = @operator;
        Tag = tag;
        Value = new();
    }

    public void Append(Word value)
    {
        Value.Add(value);
    }

    public override string ToString()
    {
        return new StringBuilder()
            .Append(Tag)
            .Append('[')
            .AppendJoin(' ', Value)
            .Append(']')
            .ToString();
    }
}