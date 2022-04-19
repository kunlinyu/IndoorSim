using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;
using NetTopologySuite.Geometries;

public class ConsistencyTest
{
    private string extension = ".indoor.json";

    // public void GenericCase(string prefix, string caseName, bool fulltest)
    // {
    //     if (!caseName.StartsWith(prefix + "_"))
    //         throw new ArgumentException($"case name should starts with \"{prefix}\"");

    //     string filePath = $"Assets/src/Tests/{prefix}/" + caseName.Substring((prefix + "_").Length) + extension;
    //     string json = File.ReadAllText(filePath);

    //     IndoorTiling indoorTiling = IndoorTiling.Deserialize(json);

    //     IndoorTiling newIndoorTiling = new IndoorTiling();
    //     foreach (ReducedInstruction instruction in indoorTiling.history)
    //         newIndoorTiling.InterpretInstruction(instruction);

    //     string expectDigest = newIndoorTiling.CalcDigest(Digest.PolygonList(indoorTiling.Polygonizer().Select(geom => (Polygon)geom).ToList()));
    //     Debug.Log(expectDigest);
    //     Debug.Log("---");
    //     Debug.Log(indoorTiling.digestCache);
    //     if (fulltest)
    //     {
    //         Assert.AreEqual(expectDigest, indoorTiling.digestCache);      // old cache
    //         Assert.AreEqual(expectDigest, indoorTiling.CalcDigest());     // old calc
    //         Assert.AreEqual(expectDigest, newIndoorTiling.CalcDigest());  // new calc
    //     }
    //     else
    //     {
    //         Assert.AreEqual(expectDigest, newIndoorTiling.CalcDigest());  // new calc
    //     }
    // }

    // public void FullTest(string caseName)
    //     => GenericCase("full_test", caseName, true);

    // public void BadCase(string caseName)
    // => GenericCase("badcase", caseName, false);

    // [Test] public void badcase_split_hole_one_branch_remove() => BadCase(MethodBase.GetCurrentMethod().Name);
    // [Test] public void badcase_hole_branch_outside_split() => BadCase(MethodBase.GetCurrentMethod().Name);

    // [Test] public void full_test_segments() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_2_triangles() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_1_hole() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_cross() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_hole_connect_shell() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_hole_connect_shell2() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_2_in_1_hole() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_2_hole_split_remove() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_2_holes_split_boundary_make_3rd() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_1_hole_connect_1_hole_not_remove() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_branch() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_3_holes_remove_1() => FullTest(MethodBase.GetCurrentMethod().Name);
    // [Test] public void full_test_3_holes_remove_middle_tri() => FullTest(MethodBase.GetCurrentMethod().Name);


}
