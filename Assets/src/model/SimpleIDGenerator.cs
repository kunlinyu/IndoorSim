using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleIDGenerator : IDGenInterface
{
    private int next = 0;

    public string Prefix { get; private set; }
    public string Suffix { get; private set; }

    public SimpleIDGenerator(string prefix, string suffix = "")
    {
        Prefix = prefix;
        Suffix = suffix;
    }

    public SimpleIDGenerator(SimpleIDGenerator another)
    {
        next = another.next;
        Prefix = "P" + another.Prefix;
        Suffix = another.Suffix;
    }

    public string Gen() => Prefix + next++ + Suffix;

    public string Preview() => Prefix + next + Suffix;

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

    public void Reset() => next = 0;

    public void ResetLast(string last) => next = Number(last) + 1;
    public void ResetNext(string next) => this.next = Number(next);

    public void Reset(ICollection<string> allHistory)
    {
        int maxLast = 0;
        foreach (string id in allHistory)
        {
            int last = Number(id);
            if (maxLast < last) maxLast = last;
        }
        next = maxLast + 1;
        Debug.Log("Reset: " + Prefix + " next: " + next);
    }

    public IDGenInterface clone() => new SimpleIDGenerator(this);
}
