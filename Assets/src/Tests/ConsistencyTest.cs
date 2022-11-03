using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using NetTopologySuite.Geometries;

public class ConsistencyTest
{
    private string extension = ".indoor.json";

    public void GenericCase(string prefix, string caseName, bool fullTest)
    {
        if (!caseName.StartsWith(prefix + "_"))
            throw new ArgumentException($"case name should starts with \"{prefix}\"");

        string filePath = $"Assets/src/Tests/{prefix}/" + caseName.Substring((prefix + "_").Length) + extension;
        string json = File.ReadAllText(filePath);

        IndoorSimData offlineIndoorSimData = IndoorSimData.Deserialize(json);

        IndoorSimData newIndoorSimData = IndoorSimData.Deserialize(json, true);
        do { } while (newIndoorSimData.Redo());

        string expectDigest = newIndoorSimData.indoorFeatures.ActiveLayer.CalcDigest(Digest.PolygonList(offlineIndoorSimData.indoorFeatures.ActiveLayer.Polygonizer().Select(geom => (Polygon)geom).ToList()));
        Debug.Log("expect: " + expectDigest);
        Debug.Log("---");
        Debug.Log("offline digest cache: " + offlineIndoorSimData.digestCache);
        if (fullTest)
        {
            Assert.AreEqual(expectDigest, offlineIndoorSimData.digestCache);      // old cache
            Assert.AreEqual(expectDigest, offlineIndoorSimData.indoorFeatures.ActiveLayer.CalcDigest());     // old calc
            Assert.AreEqual(expectDigest, newIndoorSimData.indoorFeatures.ActiveLayer.CalcDigest());  // new calc
        }
        else
        {
            Assert.AreEqual(expectDigest, newIndoorSimData.indoorFeatures.ActiveLayer.CalcDigest());  // new calc
        }
    }

    public void FullTest(string caseName)
        => GenericCase("full_test", caseName, true);

    public void BadCase(string caseName)
    => GenericCase("badcase", caseName, false);


    [Test] public void full_test_C_hole_both_CCW() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_C_hole() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_circle() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_glasses() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_hole_split_boundary_and_branch() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_hole_split_two_remove_one_of_them() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_mani_cell_one_C_move() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_one_hole_cut() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_one_hole_in_circle() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_one_hole_split() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_split_two_boundaries() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_square() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_cell_in_out_middle() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_cell_in_to_out() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_cell_out_in_middle() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_cell_out_to_in() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_in_one_hole_remove_middle_boundary() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_three_in_one_hole_remove_middle_triangle_boundary() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_two_holes_cut() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_two_holes_in_circle() => FullTest(MethodBase.GetCurrentMethod().Name);
    [Test] public void full_test_two_holes_merge_to_one() => FullTest(MethodBase.GetCurrentMethod().Name);



}
