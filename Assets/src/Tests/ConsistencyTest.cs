using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ConsistencyTest
{

    [Test]
    public void Case1()
    {
        Debug.Log(Directory.GetCurrentDirectory());
        string json = File.ReadAllText("Assets/src/Tests/unnamed_map2.indoor.json");
        IndoorTiling indoorTiling = IndoorTiling.Deserialize(json, new SimpleIDGenerator("TVTX"), new SimpleIDGenerator("TBDR"), new SimpleIDGenerator("TSPC"));
        Debug.Log(indoorTiling.Digest());
    }
}
