using System;
using System.Collections.Generic;

using NetTopologySuite.Geometries;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv3, cv3.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void SimpleRectangleReverse()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);

        List<CellVertex> path = RotateNext.SearchPolygon(cv3, cv0, cv0.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

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

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv4, cv4.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv4, closestPoint = cv4.Geom });
                    break;
                case 4:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
            }
            return result;
        });

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

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv4, cv4.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv4, closestPoint = cv4.Geom });
                    break;
                case 4:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1, cv3, cv4);
    }

    [Test]
    public void SimpleRectangleMergeByTwoTriangle()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv3, cv3.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1, cv3);
    }

    [Test]
    public void SnakeLeft()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(2.0d, 0.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(3.0d, 0.1d), 3);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv3, cv3.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void SnakeRight()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(2.0d, 0.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(3.0d, -0.1d), 3);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv3, cv3.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1, cv2, cv3);
    }

    [Test]
    public void TwoSeparateSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        CellVertex cv2 = new CellVertex(new Point(1.0d, 1.0d), 2);
        CellVertex cv3 = new CellVertex(new Point(0.0d, 1.0d), 3);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv3, cv3.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    break;
                case 2:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv3, closestPoint = cv3.Geom });
                    break;
                case 3:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv2, closestPoint = cv2.Geom });
                    break;
            }
            return result;
        });

        Assert.IsEmpty(path);
    }

    [Test]
    public void TwoSeparateVertex()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);
        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv1, cv1.Geom, cv => new List<RotateNext.OutInfo>());

        Assert.IsEmpty(path);
    }


    [Test]
    public void OneSegment()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv1, cv1.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0, cv1);
    }

    [Test]
    public void StartEqualsEnd()
    {
        CellVertex cv0 = new CellVertex(new Point(0.0d, 0.0d), 0);
        CellVertex cv1 = new CellVertex(new Point(1.0d, 0.0d), 1);

        List<CellVertex> path = RotateNext.SearchPolygon(cv0, cv0, cv0.Geom, (cv) =>
        {
            var result = new List<RotateNext.OutInfo>();
            switch (cv.Id)
            {
                case 0:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv1, closestPoint = cv1.Geom });
                    break;
                case 1:
                    result.Add(new RotateNext.OutInfo() { targetCellVertex = cv0, closestPoint = cv0.Geom });
                    break;
            }
            return result;
        });

        AssertListCellVertex(path, cv0);
    }
}
