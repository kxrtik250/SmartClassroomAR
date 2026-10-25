using System;

namespace SmartClassroomAR.Mapping
{
    [Serializable]
    public class FloorData
    {
        public string floorId;
        public string floorName;
        public int floorNumber;
        public string description;

        public FloorData()
        {
            floorId = string.Empty;
            floorName = string.Empty;
            floorNumber = 0;
            description = string.Empty;
        }

        public FloorData(string floorId, string floorName, int floorNumber, string description)
        {
            this.floorId = floorId ?? string.Empty;
            this.floorName = floorName ?? string.Empty;
            this.floorNumber = floorNumber;
            this.description = description ?? string.Empty;
        }
    }
}
