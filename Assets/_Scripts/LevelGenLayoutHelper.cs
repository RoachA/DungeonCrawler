using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Rooms
{
    public static class LevelGenLayoutHelper
    {
        private static List<Collider> _objectsToCheck;

        public static List<Collider> CheckForCollidingRooms(List<Collider> colliders)
        {
            _objectsToCheck = colliders;
            var collidingRooms = new List<Collider>();

            for (int i = 0; i < _objectsToCheck.Count; i++)
            {
                for (int j = i + 1; j < _objectsToCheck.Count; j++)
                {
                    if (CheckCollision(_objectsToCheck[i], _objectsToCheck[j]))
                    {
                        if (collidingRooms.Contains(_objectsToCheck[i]) == false)
                            collidingRooms.Add(_objectsToCheck[i]);
                        if (collidingRooms.Contains(_objectsToCheck[j]) == false)
                            collidingRooms.Add(_objectsToCheck[j]);
                    }
                }
            }

            return collidingRooms;
        }

        public static bool CheckCollision(Collider collider1, Collider collider2)
        {
            if (collider1 != null && collider2 != null)
            {
                return collider1.bounds.Intersects(collider2.bounds);
            }

            return false;
        }
        
        public static bool IsWithinBounds(Vector3 vector, Vector2 bounds)
        {
            bounds -= new Vector2(5, 5); 
            return vector.x >= -bounds.x / 2 && vector.x <= bounds.x / 2 &&
                   vector.z >= -bounds.y / 2 && vector.z <= bounds.y / 2;
        }

        public static Vector3 GenerateRandomAxisAlignedUnitVector()
        {
            int randomIndex = Random.Range(0, 4);

            switch (randomIndex)
            {
                case 0:
                    return new Vector3(1, 0, 0); // +X direction
                case 1:
                    return new Vector3(-1, 0, 0); // -X direction
                case 2:
                    return new Vector3(0, 0, 1); // +Z direction
                case 3:
                    return new Vector3(0, 0, -1); // -Z direction
                default:
                    return Vector3.zero; // Fallback, though this should never be reached
            }
        }
        
        public static List<Vector2Int> CheckPositionsWithinRoomBounds(List<Vector2Int> positions, List<Collider> colliders)
        {
            List<Vector2Int> positionsWithinBounds = new List<Vector2Int>();

            foreach (var pos in positions)
            {
                foreach (var col in colliders)
                {
                    if (IsPositionWithinCollider(new Vector3(pos.x, 0, pos.y), col))
                    {
                        positionsWithinBounds.Add(pos);
                        break; // No need to check other colliders if one already contains the position
                    }
                }
            }

            return positionsWithinBounds;
        }

        public static bool IsPositionWithinCollider(Vector3 position, Collider collider)
        {
            Vector3 closestPoint = collider.ClosestPoint(position);
            return Vector3.Distance(closestPoint, position) < Mathf.Epsilon;
        }
    }
}