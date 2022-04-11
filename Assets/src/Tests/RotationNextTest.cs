using System;
using System.Collections.Generic;

using NetTopologySuite.Geometries;

using NUnit.Framework;
using UnityEngine;

using JumpInfo = PSLGPolygonSearcher.JumpInfo;

class AdjacentFinder
{
    private List<CellBoundary> boundaries;
    public AdjacentFinder(List<CellBoundary> boundaries)
    {
        this.boundaries = boundaries;
    }

    public List<JumpInfo> Find(CellVertex cv)
    {
        var result = new List<JumpInfo>();
        foreach (CellBoundary b in boundaries)
            if (b.Contains(cv))
                result.Add(new JumpInfo() { target = b.Another(cv), through = b });
        return result;
    }

}

public class RotationNextTest
{
    private void AssertListCellVertex(List<JumpInfo> path, params CellVertex[] expect)
    {
        Debug.Log("Path");
        foreach (JumpInfo jump in path)
            Debug.Log(jump.target.Id);
        Debug.Log("Expect");
        foreach (CellVertex v in expect)
            Debug.Log(v.Id);
        Debug.Log("\\Expect");
        Assert.AreEqual(expect.Length, path.Count);
        for (int i = 0; i < path.Count; i++)
            Assert.True(System.Object.ReferenceEquals(expect[i], path[i].target));

    }

    [Test]
    public void SimpleRectangle()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void SimpleRectangleReverse()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv3, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv0, finder.Find);

        AssertListCellVertex(path, cv3, cv2, cv1, cv0);
    }

    [Test]
    public void RectangleWithExtraSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(2.0d, 0.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(1.0d, 1.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(0.0d, 1.0d), 4);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv1, cv3),
            new CellBoundary(cv3, cv4),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv4) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv4, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv3, cv4);
    }

    [Test]
    public void RectangleWithExtraInnerSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(0.5d, 0.5d), 2);
        CellVertex cv3 = new CellVertex(new Point(1.0d, 1.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(0.0d, 1.0d), 4);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv1, cv3),
            new CellBoundary(cv3, cv4),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv4) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv4, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv3, cv4);
    }

    [Test]
    public void SimpleRectangleMergeByTwoTriangle()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv1, cv3),
            new CellBoundary(cv2, cv3),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv3);
    }

    [Test]
    public void SnakeLeft()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(2.0d, 0.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(3.0d, 0.1d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void SnakeRight()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(2.0d, 0.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(3.0d, -0.1d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
        };
        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void TwoSeparateSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv2, cv3),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        Assert.IsEmpty(path);
    }

    [Test]
    public void TwoSeparateVertex()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        var finder = new AdjacentFinder(new List<CellBoundary>());
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv1) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv1, finder.Find);

        Assert.IsEmpty(path);
    }


    [Test]
    public void OneSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv1) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv1, finder.Find);

        AssertListCellVertex(path, cv0, cv1);
    }

    [Test]
    public void StartEqualsEnd()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
        };

        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv0, through = new CellBoundary(cv0, cv1) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv0, finder.Find);

        AssertListCellVertex(path, cv0);
    }

    [Test]
    public void TwoTriangle()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(-1.0d, 1.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(-1.0d, 0.0d), 4);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv0),
            new CellBoundary(cv0, cv3),
            new CellBoundary(cv3, cv4),
            new CellBoundary(cv4, cv0),
        };
        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv3, through = new CellBoundary(cv2, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv3, finder.Find);

        var result = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        Assert.AreEqual(2, result.Count);
        AssertListCellVertex(result[0], cv2, cv1, cv0);
        AssertListCellVertex(result[1], cv0, cv4, cv3);
    }

    [Test]
    public void TwoTriangleReverse()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(-1.0d, 1.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(-1.0d, 0.0d), 4);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv0),
            new CellBoundary(cv0, cv3),
            new CellBoundary(cv3, cv4),
            new CellBoundary(cv4, cv0),
        };
        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv2, through = new CellBoundary(cv2, cv3) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv2, finder.Find);

        var result = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        Assert.AreEqual(2, result.Count);
        AssertListCellVertex(result[0], cv4, cv3, cv0);
        AssertListCellVertex(result[1], cv1, cv0, cv2);
    }

    [Test]
    public void TwoSquareConnectOneBoundary()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(0.0d, 1.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(1.0d, 0.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(2.0d, 0.0d), 4);
        CellVertex cv5 = new CellVertex(new Point(2.0d, 1.0d), 5);
        CellVertex cv6 = new CellVertex(new Point(3.0d, 1.0d), 6);
        CellVertex cv7 = new CellVertex(new Point(3.0d, 0.0d), 7);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
            new CellBoundary(cv3, cv0),
            new CellBoundary(cv3, cv4),
            new CellBoundary(cv4, cv5),
            new CellBoundary(cv5, cv6),
            new CellBoundary(cv6, cv7),
            new CellBoundary(cv7, cv4),
        };
        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv5, through = new CellBoundary(cv2, cv5) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv5, finder.Find);

        AssertListCellVertex(path, cv5, cv6, cv7, cv4, cv3, cv0, cv1, cv2, cv3, cv4, cv5);

        var result = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedBoundary);

        Assert.AreEqual(2, result.Count);
        AssertListCellVertex(result[0], cv0, cv1, cv2, cv3);
        AssertListCellVertex(result[1], cv6, cv7, cv4, cv5);
    }

    [Test]
    public void TwoSquareConnectOneBoundaryReverse()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(0.0d, 1.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(1.0d, 0.0d), 3);
        CellVertex cv4 = new CellVertex(new Point(2.0d, 0.0d), 4);
        CellVertex cv5 = new CellVertex(new Point(2.0d, 1.0d), 5);
        CellVertex cv6 = new CellVertex(new Point(3.0d, 1.0d), 6);
        CellVertex cv7 = new CellVertex(new Point(3.0d, 0.0d), 7);
        List<CellBoundary> boundaries = new List<CellBoundary> {
            new CellBoundary(cv0, cv1),
            new CellBoundary(cv1, cv2),
            new CellBoundary(cv2, cv3),
            new CellBoundary(cv3, cv0),
            new CellBoundary(cv3, cv4),
            new CellBoundary(cv4, cv5),
            new CellBoundary(cv5, cv6),
            new CellBoundary(cv6, cv7),
            new CellBoundary(cv7, cv4),
        };
        var finder = new AdjacentFinder(boundaries);
        JumpInfo initJump = new JumpInfo() { target = cv2, through = new CellBoundary(cv2, cv5) };
        List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv2, finder.Find);

        AssertListCellVertex(path, cv2, cv3, cv4, cv5, cv6, cv7, cv4, cv3, cv0, cv1, cv2);

        var result = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedBoundary);

        Assert.AreEqual(2, result.Count);
        AssertListCellVertex(result[0], cv5, cv6, cv7, cv4);
        AssertListCellVertex(result[1], cv3, cv0, cv1, cv2);
    }

    // [Test]
    // public void SquareWithOneInnerBoundary()
    // {
    //     CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
    //     CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
    //     CellVertex cv2 = new CellVertex(new Point(-1.0d, 1.0d), 2);
    //     CellVertex cv3 = new CellVertex(new Point(-1.0d, -1.0d), 3);
    //     List<CellBoundary> boundaries = new List<CellBoundary> {
    //         new CellBoundary(cv0, cv1),
    //         new CellBoundary(cv1, cv2),
    //         new CellBoundary(cv2, cv3),
    //         new CellBoundary(cv3, cv1),
    //     };
    //     var finder = new AdjacentFinder(boundaries);
    //     JumpInfo initJump = new JumpInfo() { target = cv2, through = new CellBoundary(cv2, cv0) };
    //     List<JumpInfo> path = PSLGPolygonSearcher.Search(initJump, cv2, finder.Find);

    //     AssertListCellVertex(path, cv1, cv2, cv3, cv1);
    // }
}
