using System;
using System.Collections.Generic;
using UnityEngine;

public class IdGenerator : IDGenInterface
{
    private int number = 0;

    public string Prefix { get; private set; }
    public string Suffix { get; private set; }
    public string Gen() => "" + number++;

    public int Compare(string id1, string id2) => throw new NotImplementedException();
    public void Reset() => throw new NotImplementedException();
    public void Reset(string next) => throw new NotImplementedException();
    public void ReverseGen() => throw new NotImplementedException();
    public void Reset(ICollection<string> allHistory) => throw new NotImplementedException();
    public bool valid(string id) => throw new NotImplementedException();
    public IDGenInterface clone() => throw new NotImplementedException();
}