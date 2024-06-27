using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Rooms
{
    public class CorridorView : MonoBehaviour
    {
        [SerializeField] private Vector2Int _pos;
        [SerializeField] private GameObject _wallObj;

        public void Init(Vector2Int pos, List<Vector2Int> allCorridors)
        {
            _pos = pos;
            SetWalls(allCorridors);
        }

        private List<Vector2Int> _wallDebug = new List<Vector2Int>();

        [SerializeField] private List<Vector2Int> _neighbors = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> _freePositions = new List<Vector2Int>();

        //todo cardinals are offsetted. needs fix.
        private void SetWalls(List<Vector2Int> corridorsMap)
        {
            List<Vector2Int> cardinalNeighbors = new List<Vector2Int>();
            List<Vector2Int> freePositions = new List<Vector2Int>();

            // Define the cardinal directions
            List<Vector2Int> directions = new List<Vector2Int>
            {
                new Vector2Int(1, 0), // Right
                new Vector2Int(-1, 0), // Left
                new Vector2Int(0, 1), // Up
                new Vector2Int(0, -1) // Down
            };

            // Check each direction for a neighbor
            foreach (var direction in directions)
            {
                Vector2Int neighborPosition = _pos + direction;
                if (corridorsMap.Contains(neighborPosition))
                {
                    cardinalNeighbors.Add(neighborPosition);
                }
                else
                {
                    freePositions.Add(direction + _pos);
                }
            }

            _neighbors = cardinalNeighbors;
            _freePositions = freePositions;
        }

        private void OnDrawGizmos()
        {
            foreach (var deg in _freePositions)
            {
                Gizmos.DrawSphere(new Vector3(deg.x, 1, deg.y), .25f);
            }
        }
    }
}