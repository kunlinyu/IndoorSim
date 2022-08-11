using System.Collections.Generic;

public interface IDGenInterface
{
    string Gen();
    string Preview();
    void Reset();
    void ResetLast(string last);
    void ResetNext(string next);
    void Reset(ICollection<string> allHistory);

    bool valid(string id);

    int Compare(string id1, string id2);

    IDGenInterface clone();
}
