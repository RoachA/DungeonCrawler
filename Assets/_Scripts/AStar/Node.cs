using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Rooms.Utils.Astar
{
    public class Node
    {
        public Vector2Int Position { get; set; }
        public Node Parent { get; set; }
        public float G { get; set; } // Cost from start to this node
        public float H { get; set; } // Estimated cost from this node to end
        public float F => G + H; // Total cost

        public Node(Vector2Int position)
        {
            Position = position;
        }
    }
}