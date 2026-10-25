using System;

namespace SmartClassroomAR.Navigation
{
    [Serializable]
    public enum EdgeType
    {
        Corridor,
        Staircase,
        Entrance,
        Door
    }

    [Serializable]
    public class NavEdge
    {
        public string edgeId;
        public string fromNodeId;
        public string toNodeId;
        public float cost;
        public bool isBidirectional = true;
        public EdgeType edgeType;

        public NavEdge()
        {
            edgeId = string.Empty;
            fromNodeId = string.Empty;
            toNodeId = string.Empty;
            cost = 1f;
            isBidirectional = true;
            edgeType = EdgeType.Corridor;
        }

        public NavEdge(string edgeId, string fromNodeId, string toNodeId, float cost, bool isBidirectional = true, EdgeType edgeType = EdgeType.Corridor)
        {
            this.edgeId = edgeId ?? string.Empty;
            this.fromNodeId = fromNodeId ?? string.Empty;
            this.toNodeId = toNodeId ?? string.Empty;
            this.cost = cost;
            this.isBidirectional = isBidirectional;
            this.edgeType = edgeType;
        }
    }
}
