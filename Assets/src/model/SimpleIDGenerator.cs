using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleIDGenerator : IDGenInterface
{
    private int number = 0;

    public string Prefix { get; private set; }
    public string Suffix { get; private set; }

    public SimpleIDGenerator(string prefix, string suffix = "")
    {
        Prefix = prefix;
        Suffix = suffix;
    }
    public string Gen() => Prefix + number++ + Suffix;
    public void ReverseGen() => number --;

    public bool valid(string id)
        => id.StartsWith(Prefix) &&
           id.EndsWith(Suffix) &&
           int.TryParse(id.Substring(Prefix.Length, id.Length - Prefix.Length - Suffix.Length), out int result);

    private int Number(string id)
    {
        if (int.TryParse(id.Substring(Prefix.Length, id.Length - Prefix.Length - Suffix.Length), out int result))
            return result;
        else
            throw new ArgumentException("this is not a valid id string: " + id);
    }
    public int Compare(string id1, string id2) => Number(id1) - Number(id2);

    public void Reset() => number = 0;

    public void Reset(int next) => number = next;

    public void Reset(string next) => number = Number(next);
}
