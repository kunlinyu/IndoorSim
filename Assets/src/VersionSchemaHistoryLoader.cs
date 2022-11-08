using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VersionSchemaHistoryLoader
{
    static Dictionary<string, string> history = null;
    public static Dictionary<string, string> Load()
    {
        if (history != null) return history;

        TextAsset schemaHashHistoryAsset = Resources.Load<TextAsset>("schemaHashHistory");
        List<string> lines = new(schemaHashHistoryAsset.text.Split("\n"));
        List<string> validLines = lines.Where(line => line.Length != 0 && line.Count(c => c == ' ') == 1).ToList();

        history = new Dictionary<string, string>();
        validLines.ForEach(line => history.Add(line.Split(' ')[0], line.Split(' ')[1]));

        return history; 
    }


}
