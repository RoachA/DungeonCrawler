using System;
using UnityEngine;

namespace Game.Rooms
{
    [Serializable]
    public class LevelLayoutData
    {
        public Vector2Int Bounds;
        [Range(3, 100)]
        public int RoomCount;
        [Range(0, 1)]
        public float SideHallFrequency = 0.1f;
    }
}