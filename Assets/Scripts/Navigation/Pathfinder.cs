using System;
using System.Collections.Generic;

namespace SmartClassroomAR.Navigation
{
    /// <summary>
    /// Result structure for pathfinding queries.
    /// </summary>
    public class PathResult
    {
        public bool success;
        public List<string> nodeIds = new List<string>();
        public List<NavNode> nodes = new List<NavNode>();
        public float totalCost;
        public string errorMessage = string.Empty;
        public bool crossesFloors;
        public List<string> floorsTraversed = new List<string>();

        public static PathResult Failure(string message)
        {
            return new PathResult
            {
                success = false,
                errorMessage = message ?? "Pathfinding failed."
            };
        }
    }

    /// <summary>
    /// Implements Dijkstra's shortest path algorithm over a NavigationGraph.
    /// Pure C# class without Unity MonoBehaviour dependencies.
    /// </summary>
    public class Pathfinder
    {
        private readonly NavigationGraph _graph;

        public Pathfinder(NavigationGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        /// <summary>
        /// Finds the shortest path between startNodeId and endNodeId using Dijkstra's algorithm.
        /// </summary>
        public PathResult FindPath(string startNodeId, string endNodeId)
        {
            if (string.IsNullOrEmpty(startNodeId))
            {
                return PathResult.Failure("Start node ID is null or empty.");
            }

            if (string.IsNullOrEmpty(endNodeId))
            {
                return PathResult.Failure("End node ID is null or empty.");
            }

            var startNode = _graph.GetNode(startNodeId);
            if (startNode == null)
            {
                return PathResult.Failure($"Start node '{startNodeId}' does not exist in navigation graph.");
            }

            var endNode = _graph.GetNode(endNodeId);
            if (endNode == null)
            {
                return PathResult.Failure($"End node '{endNodeId}' does not exist in navigation graph.");
            }

            // Case: Start and destination are the exact same node
            if (string.Equals(startNodeId, endNodeId, StringComparison.OrdinalIgnoreCase))
            {
                var sameNodeResult = new PathResult
                {
                    success = true,
                    totalCost = 0f,
                    nodeIds = new List<string> { startNode.nodeId },
                    nodes = new List<NavNode> { startNode },
                    crossesFloors = false,
                    floorsTraversed = string.IsNullOrEmpty(startNode.floorId)
                        ? new List<string>()
                        : new List<string> { startNode.floorId },
                    errorMessage = string.Empty
                };
                return sameNodeResult;
            }

            // Dijkstra structures
            var distances = new Dictionary<string, float>();
            var previous = new Dictionary<string, string>();
            var priorityQueue = new MinHeap<string>();
            var visited = new HashSet<string>();

            distances[startNodeId] = 0f;
            priorityQueue.Enqueue(startNodeId, 0f);

            bool reached = false;

            while (priorityQueue.Count > 0)
            {
                var (currentNodeId, currentDist) = priorityQueue.Dequeue();

                if (visited.Contains(currentNodeId))
                {
                    continue;
                }
                visited.Add(currentNodeId);

                if (string.Equals(currentNodeId, endNodeId, StringComparison.OrdinalIgnoreCase))
                {
                    reached = true;
                    break;
                }

                var neighbors = _graph.GetNeighbors(currentNodeId);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    var (neighborId, edgeCost, edge) = neighbors[i];

                    if (string.IsNullOrEmpty(neighborId) || visited.Contains(neighborId))
                    {
                        continue;
                    }

                    if (edgeCost <= 0f)
                    {
                        edgeCost = 0.001f; // Prevent non-positive costs from disrupting Dijkstra
                    }

                    float newDist = currentDist + edgeCost;
                    if (!distances.TryGetValue(neighborId, out float oldDist) || newDist < oldDist)
                    {
                        distances[neighborId] = newDist;
                        previous[neighborId] = currentNodeId;
                        priorityQueue.Enqueue(neighborId, newDist);
                    }
                }
            }

            if (!reached || !distances.ContainsKey(endNodeId))
            {
                return PathResult.Failure($"No path found from '{startNodeId}' to '{endNodeId}'.");
            }

            // Reconstruct path
            var pathNodeIds = new List<string>();
            var curr = endNodeId;
            while (curr != null)
            {
                pathNodeIds.Add(curr);
                if (!previous.TryGetValue(curr, out curr))
                {
                    curr = null;
                }
            }
            pathNodeIds.Reverse();

            var pathNodes = new List<NavNode>();
            var floorsTraversed = new List<string>();
            string lastFloor = null;

            for (int i = 0; i < pathNodeIds.Count; i++)
            {
                var node = _graph.GetNode(pathNodeIds[i]);
                if (node != null)
                {
                    pathNodes.Add(node);
                    if (!string.IsNullOrEmpty(node.floorId) && !string.Equals(node.floorId, lastFloor, StringComparison.OrdinalIgnoreCase))
                    {
                        floorsTraversed.Add(node.floorId);
                        lastFloor = node.floorId;
                    }
                }
            }

            bool crossesFloors = false;
            if (floorsTraversed.Count > 1)
            {
                // Verify distinct floors
                var distinctFloors = new HashSet<string>(floorsTraversed, StringComparer.OrdinalIgnoreCase);
                crossesFloors = distinctFloors.Count > 1;
            }

            return new PathResult
            {
                success = true,
                nodeIds = pathNodeIds,
                nodes = pathNodes,
                totalCost = distances[endNodeId],
                crossesFloors = crossesFloors,
                floorsTraversed = floorsTraversed,
                errorMessage = string.Empty
            };
        }

        /// <summary>
        /// Simple binary min-heap for Dijkstra's priority queue.
        /// </summary>
        private class MinHeap<T>
        {
            private readonly List<(T item, float priority)> _elements = new List<(T, float)>();

            public int Count => _elements.Count;

            public void Enqueue(T item, float priority)
            {
                _elements.Add((item, priority));
                BubbleUp(_elements.Count - 1);
            }

            public (T item, float priority) Dequeue()
            {
                if (_elements.Count == 0)
                {
                    throw new InvalidOperationException("Priority queue is empty.");
                }

                var root = _elements[0];
                int lastIndex = _elements.Count - 1;
                _elements[0] = _elements[lastIndex];
                _elements.RemoveAt(lastIndex);

                if (_elements.Count > 0)
                {
                    BubbleDown(0);
                }

                return root;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (_elements[index].priority < _elements[parentIndex].priority)
                    {
                        var temp = _elements[index];
                        _elements[index] = _elements[parentIndex];
                        _elements[parentIndex] = temp;
                        index = parentIndex;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void BubbleDown(int index)
            {
                int count = _elements.Count;
                while (true)
                {
                    int smallest = index;
                    int left = 2 * index + 1;
                    int right = 2 * index + 2;

                    if (left < count && _elements[left].priority < _elements[smallest].priority)
                    {
                        smallest = left;
                    }

                    if (right < count && _elements[right].priority < _elements[smallest].priority)
                    {
                        smallest = right;
                    }

                    if (smallest != index)
                    {
                        var temp = _elements[index];
                        _elements[index] = _elements[smallest];
                        _elements[smallest] = temp;
                        index = smallest;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
