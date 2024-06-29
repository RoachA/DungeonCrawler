using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Rooms
{
    public class RoomView : MonoBehaviour
    {
        public struct WallOrientation
        {
            public Vector3 Pos;
            public Vector3 Rot;

            public WallOrientation(Vector3 pos, Vector3 rot)
            {
                Pos = pos;
                Rot = rot;
            }
        }

        [SerializeField] private RoomConstructionData _roomConstructionData;
        [Space(6)] //todo get from resources laters;
        [SerializeField] private GameObject _wallObj;
        [SerializeField] private GameObject _doorObj;

        private List<GameObject> _walls = new List<GameObject>();
        private List<GameObject> _doors = new List<GameObject>();
        private List<Vector3> _roomCorners = new List<Vector3>();
        private List<WallOrientation> _wallTransforms = new List<WallOrientation>();
        private GameObject _wallsContainer;
        [SerializeField] private Collider _floorCollider;

        //todo can be bound to a one time setup.
        //todo these shall work with rotation too :)( 
        //todo or instantiate with vector3.zero, handle doors and walls etc and go on.

        private List<Vector3> FindRoomBounds()
        {
            var corners = new List<Vector3>();
            float x = _roomConstructionData.Dimensions.x;
            float y = _roomConstructionData.Dimensions.y;
            var localPos = transform.localPosition;

            corners.Add(new Vector3(localPos.x + x / 2, localPos.y, localPos.z + y / 2));
            corners.Add(new Vector3(localPos.x - x / 2, localPos.y, localPos.z + y / 2));
            corners.Add(new Vector3(localPos.x - x / 2, localPos.y, localPos.z - y / 2));
            corners.Add(new Vector3(localPos.x + x / 2, localPos.y, localPos.z - y / 2));

            GetFloorCollider();

            _roomCorners = corners;
            return corners;
        }

        public Collider GetFloorCollider()
        {
            _floorCollider = GetComponentInChildren<Collider>();
            return _floorCollider;
        }

        private List<WallOrientation> FindWallPlacementPoints()
        {
            var wallPoints = new List<WallOrientation>();

            float x = _roomConstructionData.Dimensions.x;
            float y = _roomConstructionData.Dimensions.y;

            Vector3 pos;
            Vector3 rot;

            for (int i = 1; i < x + 1; i++)
            {
                pos = new Vector3(_roomCorners[0].x - i + 0.5f, _roomCorners[0].y, _roomCorners[0].z);
                rot = new Vector3(0, 0, 0);
                wallPoints.Add(new WallOrientation(pos, rot));
            }

            for (int i = 1; i < y + 1; i++)
            {
                pos = new Vector3(_roomCorners[1].x, _roomCorners[1].y, _roomCorners[1].z - i + 0.5f);
                rot = new Vector3(0, -90, 0);
                wallPoints.Add(new WallOrientation(pos, rot));
            }

            for (int i = 1; i < x + 1; i++)
            {
                pos = new Vector3(_roomCorners[2].x + i - 0.5f, _roomCorners[2].y, _roomCorners[2].z);
                rot = new Vector3(0, -180, 0);
                wallPoints.Add(new WallOrientation(pos, rot));
            }

            for (int i = 1; i < y + 1; i++)
            {
                pos = new Vector3(_roomCorners[3].x, _roomCorners[3].y, _roomCorners[3].z + i - 0.5f);
                rot = new Vector3(0, 90, 0);
                wallPoints.Add(new WallOrientation(pos, rot));
            }

            _wallTransforms = wallPoints;
            return wallPoints;
        }

        public List<GameObject> GetDoors()
        {
            return _doors;
        }

        [Button]
        public void GenerateRoom()
        {
            FindRoomBounds();
            FindWallPlacementPoints();

            if (_wallTransforms == null || _wallTransforms.Count == 0) return;

            foreach (var wall in _walls)
            {
                DestroyImmediate(wall);
            }

            _walls.Clear();

            if (_wallsContainer == null)
            {
                _wallsContainer = new GameObject();
                _wallsContainer.transform.parent = transform;
                _wallsContainer.name = "Walls Container";
            }

            foreach (var wallTransform in _wallTransforms)
            {
                var wall = Instantiate(_wallObj, wallTransform.Pos, Quaternion.identity, _wallsContainer.transform);
                var directionVector = wall.transform.position - wallTransform.Pos;

                wall.transform.rotation = Quaternion.Euler(wallTransform.Rot);

                _walls.Add(wall);
            }

            GenerateDoors();
        }

        private void GenerateDoors()
        {
            _doors.Clear();
            List<int> wallIndexes = new List<int>();
            var doorCount = Random.Range(0, _roomConstructionData.MaxDoorCount);

            for (int i = 0; i < doorCount; i++)
            {
                wallIndexes.Add(Random.Range(0, _wallTransforms.Count));
            }

            foreach (var index in wallIndexes)
            {
                var door = Instantiate(_doorObj, _walls[index].transform.position, _walls[index].transform.rotation,
                    _wallsContainer.transform);
                DestroyImmediate(_walls[index]);
                _walls[index] = door;
                _doors.Add(door);
            }
        }

        [Button]
        private void ClearRoom()
        {
            if (_wallsContainer != null)
            {
                DestroyImmediate(_wallsContainer);
                _wallsContainer = null;
            }

            _walls.Clear();
            _doors.Clear();
        }


        private void OnDrawGizmosSelected()
        {
            var index = 0;
            foreach (var pos in FindRoomBounds())
            {
                Gizmos.color = Color.yellow;
                Handles.Label(pos + Vector3.up / 3, index.ToString());
                Gizmos.DrawSphere(pos, 0.1f);
                index++;
            }

            index = 0;
            foreach (var orientation in FindWallPlacementPoints())
            {
                Gizmos.color = Color.red;
                Handles.Label(orientation.Pos + Vector3.up / 3, index.ToString());
                Gizmos.DrawSphere(orientation.Pos, 0.05f);
                index++;
            }
        }
    }
}