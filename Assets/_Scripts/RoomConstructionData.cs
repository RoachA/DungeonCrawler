using System;
using UnityEngine;

namespace Game.Rooms
{
    [Serializable]
    public class RoomConstructionData
    {
        public Vector2 Dimensions;
        public int MaxDoorCount;
    }
}