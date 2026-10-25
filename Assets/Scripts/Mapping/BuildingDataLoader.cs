using System;
using System.Collections.Generic;
using UnityEngine;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.Mapping
{
    /// <summary>
    /// Root data container representing building floors, destinations, navigation nodes, and edges.
    /// Serializable for Unity's JsonUtility.
    /// </summary>
    [Serializable]
    public class BuildingData
    {
        public List<FloorData> floors = new List<FloorData>();
        public List<DestinationData> destinations = new List<DestinationData>();
        public List<NavNode> nodes = new List<NavNode>();
        public List<NavEdge> edges = new List<NavEdge>();

        /// <summary>
        /// Converts the BuildingData into a populated NavigationGraph instance.
        /// </summary>
        public NavigationGraph ToNavigationGraph()
        {
            var graph = new NavigationGraph();

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null)
                    {
                        graph.AddNode(nodes[i]);
                    }
                }
            }

            if (edges != null)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    if (edges[i] != null)
                    {
                        graph.AddEdge(edges[i]);
                    }
                }
            }

            if (destinations != null)
            {
                graph.AddDestinations(destinations);
            }

            return graph;
        }
    }

    /// <summary>
    /// Utility class for loading, parsing, and validating building navigation data.
    /// Non-MonoBehaviour class compatible with Unity JsonUtility.
    /// </summary>
    public class BuildingDataLoader
    {
        /// <summary>
        /// Parses a JSON string into a BuildingData instance using Unity's JsonUtility.
        /// </summary>
        public BuildingData LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[BuildingDataLoader] Input JSON string is null or empty.");
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<BuildingData>(json);
                if (data == null)
                {
                    Debug.LogError("[BuildingDataLoader] JsonUtility returned null while deserializing BuildingData.");
                    return null;
                }

                // Ensure lists are never null
                if (data.floors == null) data.floors = new List<FloorData>();
                if (data.destinations == null) data.destinations = new List<DestinationData>();
                if (data.nodes == null) data.nodes = new List<NavNode>();
                if (data.edges == null) data.edges = new List<NavEdge>();

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildingDataLoader] Exception while deserializing JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates building data cross-references, floor mappings, and graph consistency.
        /// </summary>
        public List<string> ValidateData(BuildingData data)
        {
            var errors = new List<string>();

            if (data == null)
            {
                errors.Add("BuildingData is null.");
                return errors;
            }

            var floorIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodeIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var destinationIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var edgeIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Validate Floors
            if (data.floors == null || data.floors.Count == 0)
            {
                errors.Add("BuildingData contains no floors.");
            }
            else
            {
                for (int i = 0; i < data.floors.Count; i++)
                {
                    var floor = data.floors[i];
                    if (floor == null)
                    {
                        errors.Add($"Floor entry at index {i} is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(floor.floorId))
                    {
                        errors.Add($"Floor at index {i} has empty floorId.");
                    }
                    else if (!floorIdSet.Add(floor.floorId))
                    {
                        errors.Add($"Duplicate floorId found: '{floor.floorId}'.");
                    }
                }
            }

            // 2. Validate Nodes
            if (data.nodes == null || data.nodes.Count == 0)
            {
                errors.Add("BuildingData contains no navigation nodes.");
            }
            else
            {
                for (int i = 0; i < data.nodes.Count; i++)
                {
                    var node = data.nodes[i];
                    if (node == null)
                    {
                        errors.Add($"NavNode entry at index {i} is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(node.nodeId))
                    {
                        errors.Add($"NavNode at index {i} has empty nodeId.");
                    }
                    else if (!nodeIdSet.Add(node.nodeId))
                    {
                        errors.Add($"Duplicate nodeId found: '{node.nodeId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(node.floorId))
                    {
                        errors.Add($"NavNode '{node.nodeId}' has empty floorId.");
                    }
                    else if (!floorIdSet.Contains(node.floorId))
                    {
                        errors.Add($"NavNode '{node.nodeId}' references non-existent floorId: '{node.floorId}'.");
                    }
                }
            }

            // 3. Validate Destinations
            if (data.destinations != null)
            {
                for (int i = 0; i < data.destinations.Count; i++)
                {
                    var dest = data.destinations[i];
                    if (dest == null)
                    {
                        errors.Add($"Destination entry at index {i} is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dest.destinationId))
                    {
                        errors.Add($"Destination at index {i} has empty destinationId.");
                    }
                    else if (!destinationIdSet.Add(dest.destinationId))
                    {
                        errors.Add($"Duplicate destinationId found: '{dest.destinationId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(dest.floorId))
                    {
                        errors.Add($"Destination '{dest.destinationId}' has empty floorId.");
                    }
                    else if (!floorIdSet.Contains(dest.floorId))
                    {
                        errors.Add($"Destination '{dest.destinationId}' references non-existent floorId: '{dest.floorId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(dest.navigationNodeId))
                    {
                        errors.Add($"Destination '{dest.destinationId}' has empty navigationNodeId.");
                    }
                    else if (!nodeIdSet.Contains(dest.navigationNodeId))
                    {
                        errors.Add($"Destination '{dest.destinationId}' references non-existent navigationNodeId: '{dest.navigationNodeId}'.");
                    }
                }
            }

            // 4. Validate Edges
            if (data.edges == null || data.edges.Count == 0)
            {
                errors.Add("BuildingData contains no navigation edges.");
            }
            else
            {
                for (int i = 0; i < data.edges.Count; i++)
                {
                    var edge = data.edges[i];
                    if (edge == null)
                    {
                        errors.Add($"NavEdge entry at index {i} is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(edge.edgeId))
                    {
                        errors.Add($"NavEdge at index {i} has empty edgeId.");
                    }
                    else if (!edgeIdSet.Add(edge.edgeId))
                    {
                        errors.Add($"Duplicate edgeId found: '{edge.edgeId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(edge.fromNodeId) || !nodeIdSet.Contains(edge.fromNodeId))
                    {
                        errors.Add($"NavEdge '{edge.edgeId}' references invalid fromNodeId: '{edge.fromNodeId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(edge.toNodeId) || !nodeIdSet.Contains(edge.toNodeId))
                    {
                        errors.Add($"NavEdge '{edge.edgeId}' references invalid toNodeId: '{edge.toNodeId}'.");
                    }

                    if (edge.cost <= 0f)
                    {
                        errors.Add($"NavEdge '{edge.edgeId}' has non-positive cost: {edge.cost}.");
                    }
                }
            }

            // 5. Cross-validate graph reachability
            if (errors.Count == 0)
            {
                var graph = data.ToNavigationGraph();
                var graphErrors = graph.ValidateGraph();
                errors.AddRange(graphErrors);
            }

            return errors;
        }
    }
}
