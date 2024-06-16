using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Rooms
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private LevelLayoutData _layoutData;
        //will do manually now.
        [SerializeField] private List<RoomView> _roomTemplates;

        [SerializeField] private List<RoomView> _rooms = new List<RoomView>();

        [Button]
        private void GenerateRooms()
        {
            if (_roomTemplates == null || _roomTemplates.Count == 0) return;

            if (_rooms != null)
            {
                foreach (var room in _rooms)
                {
                    DestroyImmediate(room.gameObject);
                }

                _rooms.Clear();
            }

            for (int i = 0; i < _layoutData.RoomCount; i++)
            {
                var rndPick = Random.Range(0, _roomTemplates.Count);
                var newRoom = Instantiate(_roomTemplates[rndPick], Vector3.zero, Quaternion.identity, transform);
                newRoom.GenerateRoom();
                _rooms.Add(newRoom);
            }

            RandomizePositions();
        }

        [Button]
        private void ClearLevel()
        {
            if (_rooms != null)
            {
                foreach (var room in _rooms)
                {
                    DestroyImmediate(room.gameObject);
                }

                _rooms.Clear();
            }
        }

        private void RandomizePositions()
        {
            if (_rooms == null || _rooms.Count == 0) return;

            foreach (var room in _rooms)
            {
                float randomX = Random.Range(-_layoutData.Bounds.x / 2, _layoutData.Bounds.x / 2);
                float randomZ = Random.Range(-_layoutData.Bounds.y / 2, _layoutData.Bounds.y / 2);
                room.transform.position =
                    new Vector3(Mathf.RoundToInt(randomX), room.transform.position.y, Mathf.RoundToInt(randomZ));
            }
        }

        //todo fix positions if interwined

        //get doors into list

        //triangulate to find closest distances

        //*A for finding paths

        //build corridors :< 

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero,
                new Vector3(_layoutData.Bounds.x, 0, _layoutData.Bounds.y));
        }
    }
}