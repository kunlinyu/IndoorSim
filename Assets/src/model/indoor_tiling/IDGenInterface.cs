using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDGenInterface
{
    string Prefix { get; }
    string Suffix { get; }

    string Gen();
    void ReverseGen();
    void Reset();
    void Reset(int next);
    void Reset(string next);

    bool valid(string id);

    int Compare(string id1, string id2);

    IDGenInterface clone();
}
