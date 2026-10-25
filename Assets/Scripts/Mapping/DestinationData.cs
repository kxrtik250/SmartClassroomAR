using System;

namespace SmartClassroomAR.Mapping
{
    [Serializable]
    public enum DestinationType
    {
        Classroom,
        Laboratory,
        FacultyRoom,
        Entrance,
        Staircase,
        Corridor,
        Other
    }

    [Serializable]
    public class DestinationData
    {
        public string destinationId;
        public string destinationName;
        public string department;
        public string floorId;
        public string navigationNodeId;
        public DestinationType destinationType;
        public string description;

        public DestinationData()
        {
            destinationId = string.Empty;
            destinationName = string.Empty;
            department = string.Empty;
            floorId = string.Empty;
            navigationNodeId = string.Empty;
            destinationType = DestinationType.Other;
            description = string.Empty;
        }

        public DestinationData(
            string destinationId,
            string destinationName,
            string department,
            string floorId,
            string navigationNodeId,
            DestinationType destinationType,
            string description)
        {
            this.destinationId = destinationId ?? string.Empty;
            this.destinationName = destinationName ?? string.Empty;
            this.department = department ?? string.Empty;
            this.floorId = floorId ?? string.Empty;
            this.navigationNodeId = navigationNodeId ?? string.Empty;
            this.destinationType = destinationType;
            this.description = description ?? string.Empty;
        }
    }
}
