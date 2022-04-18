using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDGenInterface
{
    string Gen();
    void Reset();
    void Reset(string last);
    void Reset(ICollection<string> allHistory);

    bool valid(string id);

    int Compare(string id1, string id2);

    IDGenInterface clone();
}
