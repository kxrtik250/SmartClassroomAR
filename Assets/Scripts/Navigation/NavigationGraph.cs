using System;
using System.Collections.Generic;
using System.Linq;
using SmartClassroomAR.Mapping;

namespace SmartClassroomAR.Navigation
{
    /// <summary>
    /// Represents the full navigation graph with nodes, edges, and adjacency queries.
    /// Non-MonoBehaviour class for runtime and unit test usage.
    /// </summary>
    public class NavigationGraph
    {
        private readonly Dictionary<string, NavNode> _nodes = new Dictionary<string, NavNode>();
        private readonly List<NavEdge> _edges = new List<NavEdge>();
        private readonly Dictionary<string, List<(string neighborId, float cost, NavEdge edge)>> _adjacency =
            new Dictionary<string, List<(string neighborId, float cost, NavEdge edge)>>();
        private readonly List<DestinationData> _destinations = new List<DestinationData>();
        private readonly List<string> _duplicateNodeIdLogs = new List<string>();

        public IReadOnlyDictionary<string, NavNode> Nodes => _nodes;
        public IReadOnlyList<NavEdge> Edges => _edges;
        public IReadOnlyList<DestinationData> Destinations => _destinations;

        public int NodeCount => _nodes.Count;
        public int EdgeCount => _edges.Count;

        public NavigationGraph()
        {
        }

        /// <summary>
        /// Registers a destination reference to validate against navigation nodes.
        /// </summary>
        public void AddDestination(DestinationData destination)
        {
            if (destination != null)
            {
                _destinations.Add(destination);
            }
        }

        /// <summary>
        /// Registers multiple destination references.
        /// </summary>
        public void AddDestinations(IEnumerable<DestinationData> destinations)
        {
            if (destinations == null) return;
            foreach (var dest in destinations)
            {
                if (dest != null)
                {
                    _destinations.Add(dest);
                }
            }
        }

        /// <summary>
        /// Adds a node to the navigation graph.
        /// </summary>
        public bool AddNode(NavNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId))
            {
                return false;
            }

            if (_nodes.ContainsKey(node.nodeId))
            {
                _duplicateNodeIdLogs.Add(node.nodeId);
                return false;
            }

            _nodes[node.nodeId] = node;
            if (!_adjacency.ContainsKey(node.nodeId))
            {
                _adjacency[node.nodeId] = new List<(string neighborId, float cost, NavEdge edge)>();
            }

            return true;
        }

        /// <summary>
        /// Adds an edge to the navigation graph and updates adjacency index.
        /// </summary>
        public void AddEdge(NavEdge edge)
        {
            if (edge == null) return;
            _edges.Add(edge);

            if (!string.IsNullOrEmpty(edge.fromNodeId) && !string.IsNullOrEmpty(edge.toNodeId))
            {
                if (!_adjacency.ContainsKey(edge.fromNodeId))
                {
                    _adjacency[edge.fromNodeId] = new List<(string neighborId, float cost, NavEdge edge)>();
                }
                _adjacency[edge.fromNodeId].Add((edge.toNodeId, edge.cost, edge));

                if (edge.isBidirectional)
                {
                    if (!_adjacency.ContainsKey(edge.toNodeId))
                    {
                        _adjacency[edge.toNodeId] = new List<(string neighborId, float cost, NavEdge edge)>();
                    }
                    _adjacency[edge.toNodeId].Add((edge.fromNodeId, edge.cost, edge));
                }
            }
        }

        /// <summary>
        /// Gets a node by its unique identifier. Returns null if not found.
        /// </summary>
        public NavNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            return _nodes.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// Returns all outgoing neighbors and associated edge info for a given node.
        /// </summary>
        public List<(string neighborId, float cost, NavEdge edge)> GetNeighbors(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return new List<(string neighborId, float cost, NavEdge edge)>();
            }

            if (_adjacency.TryGetValue(nodeId, out var neighbors))
            {
                return new List<(string neighborId, float cost, NavEdge edge)>(neighbors);
            }

            return new List<(string neighborId, float cost, NavEdge edge)>();
        }

        /// <summary>
        /// Retrieves all nodes associated with a specific floor.
        /// </summary>
        public List<NavNode> GetNodesOnFloor(string floorId)
        {
            var result = new List<NavNode>();
            if (string.IsNullOrEmpty(floorId)) return result;

            foreach (var kvp in _nodes)
            {
                if (string.Equals(kvp.Value.floorId, floorId, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Validates graph integrity and returns a list of error descriptions.
        /// </summary>
        public List<string> ValidateGraph()
        {
            var errors = new List<string>();

            // 1. Check for duplicate node IDs encountered during population
            foreach (var duplicateId in _duplicateNodeIdLogs)
            {
                errors.Add($"Duplicate node ID detected: '{duplicateId}'.");
            }

            // 2. Check for edges referencing non-existent nodes and zero/negative costs
            for (int i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (edge == null)
                {
                    errors.Add($"Edge at index {i} is null.");
                    continue;
                }

                if (string.IsNullOrEmpty(edge.fromNodeId) || !_nodes.ContainsKey(edge.fromNodeId))
                {
                    errors.Add($"Edge '{edge.edgeId}' references non-existent fromNodeId: '{edge.fromNodeId}'.");
                }

                if (string.IsNullOrEmpty(edge.toNodeId) || !_nodes.ContainsKey(edge.toNodeId))
                {
                    errors.Add($"Edge '{edge.edgeId}' references non-existent toNodeId: '{edge.toNodeId}'.");
                }

                if (edge.cost <= 0f)
                {
                    errors.Add($"Edge '{edge.edgeId}' has invalid non-positive cost: {edge.cost}.");
                }
            }

            // 3. Check for destinations referencing invalid navigation nodes
            foreach (var dest in _destinations)
            {
                if (dest == null) continue;

                if (string.IsNullOrEmpty(dest.navigationNodeId))
                {
                    errors.Add($"Destination '{dest.destinationId}' ({dest.destinationName}) has an empty navigationNodeId.");
                }
                else if (!_nodes.ContainsKey(dest.navigationNodeId))
                {
                    errors.Add($"Destination '{dest.destinationId}' ({dest.destinationName}) references non-existent navigationNodeId: '{dest.navigationNodeId}'.");
                }
            }

            // 4. Check for unreachable nodes (disconnected components)
            if (_nodes.Count > 1)
            {
                // Build an undirected graph view to check general reachability
                var undirectedAdjacency = new Dictionary<string, HashSet<string>>();
                foreach (var id in _nodes.Keys)
                {
                    undirectedAdjacency[id] = new HashSet<string>();
                }

                foreach (var edge in _edges)
                {
                    if (edge != null && _nodes.ContainsKey(edge.fromNodeId) && _nodes.ContainsKey(edge.toNodeId))
                    {
                        undirectedAdjacency[edge.fromNodeId].Add(edge.toNodeId);
                        undirectedAdjacency[edge.toNodeId].Add(edge.fromNodeId);
                    }
                }

                var visited = new HashSet<string>();
                var components = new List<List<string>>();

                foreach (var nodeId in _nodes.Keys)
                {
                    if (!visited.Contains(nodeId))
                    {
                        var component = new List<string>();
                        var queue = new Queue<string>();
                        queue.Enqueue(nodeId);
                        visited.Add(nodeId);

                        while (queue.Count > 0)
                        {
                            var current = queue.Dequeue();
                            component.Add(current);

                            foreach (var neighbor in undirectedAdjacency[current])
                            {
                                if (!visited.Contains(neighbor))
                                {
                                    visited.Add(neighbor);
                                    queue.Enqueue(neighbor);
                                }
                            }
                        }

                        components.Add(component);
                    }
                }

                if (components.Count > 1)
                {
                    // Find largest component as primary component
                    var largest = components.OrderByDescending(c => c.Count).First();
                    foreach (var comp in components)
                    {
                        if (comp != largest)
                        {
                            var nodeSamples = string.Join(", ", comp.Take(5));
                            if (comp.Count > 5) nodeSamples += "...";
                            errors.Add($"Disconnected component found with {comp.Count} unreachable node(s): [{nodeSamples}].");
                        }
                    }
                }
            }

            return errors;
        }
    }
}
