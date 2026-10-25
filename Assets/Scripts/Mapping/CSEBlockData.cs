using System.Collections.Generic;
using UnityEngine;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.Mapping
{
    /// <summary>
    /// PLACEHOLDER DATA - Replace with actual CSE block measurements
    /// Provides default pre-configured map and navigation data for the CSE Block at Sathyabama Institute.
    /// Building footprint is approximately 50m x 15m across 3 floors.
    /// </summary>
    public static class CSEBlockData
    {
        private const string DepartmentName = "Computer Science and Engineering";

        /// <summary>
        /// Generates and returns the default JSON representation of the CSE Block building data.
        /// Compatible with Unity JsonUtility.
        /// </summary>
        public static string GetDefaultBuildingDataJson()
        {
            var data = CreateDefaultBuildingData();
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Builds the typed BuildingData structure for CSE Block.
        /// </summary>
        public static BuildingData CreateDefaultBuildingData()
        {
            var data = new BuildingData();

            // -------------------------------------------------------------
            // Floors Definition
            // -------------------------------------------------------------
            data.floors.Add(new FloorData("F0", "Ground Floor", 0, "CSE Block Ground Floor - Admin & Labs"));
            data.floors.Add(new FloorData("F1", "First Floor", 1, "CSE Block First Floor - Classrooms & Faculty"));
            data.floors.Add(new FloorData("F2", "Second Floor", 2, "CSE Block Second Floor - Advanced Labs & Seminar Hall"));

            // -------------------------------------------------------------
            // Ground Floor (F0) Nodes - Y = 0
            // -------------------------------------------------------------
            data.nodes.Add(new NavNode("N_F0_ENTRANCE", "F0", -3.0f, 0f, 0.0f, "Main Entrance", NodeType.Entrance));
            data.nodes.Add(new NavNode("N_F0_C0", "F0", 0.0f, 0f, 0.0f, "Ground Floor Corridor West", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F0_C1", "F0", 12.5f, 0f, 0.0f, "Ground Floor Corridor Junction 1", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F0_C2", "F0", 25.0f, 0f, 0.0f, "Ground Floor Central Corridor", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F0_C3", "F0", 37.5f, 0f, 0.0f, "Ground Floor Corridor Junction 2", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F0_C4", "F0", 50.0f, 0f, 0.0f, "Ground Floor Corridor East", NodeType.Waypoint));

            data.nodes.Add(new NavNode("N_F0_OFFICE", "F0", 12.5f, 0f, 6.0f, "CSE Office", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F0_LOUNGE", "F0", 12.5f, 0f, -6.0f, "Student Lounge", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F0_STAIRS", "F0", 25.0f, 0f, 5.0f, "Ground Floor Staircase", NodeType.Staircase));
            data.nodes.Add(new NavNode("N_F0_LAB1", "F0", 37.5f, 0f, -6.0f, "CSE Lab 1", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F0_CONF", "F0", 37.5f, 0f, 6.0f, "Conference Room", NodeType.Destination));

            // Ground Floor Destinations
            data.destinations.Add(new DestinationData("DEST_F0_ENTRANCE", "Main Entrance", DepartmentName, "F0", "N_F0_ENTRANCE", DestinationType.Entrance, "Main building entrance gate and reception"));
            data.destinations.Add(new DestinationData("DEST_F0_OFFICE", "CSE Department Office", DepartmentName, "F0", "N_F0_OFFICE", DestinationType.FacultyRoom, "Head of Department and CSE administrative office"));
            data.destinations.Add(new DestinationData("DEST_F0_LOUNGE", "Student Lounge", DepartmentName, "F0", "N_F0_LOUNGE", DestinationType.Other, "Ground floor student lounge and waiting area"));
            data.destinations.Add(new DestinationData("DEST_F0_STAIRS", "Ground Floor Staircase", DepartmentName, "F0", "N_F0_STAIRS", DestinationType.Staircase, "Staircase access to upper floors"));
            data.destinations.Add(new DestinationData("DEST_F0_LAB1", "CSE Lab 1", DepartmentName, "F0", "N_F0_LAB1", DestinationType.Laboratory, "CSE Programming and Software Development Lab"));
            data.destinations.Add(new DestinationData("DEST_F0_CONF", "Conference Room", DepartmentName, "F0", "N_F0_CONF", DestinationType.Other, "Department conference and presentation room"));

            // Ground Floor Edges
            data.edges.Add(new NavEdge("E_F0_01", "N_F0_ENTRANCE", "N_F0_C0", 3.0f, true, EdgeType.Entrance));
            data.edges.Add(new NavEdge("E_F0_02", "N_F0_C0", "N_F0_C1", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F0_03", "N_F0_C1", "N_F0_C2", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F0_04", "N_F0_C2", "N_F0_C3", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F0_05", "N_F0_C3", "N_F0_C4", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F0_06", "N_F0_C1", "N_F0_OFFICE", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F0_07", "N_F0_C1", "N_F0_LOUNGE", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F0_08", "N_F0_C2", "N_F0_STAIRS", 5.0f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F0_09", "N_F0_C3", "N_F0_LAB1", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F0_10", "N_F0_C3", "N_F0_CONF", 6.0f, true, EdgeType.Door));

            // -------------------------------------------------------------
            // First Floor (F1) Nodes - Y = 4
            // -------------------------------------------------------------
            data.nodes.Add(new NavNode("N_F1_C0", "F1", 0.0f, 4f, 0.0f, "First Floor Corridor West", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F1_C1", "F1", 12.5f, 4f, 0.0f, "First Floor Corridor Junction 1", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F1_C2", "F1", 25.0f, 4f, 0.0f, "First Floor Central Corridor", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F1_C3", "F1", 37.5f, 4f, 0.0f, "First Floor Corridor Junction 2", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F1_C4", "F1", 50.0f, 4f, 0.0f, "First Floor Corridor East", NodeType.Waypoint));

            data.nodes.Add(new NavNode("N_F1_R101", "F1", 12.5f, 4f, 6.0f, "Room 101", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F1_LAB2", "F1", 12.5f, 4f, -6.0f, "CSE Lab 2", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F1_STAIRS", "F1", 25.0f, 4f, 5.0f, "First Floor Staircase", NodeType.Staircase));
            data.nodes.Add(new NavNode("N_F1_R102", "F1", 37.5f, 4f, 6.0f, "Room 102", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F1_FAC1", "F1", 37.5f, 4f, -6.0f, "Faculty Room 1", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F1_R103", "F1", 0.0f, 4f, 6.0f, "Tutorial Room 103", NodeType.Destination));

            // First Floor Destinations
            data.destinations.Add(new DestinationData("DEST_F1_R101", "Room 101", DepartmentName, "F1", "N_F1_R101", DestinationType.Classroom, "Classroom for 2nd Year CSE Section A"));
            data.destinations.Add(new DestinationData("DEST_F1_R102", "Room 102", DepartmentName, "F1", "N_F1_R102", DestinationType.Classroom, "Classroom for 2nd Year CSE Section B"));
            data.destinations.Add(new DestinationData("DEST_F1_LAB2", "CSE Lab 2", DepartmentName, "F1", "N_F1_LAB2", DestinationType.Laboratory, "Computer Networks & Operating Systems Laboratory"));
            data.destinations.Add(new DestinationData("DEST_F1_FAC1", "Faculty Room 1", DepartmentName, "F1", "N_F1_FAC1", DestinationType.FacultyRoom, "Faculty cabins for CSE professors"));
            data.destinations.Add(new DestinationData("DEST_F1_STAIRS", "First Floor Staircase", DepartmentName, "F1", "N_F1_STAIRS", DestinationType.Staircase, "Central staircase connecting F0, F1, and F2"));
            data.destinations.Add(new DestinationData("DEST_F1_R103", "Tutorial Room 103", DepartmentName, "F1", "N_F1_R103", DestinationType.Classroom, "Interactive tutorial and seminar classroom"));

            // First Floor Edges
            data.edges.Add(new NavEdge("E_F1_01", "N_F1_C0", "N_F1_C1", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F1_02", "N_F1_C1", "N_F1_C2", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F1_03", "N_F1_C2", "N_F1_C3", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F1_04", "N_F1_C3", "N_F1_C4", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F1_05", "N_F1_C0", "N_F1_R103", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F1_06", "N_F1_C1", "N_F1_R101", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F1_07", "N_F1_C1", "N_F1_LAB2", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F1_08", "N_F1_C2", "N_F1_STAIRS", 5.0f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F1_09", "N_F1_C3", "N_F1_R102", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F1_10", "N_F1_C3", "N_F1_FAC1", 6.0f, true, EdgeType.Door));

            // -------------------------------------------------------------
            // Second Floor (F2) Nodes - Y = 8
            // -------------------------------------------------------------
            data.nodes.Add(new NavNode("N_F2_C0", "F2", 0.0f, 8f, 0.0f, "Second Floor Corridor West", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F2_C1", "F2", 12.5f, 8f, 0.0f, "Second Floor Corridor Junction 1", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F2_C2", "F2", 25.0f, 8f, 0.0f, "Second Floor Central Corridor", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F2_C3", "F2", 37.5f, 8f, 0.0f, "Second Floor Corridor Junction 2", NodeType.Waypoint));
            data.nodes.Add(new NavNode("N_F2_C4", "F2", 50.0f, 8f, 0.0f, "Second Floor Corridor East", NodeType.Waypoint));

            data.nodes.Add(new NavNode("N_F2_R201", "F2", 12.5f, 8f, 6.0f, "Room 201", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F2_LAB3", "F2", 12.5f, 8f, -6.0f, "CSE Lab 3", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F2_STAIRS", "F2", 25.0f, 8f, 5.0f, "Second Floor Staircase", NodeType.Staircase));
            data.nodes.Add(new NavNode("N_F2_R202", "F2", 37.5f, 8f, 6.0f, "Room 202", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F2_SEMINAR", "F2", 37.5f, 8f, -6.0f, "Seminar Hall", NodeType.Destination));
            data.nodes.Add(new NavNode("N_F2_PROJECT", "F2", 0.0f, 8f, 6.0f, "Project Lab", NodeType.Destination));

            // Second Floor Destinations
            data.destinations.Add(new DestinationData("DEST_F2_R201", "Room 201", DepartmentName, "F2", "N_F2_R201", DestinationType.Classroom, "Classroom for 3rd Year CSE Section A"));
            data.destinations.Add(new DestinationData("DEST_F2_R202", "Room 202", DepartmentName, "F2", "N_F2_R202", DestinationType.Classroom, "Classroom for 3rd Year CSE Section B"));
            data.destinations.Add(new DestinationData("DEST_F2_LAB3", "CSE Lab 3", DepartmentName, "F2", "N_F2_LAB3", DestinationType.Laboratory, "AI, Machine Learning and Cloud Computing Lab"));
            data.destinations.Add(new DestinationData("DEST_F2_SEMINAR", "Seminar Hall", DepartmentName, "F2", "N_F2_SEMINAR", DestinationType.Other, "Auditorium and seminar hall for departmental events"));
            data.destinations.Add(new DestinationData("DEST_F2_STAIRS", "Second Floor Staircase", DepartmentName, "F2", "N_F2_STAIRS", DestinationType.Staircase, "Upper floor staircase landing"));
            data.destinations.Add(new DestinationData("DEST_F2_PROJECT", "Project Lab", DepartmentName, "F2", "N_F2_PROJECT", DestinationType.Laboratory, "Final year student project research laboratory"));

            // Second Floor Edges
            data.edges.Add(new NavEdge("E_F2_01", "N_F2_C0", "N_F2_C1", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F2_02", "N_F2_C1", "N_F2_C2", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F2_03", "N_F2_C2", "N_F2_C3", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F2_04", "N_F2_C3", "N_F2_C4", 12.5f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F2_05", "N_F2_C0", "N_F2_PROJECT", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F2_06", "N_F2_C1", "N_F2_R201", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F2_07", "N_F2_C1", "N_F2_LAB3", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F2_08", "N_F2_C2", "N_F2_STAIRS", 5.0f, true, EdgeType.Corridor));
            data.edges.Add(new NavEdge("E_F2_09", "N_F2_C3", "N_F2_R202", 6.0f, true, EdgeType.Door));
            data.edges.Add(new NavEdge("E_F2_10", "N_F2_C3", "N_F2_SEMINAR", 6.0f, true, EdgeType.Door));

            // -------------------------------------------------------------
            // Inter-Floor Vertical Connections (Staircases)
            // -------------------------------------------------------------
            // Distance between floors is 4m vertical + stair length ≈ 6.0m effective walking distance
            data.edges.Add(new NavEdge("E_STAIR_F0_F1", "N_F0_STAIRS", "N_F1_STAIRS", 6.0f, true, EdgeType.Staircase));
            data.edges.Add(new NavEdge("E_STAIR_F1_F2", "N_F1_STAIRS", "N_F2_STAIRS", 6.0f, true, EdgeType.Staircase));

            return data;
        }
    }
}
