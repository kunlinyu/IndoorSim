using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

#nullable enable

public enum SplitRingType
{
    SplitByRepeatedVertex,
    SplitByRepeatedBoundary,
}

public class PSLGPolygonSearcher
{
    public struct JumpInfo
    {
        public CellVertex target;
        public CellBoundary through;

        public LineString Geom { get => through.GeomEndWith(target); }

        public CellVertex Another() => through.Another(target);

        public bool ContentEqual(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            JumpInfo oi = (JumpInfo)obj;
            return System.Object.ReferenceEquals(oi.target, target) && System.Object.ReferenceEquals(oi.through, through);
        }

        public bool ReverseEqual(JumpInfo jump)
        {
            return System.Object.ReferenceEquals(jump.through, through) && System.Object.ReferenceEquals(jump.Another(), target);
        }
    }

    public static List<List<JumpInfo>> Jumps2Rings(List<JumpInfo> jumps, SplitRingType splitRingType)
    {
        bool outsideInit = System.Object.ReferenceEquals(jumps.First().target, jumps.Last().target);

        List<List<JumpInfo>> result = new List<List<JumpInfo>>();
        List<JumpInfo> stack = new List<JumpInfo>();

        foreach (JumpInfo jump in jumps)
        {
            bool newRing = false;
            if (splitRingType == SplitRingType.SplitByRepeatedVertex)
            {
                for (int i = 0; i < stack.Count; i++)
                    if (System.Object.ReferenceEquals(stack[i].target, jump.target))
                    {
                        List<JumpInfo> ring = new List<JumpInfo>();
                        for (int j = i + 1; j < stack.Count; j++)
                            ring.Add(stack[j]);
                        stack.RemoveRange(i + 1, stack.Count - (i + 1));
                        ring.Add(jump);
                        if (ring.Count > 2)
                            result.Add(ring);
                        newRing = true;
                        break;
                    }
                if (!newRing)
                    stack.Add(jump);
            }
            else if (splitRingType == SplitRingType.SplitByRepeatedBoundary)
            {
                for (int i = 0; i < stack.Count; i++)
                    if (stack[i].ReverseEqual(jump))
                    {
                        List<JumpInfo> ring = new List<JumpInfo>();
                        for (int j = i + 1; j < stack.Count; j++)
                            ring.Add(stack[j]);
                        stack.RemoveRange(i + 0, stack.Count - (i + 0));
                        // ring.Add(jump);
                        if (ring.Count > 2)
                            result.Add(ring);
                        newRing = true;
                        break;
                    }
                if (!newRing)
                    stack.Add(jump);
            }
            else
            {
                throw new ArgumentException("unknown split ring type");
            }
        }


        if (outsideInit)
            stack.RemoveAt(0);

        if (stack.Count > 2)
            result.Add(stack);

        return result;
    }

    public static List<JumpInfo> Search(JumpInfo initJump,
                                        CellVertex target,
                                        Func<CellVertex, List<JumpInfo>> adjacentFinder,
                                        bool ccw = true,
                                        bool forceComebackDirection = false
                                        )
    {
        Stack<JumpInfo> jumps = new Stack<JumpInfo>();
        jumps.Push(initJump);
        List<JumpInfo> jumpsHistory = new List<JumpInfo>(jumps);

        JumpInfo currentJump = initJump;
        JumpInfo? lastJump = null;

        CellBoundary? combackBoundary = null;

        if (forceComebackDirection)
        {
            List<JumpInfo> jumpInfos = adjacentFinder(currentJump.target);
            if (jumpInfos.Count == 0) return new List<JumpInfo>();

            List<CellBoundary> outBoundaries = jumpInfos.Select(ji => ji.through).ToList();
            List<Point> closestPoints = outBoundaries.Where(b => b != null).Select(b => b.ClosestPointTo(currentJump.target)).ToList();

            closestPoints.Add(initJump.through.ClosestPointTo(initJump.target));
            int startIndex = closestPoints.Count - 1;

            Next(currentJump.target.Geom, closestPoints, startIndex, out int CWNextIndex, out int CCWNextIndex);

            combackBoundary = ccw ? jumpInfos[CWNextIndex].through : jumpInfos[CCWNextIndex].through;
        }

        int loopCount = 0;
        bool finish;
        do
        {
            loopCount++;
            if (loopCount > 100000) throw new Exception("dead loop");

            List<JumpInfo> jumpInfos = adjacentFinder(currentJump.target);
            if (jumpInfos.Count == 0) return new List<JumpInfo>();

            List<CellBoundary> outBoundaries = jumpInfos.Select(oi => oi.through).ToList();
            List<Point> closestPoints = outBoundaries.Where(b => b != null).Select(b => b.ClosestPointTo(currentJump.target)).ToList();

            int startIndex = -1;
            if (lastJump == null)
            {
                closestPoints.Add(initJump.through.ClosestPointTo(initJump.target));
                startIndex = closestPoints.Count - 1;
            }
            else
            {
                for (int i = 0; i < jumpInfos.Count; i++)
                    if (System.Object.ReferenceEquals(jumpInfos[i].target, lastJump.Value.target))
                    {
                        startIndex = i;
                        break;
                    }
            }
            if (startIndex == -1) throw new Exception("Oops! startIndex == -1 ");

            Next(currentJump.target.Geom, closestPoints, startIndex, out int CWNextIndex, out int CCWNextIndex);

            lastJump = currentJump;
            currentJump = ccw ? jumpInfos[CCWNextIndex] : jumpInfos[CWNextIndex];

            // come back to start, no path from start to end
            if (jumpsHistory.Any(jump => jump.ContentEqual(currentJump)))
                return new List<JumpInfo>();

            if (jumps.Count > 0 && jumps.Peek().ReverseEqual(currentJump))
            {
                jumps.Pop();
            }
            else
            {
                jumps.Push(currentJump);
                jumpsHistory.Add(currentJump);
            }

            if (forceComebackDirection)
            {
                finish = System.Object.ReferenceEquals(currentJump.target, target)  &&
                         System.Object.ReferenceEquals(currentJump.through, combackBoundary);
            }
            else
            {
                finish = System.Object.ReferenceEquals(currentJump.target, target);
            }
        } while (!finish);

        var result = jumps.ToList();
        result.Reverse();

        // while (result.First().ReverseEqual(result.Last()))
        // {
        //     result.RemoveAt(0);
        //     result.RemoveAt(result.Count - 1);
        // }

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