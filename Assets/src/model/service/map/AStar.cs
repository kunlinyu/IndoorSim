/*
 *  <p>
 *  Syrius Robotics Ltd. Co. CONFIDENTIAL
 *  <p>
 *  Author: Kunlin Yu <yukunlin@syriusrobotics.com>
 *  Create Date: 2021-4-14
 *  <p>
 *  Unpublished Copyright (c) 2018 - 2021 [Syrius Robotics Ltd. Co.],
 *  All Rights Reserved.
 *  <p>
 *  NOTICE: All information contained herein is, and remains the property of
 *  Syrius Robotics Ltd. Co. The intellectual and technical concepts contained
 *  herein are proprietary to Syrius Robotics Ltd. Co. and may be covered by
 *  U.S., P.R.China and Foreign Patents, patents in process, and are protected
 *  by trade secret or copyright law.  Dissemination of this information or
 *  reproduction of this material is strictly forbidden unless prior written
 *  permission is obtained from Syrius Robotics Ltd. Co..  Access to the source
 *  code contained herein is hereby forbidden to anyone except current Syrius
 *  Robotics Ltd. Co. employees, managers or contractors who have executed
 *  Confidentiality and Non-disclosure agreements explicitly covering such
 *  access.
 *  <p>
 *  The copyright notice above does not evidence any actual or intended
 *  publication or disclosure of this source code, which includes information
 *  that is confidential and/or proprietary, and is a trade secret, of Syrius
 *  Robotics Ltd. Co..  ANY REPRODUCTION, MODIFICATION, DISTRIBUTION, PUBLIC
 *  PERFORMANCE, OR PUBLIC DISPLAY OF OR THROUGH USE OF THIS SOURCE CODE WITHOUT
 *  THE EXPRESS WRITTEN CONSENT OF COMPANY IS STRICTLY PROHIBITED, AND IN
 *  VIOLATION OF APPLICABLE LAWS AND INTERNATIONAL TREATIES.  THE RECEIPT OR
 *  POSSESSION OF THIS SOURCE CODE AND/OR RELATED INFORMATION DOES NOT CONVEY OR
 *  IMPLY ANY RIGHTS TO REPRODUCE, DISCLOSE OR DISTRIBUTE ITS CONTENTS, OR TO
 *  MANUFACTURE, USE, OR SELL ANYTHING THAT IT MAY DESCRIBE, IN WHOLE OR IN
 *  PART.
 */

using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

public class NodeWithCost<NodeType>
{
    public NodeType node { set; get; }
    public double cost { set; get; }

    public NodeWithCost(NodeType node, double cost)
    {
        this.node = node;
        this.cost = cost;
    }
}

public interface AdjacentFinder<NodeType>
{
    abstract List<NodeWithCost<NodeType>> adjacentWithCost(NodeType data, NodeType? predecessor);
}

public interface NodeBreaker<NodeType>
{
    abstract bool shouldBreak(NodeType currentNode, double cost, int exploredCount);
}


public class AStar<NodeType, AdjacentNodeFinder, Breaker> where AdjacentNodeFinder : AdjacentFinder<NodeType> where Breaker : NodeBreaker<NodeType>
{
    public static List<NodeType> search(NodeType initNode,
                                        AdjacentNodeFinder adjacentFinder,
                                        Action<NodeType> consumer, Breaker breaker)
        => search(initNode, adjacentFinder, consumer, breaker, node => 0);

    public static List<NodeType> search(NodeType initNode,
                                        AdjacentNodeFinder adjacentFinder,
                                        Action<NodeType> consumer, Breaker breaker,
                                        Func<NodeType, double> heuristic)
        => searchMultiInit(new List<NodeType>() { initNode }, adjacentFinder, consumer, breaker, heuristic);
    public static List<NodeType> searchMultiInit(List<NodeType> initNodes,
                                                 AdjacentNodeFinder adjacentFinder,
                                                 Action<NodeType> consumer,
                                                 Breaker breaker)
        => searchMultiInit(initNodes, adjacentFinder, consumer, breaker, node => 0);

    static public List<NodeType> searchMultiInit(List<NodeType> initNodes,
                                                 AdjacentNodeFinder adjacentFinder,
                                                 Action<NodeType> consumer,
                                                 Breaker breaker,
                                                 Func<NodeType, Double> heuristic)
    {
        Dictionary<NodeType, Double> nodeCostMap = new Dictionary<NodeType, Double>();
        Dictionary<NodeType, NodeType> parentMap = new Dictionary<NodeType, NodeType>();

        // NodeCostComparator cmp = new NodeCostComparator(nodeCostMap, heuristic);
        // Queue<NodeType> nodeQueue = new PriorityQueue<NodeType, NodeCostComparator>(cmp);
        HashSet<NodeType> nodeQueue = new HashSet<NodeType>();

        HashSet<NodeType> exploredNodes = new HashSet<NodeType>(initNodes);

        foreach (NodeType node in initNodes)
        {
            nodeCostMap.Add(node, 0.0);  // TODO(future feature): add parameter about initCost
            nodeQueue.Add(node);
        }

        int index = 0;
        while (nodeQueue.Count != 0)
        {
            if (index++ > 1000) break;

            // pop
            // NodeType node = nodeQueue.Dequeue();
            NodeType node = nodeQueue.FirstOrDefault();
            double minCost = Double.MaxValue;
            foreach (NodeType n in nodeQueue)
            {
                double cost = nodeCostMap[n] + heuristic.Invoke(n);
                if (minCost > cost)
                {
                    minCost = cost;
                    node = n;
                }
            }
            nodeQueue.Remove(node);
            exploredNodes.Add(node);

            // consume
            consumer?.Invoke(node);

            // break
            if (breaker.shouldBreak(node, nodeCostMap[node], exploredNodes.Count))
            {
                // backtrack
                List<NodeType> path = new List<NodeType>() { node };
                NodeType currentNode = node;
                while (!initNodes.Contains(currentNode))
                {
                    currentNode = parentMap[currentNode];
                    path.Add(currentNode);
                }
                path.Reverse();

                return path;
            }

            // explore
            NodeType? predecessor = parentMap.ContainsKey(node) ? parentMap[node] : default(NodeType);
            foreach (NodeWithCost<NodeType> adj in adjacentFinder.adjacentWithCost(node, predecessor))
            {
                // update cost map
                if (!nodeCostMap.ContainsKey(adj.node))
                {
                    nodeCostMap.Add(adj.node, nodeCostMap[node] + adj.cost);
                    parentMap.Add(adj.node, node);
                }
                else if (nodeCostMap[node] + adj.cost < nodeCostMap[adj.node])
                {
                    nodeCostMap[adj.node] = nodeCostMap[node] + adj.cost;
                    parentMap[adj.node] = node;
                }

                // push into queue
                if (!exploredNodes.Contains(adj.node) && !nodeQueue.Contains(adj.node))
                {
                    nodeQueue.Add(adj.node);
                }
            }
        }
        return new List<NodeType>();
    }
}
