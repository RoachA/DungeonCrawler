using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Rooms
{
    public class CorridorView : MonoBehaviour
    {
        [SerializeField] private GameObject _wallObj;

        [Space(30)]
        [Header("Info")]
        [SerializeField] private Vector2Int _pos;
        [SerializeField] private bool _isCorridorEnd;

        public void Init(Vector2Int pos, List<Vector2Int> allCorridors, bool isEndTile)
        {
            _pos = pos;
            _isCorridorEnd = isEndTile;
            SetWalls(allCorridors);
        }

        private List<Vector2Int> _wallDebug = new List<Vector2Int>();

        private List<Vector3> _neighbors = new List<Vector3>();
        private List<Vector3> _freePositions = new List<Vector3>();
        private List<GameObject> _walls = new List<GameObject>();

        //todo cardinals are offsetted. needs fix.
        private void SetWalls(List<Vector2Int> corridorsMap)
        {
            List<Vector3> cardinalNeighbors = new List<Vector3>();
            List<Vector3> freePositions = new List<Vector3>();

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
                var halfDir = new Vector3((float) direction.x / 2, 0, (float) direction.y / 2);
                Vector3 pos = new Vector3(_pos.x, 0, _pos.y);

                Vector2Int neighborPosition = _pos + direction;

                if (corridorsMap.Contains(neighborPosition))
                {
                    cardinalNeighbors.Add(pos + halfDir);
                }
                else
                {
                    freePositions.Add(pos + halfDir);
                }
            }

            //todo some how should avoid walling the room entries.
            _neighbors = cardinalNeighbors;
            _freePositions = freePositions;
            var center = transform.position;

            foreach (var wallPos in freePositions)
            {
                var wall = Instantiate(_wallObj, wallPos, Quaternion.identity, transform);
                Vector3 targetPos = new Vector3(center.x, center.y, center.z);
                wall.transform.LookAt(targetPos);
                wall.transform.eulerAngles += new Vector3(0, 180, 0);

                _walls.Add(wall);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var deg in _freePositions)
            {
                Gizmos.DrawSphere(deg, .25f);
            }
        }
    }
}