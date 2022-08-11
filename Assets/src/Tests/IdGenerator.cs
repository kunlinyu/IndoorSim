using System;
using System.Collections.Generic;

public class IdGenerator : IDGenInterface
{
    private int number = 0;

    public string Prefix { get; private set; }
    public string Suffix { get; private set; }
    public string Gen() => "" + number++;
    public string Preview() => throw new NotImplementedException();
    public int Compare(string id1, string id2) => throw new NotImplementedException();
    public void Reset() => throw new NotImplementedException();
    public void ResetNext(string next) => throw new NotImplementedException();
    public void ResetLast(string next) => throw new NotImplementedException();
    public void ReverseGen() => throw new NotImplementedException();
    public void Reset(ICollection<string> allHistory) => throw new NotImplementedException();
    public bool valid(string id) => throw new NotImplementedException();
    public IDGenInterface clone() => throw new NotImplementedException();


}