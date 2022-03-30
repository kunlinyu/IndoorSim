using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

public class PSLGPolygonSearcher
{
    public struct OutInfo
    {
        public CellVertex targetCellVertex;
        public CellBoundary boundary;
    }

    public static List<CellVertex> Search(CellVertex start, CellVertex end, Point startdir,
                                          Func<CellVertex, List<OutInfo>> adjacentFinder, out List<CellBoundary> boundaries)
    {
        boundaries = new List<CellBoundary>();
        if (System.Object.ReferenceEquals(start, end)) return new List<CellVertex>() { start };

        List<CellVertex> result = new List<CellVertex>();
        result.Add(start);

        CellVertex current = start;
        CellVertex last = null;

        do
        {
            List<OutInfo> outInfos = adjacentFinder(current);
            if (outInfos.Count == 0) return new List<CellVertex>();
            List<CellVertex> neighbors = outInfos.Select(oi => oi.targetCellVertex).ToList();
            List<CellBoundary> outBoundaries = outInfos.Select(oi => oi.boundary).ToList();
            List<Point> closestPoints = outBoundaries.Select(b => b.ClosestPointTo(current)).ToList();

            int startIndex = -1;
            if (last == null)
            {
                closestPoints.Add(startdir);
                startIndex = closestPoints.Count - 1;
            }
            else
            {
                for (int i = 0; i < outInfos.Count; i++)
                    if (System.Object.ReferenceEquals(outInfos[i].targetCellVertex, last))
                    {
                        startIndex = i;
                        break;
                    }
            }
            if (startIndex == -1) throw new Exception("Oops! startIndex == -1 ");

            Next(current.Geom, closestPoints, startIndex, out int CWNextIndex, out int CCWNextIndex);

            last = current;
            current = neighbors[CCWNextIndex];
            result.Add(current);
            boundaries.Add(outBoundaries[CCWNextIndex]);

            // come back to start, no path from start to end
            if (System.Object.ReferenceEquals(current, start)) return new List<CellVertex>();
        } while (!System.Object.ReferenceEquals(current, end));

        // Remove points between two same point (0 1 2 <3> 4 5 <3> 7 8 9 => 0 1 2 <3> 7 8 9)
        List<CellVertex> temp = new List<CellVertex>();
        for (int i = 0; i < result.Count; i++)
        {
            for (int j = 0; j < temp.Count; j++)
                if (System.Object.ReferenceEquals(temp[j], result[i]))
                {
                    temp.RemoveRange(j, temp.Count - j);
                    break;
                }
            temp.Add(result[i]);
        }
        result = temp;

        return result;

    }

    struct ThetaWithIndex
    {
        public double theta;
        public int index;
    }

    private static void Next(Point center, List<Point> neighbor, int startIndex, out int CWNextIndex, out int CCWNextIndex)
    {
        if (startIndex >= neighbor.Count)
            throw new ArgumentException($"startIndex({startIndex}) out of range(0-{neighbor.Count - 1})");

        List<ThetaWithIndex> thetaWithIndices = new List<ThetaWithIndex>();
        for (int i = 0; i < neighbor.Count; i++)
        {
            double x = neighbor[i].X - center.X;
            double y = neighbor[i].Y - center.Y;
            double theta = Math.Atan2(y, x);
            thetaWithIndices.Add(new ThetaWithIndex() { theta = theta, index = i });
        }
        thetaWithIndices.Sort((ti1, ti2) => ti1.theta.CompareTo(ti2.theta));

        for (int i = 0; i < thetaWithIndices.Count; i++)
        {
            if (thetaWithIndices[i].index == startIndex)
            {
                int next = (i + 1) % thetaWithIndices.Count;
                int prev = (i - 1) % thetaWithIndices.Count;
                if (prev < 0) prev += thetaWithIndices.Count;
                CWNextIndex = thetaWithIndices[next].index;
                CCWNextIndex = thetaWithIndices[prev].index;
                return;
            }
        }
        throw new Exception("should not get to here");
    }
}