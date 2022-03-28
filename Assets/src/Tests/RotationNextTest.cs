using System;
using System.Collections.Generic;

using NetTopologySuite.Geometries;

using NUnit.Framework;

class AdjacentFinder
{
    private List<CellBoundary> boundaries;
    public AdjacentFinder(List<CellBoundary> boundaries)
    {
        this.boundaries = boundaries;
    }

    public List<PSLGPolygonSearcher.OutInfo> Find(CellVertex cv)
    {
        var result = new List<PSLGPolygonSearcher.OutInfo>();
        foreach (CellBoundary b in boundaries)
            if (b.Contains(cv))
                result.Add(new PSLGPolygonSearcher.OutInfo() { targetCellVertex = b.Another(cv), boundary = b });
        return result;
    }

}

public class RotationNextTest
{
    private void AssertListCellVertex(List<CellVertex> path, params CellVertex[] expect)
    {
        Assert.AreEqual(expect.Length, path.Count);
        for (int i = 0; i < path.Count; i++)
            Assert.True(System.Object.ReferenceEquals(expect[i], path[i]));
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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv3, cv3.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv3, cv0, cv0.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv4, cv4.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv4, cv4.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv3, cv3.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv3, cv3.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv3, cv3.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv3, cv3.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

        Assert.IsEmpty(path);
    }

    [Test]
    public void TwoSeparateVertex()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        var finder = new AdjacentFinder(new List<CellBoundary>());
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv1, cv1.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv1, cv1.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

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
        List<CellVertex> path = PSLGPolygonSearcher.Search(cv0, cv0, cv0.Geom, cv => finder.Find(cv), out List<CellBoundary> outBoundaries);

        AssertListCellVertex(path, cv0);
    }
}
