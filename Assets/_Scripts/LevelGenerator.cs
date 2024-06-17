using System.Collections.Generic;
using System.Threading.Tasks;
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
        [SerializeField] [Range(1, 10)] private int _padding = 2;

        private List<Collider> _roomFloorColliders = new List<Collider>();

        [Button]
        private async void GenerateRooms()
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
                _rooms.Add(newRoom);
            }

            await RandomizePositions();

            foreach (var room in _rooms)
            {
                room.GenerateRoom();
            }
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

        private async Task RandomizePositions()
        {
            if (_rooms == null || _rooms.Count == 0) return;
            if (_roomFloorColliders != null && _roomFloorColliders.Count != 0) _roomFloorColliders.Clear();

            _roomFloorColliders = new List<Collider>();

            foreach (var room in _rooms)
            {
                float randomX = Random.Range(-_layoutData.Bounds.x / 2, _layoutData.Bounds.x / 2);
                float randomZ = Random.Range(-_layoutData.Bounds.y / 2, _layoutData.Bounds.y / 2);
                room.transform.position =
                    new Vector3(Mathf.RoundToInt(randomX), room.transform.position.y, Mathf.RoundToInt(randomZ));
                _roomFloorColliders.Add(room.GetFloorCollider());
            }

            await FixCollidingRoomsAsync();
        }

        private async Task FixCollidingRoomsAsync()
        {
            var collidingRooms = LevelGenLayoutHelper.CheckForCollidingRooms(_roomFloorColliders);
            if (collidingRooms.Count == 0) return;

            foreach (var collisionRoom in collidingRooms)
            {
                var offset = LevelGenLayoutHelper.GenerateRandomAxisAlignedUnitVector() * _padding;
                if (LevelGenLayoutHelper.IsWithinBounds(offset + collisionRoom.transform.parent.position, _layoutData.Bounds))
                    collisionRoom.transform.parent.position += offset;
            }

            // Add a small delay to allow for async behavior and prevent stack overflow
            await Task.Delay(1);

            // Call the method recursively
            await FixCollidingRoomsAsync();
        }


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