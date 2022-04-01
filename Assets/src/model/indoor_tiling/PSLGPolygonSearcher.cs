using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

#nullable enable

public class PSLGPolygonSearcher
{
    public struct JumpInfo
    {
        public CellVertex target;
        public CellBoundary through;

        public LineString Geom { get => through.GeomEndWith(target); }

        public bool ContentEqual(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            JumpInfo oi = (JumpInfo)obj;
            return System.Object.ReferenceEquals(oi.target, target) && System.Object.ReferenceEquals(oi.through, through);
        }
    }

    public static List<JumpInfo> Search(JumpInfo initJump,
                                        CellVertex target,
                                        Func<CellVertex, List<JumpInfo>> adjacentFinder)
    {
        List<JumpInfo> BBTs = new List<JumpInfo>() { initJump };
        if (System.Object.ReferenceEquals(initJump.target, target)) return BBTs;

        JumpInfo currentBBT = initJump;
        JumpInfo? lastBBT = null;

        int loopCount = 0;
        do
        {
            loopCount++;
            if (loopCount > 10000000) break;

            List<JumpInfo> jumpInfos = adjacentFinder(currentBBT.target);
            if (jumpInfos.Count == 0) return new List<JumpInfo>();

            List<CellBoundary> outBoundaries = jumpInfos.Select(oi => oi.through).ToList();
            List<Point> closestPoints = outBoundaries.Where(b => b != null).Select(b => b.ClosestPointTo(currentBBT.target)).ToList();

            int startIndex = -1;
            if (lastBBT == null)
            {
                closestPoints.Add(initJump.through.ClosestPointTo(initJump.target));
                startIndex = closestPoints.Count - 1;
            }
            else
            {
                for (int i = 0; i < jumpInfos.Count; i++)
                    if (System.Object.ReferenceEquals(jumpInfos[i].target, lastBBT.Value.target))
                    {
                        startIndex = i;
                        break;
                    }
            }
            if (startIndex == -1) throw new Exception("Oops! startIndex == -1 ");

            Next(currentBBT.target.Geom, closestPoints, startIndex, out int CWNextIndex, out int CCWNextIndex);

            lastBBT = currentBBT;
            currentBBT = jumpInfos[CCWNextIndex];

            // come back to start, no path from start to end
            if (BBTs.Any(bbt => bbt.ContentEqual(currentBBT)))
                return new List<JumpInfo>();

            BBTs.Add(currentBBT);

        } while (!System.Object.ReferenceEquals(currentBBT.target, target));

        List<JumpInfo> tempBBTs = new List<JumpInfo>();
        for (int i = 0; i < BBTs.Count; i++)
        {
            bool remove = false;
            for (int j = 0; j < tempBBTs.Count; j++)
                if (System.Object.ReferenceEquals(tempBBTs[j].target, BBTs[i].target))
                {
                    tempBBTs.RemoveRange(j + 1, tempBBTs.Count - j - 1);
                    remove = true;
                    break;
                }
            if (!remove)
                tempBBTs.Add(BBTs[i]);
        }
        BBTs = tempBBTs;
        return BBTs;
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