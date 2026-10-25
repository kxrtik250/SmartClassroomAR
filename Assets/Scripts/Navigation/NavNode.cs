using System;
using UnityEngine;

namespace SmartClassroomAR.Navigation
{
    [Serializable]
    public enum NodeType
    {
        Waypoint,
        Destination,
        Staircase,
        Entrance,
        Corridor
    }

    [Serializable]
    public class NavNode
    {
        public string nodeId;
        public string floorId;
        public float x;
        public float y;
        public float z;
        public string label;
        public NodeType nodeType;

        public Vector3 Position => new Vector3(x, y, z);

        public NavNode()
        {
            nodeId = string.Empty;
            floorId = string.Empty;
            x = 0f;
            y = 0f;
            z = 0f;
            label = string.Empty;
            nodeType = NodeType.Waypoint;
        }

        public NavNode(string nodeId, string floorId, float x, float y, float z, string label, NodeType nodeType)
        {
            this.nodeId = nodeId ?? string.Empty;
            this.floorId = floorId ?? string.Empty;
            this.x = x;
            this.y = y;
            this.z = z;
            this.label = label ?? string.Empty;
            this.nodeType = nodeType;
        }
    }
}
