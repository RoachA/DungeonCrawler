using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using DelaunayTriangulation;
using Sirenix.OdinInspector;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Triangle = DelaunayTriangulation.Triangle;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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
        private List<Vector3> _doorPositions = new List<Vector3>();
        
        private List<IPoint> _hullpPoints = new List<IPoint>();
        private List<Triangle> _triangles = new List<Triangle>();
        private List<ITriangle> _delaunatorTriangles = new List<ITriangle>();

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

            //Triangulate();
            Triangulate2();
        }
        
        private void Triangulate2()
        {
            List<Vertex> points = new List<Vertex>();

            for (int i = 0; i < _rooms.Count; i++)
            {
                var pos = new Vector2(_rooms[i].transform.position.x, _rooms[i].transform.position.z);
                points.Add(new Vertex(pos, i));
                Debug.Log(pos);
            }
            
            _hullpPoints.Clear();

            var triangulator = new Triangulation(points);
            _triangles = triangulator.triangles;
        }

        private void Triangulate()
        {
            var points = new IPoint[_rooms.Count];
            _hullpPoints.Clear();

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point(_rooms[i].transform.position.x, _rooms[i].transform.position.z);
                Debug.Log(points[i]);
            }
            
            var delaunay = new Delaunator(points);
            _delaunatorTriangles = delaunay.GetTriangles().ToList();
           // delaunay.gettr
            
            var hullPoints = delaunay.GetHullPoints();

            foreach (var triPoint in hullPoints)
            {
                _hullpPoints.Add(triPoint);
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
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_layoutData.Bounds.x, 0, _layoutData.Bounds.y));

            /*if (_edgePoints != null || _edgePoints?.Count != 0)
            {
                for (int i = 0; i < _edgePoints.Count; i++)
                {
                    if (i == 0) continue;

                    Gizmos.color = Color.yellow;
                    var pointA = new Vector3(_edgePoints[i].x, 0, _edgePoints[i].y);
                    var pointB = new Vector3(_edgePoints[i - 1].x, 0, _edgePoints[i - 1].y);

                    Gizmos.DrawLine(pointA, pointB);
                }
            }*/

            if (_triangles == null) return;

            Gizmos.color = Color.green * 0.7f;
            foreach (Triangle triangle in _triangles)
            {
                var vertPos0 = new Vector3(triangle.vertex0.position.x, 0, triangle.vertex0.position.y);
                var vertPos1 = new Vector3(triangle.vertex1.position.x, 0, triangle.vertex1.position.y);
                var vertPos2 = new Vector3(triangle.vertex2.position.x, 0, triangle.vertex2.position.y);

                Gizmos.DrawLine(vertPos0, vertPos1);
                Gizmos.DrawLine(vertPos1, vertPos2);
                Gizmos.DrawLine(vertPos2, vertPos0);
            }
            
            //todo shall handle it for the delaunator somehow. it is better!
            /*
            if (_delaunatorTriangles == null) return;
            foreach (ITriangle triangle in _delaunatorTriangles)
            {
                var vertPos0 = new Vector3(triangle.Points.GetEnumerator().Current.X, 
                /*var vertPos0 = new Vector3(triangle.vertex0.position.x, 0, triangle.vertex0.position.y);
                var vertPos1 = new Vector3(triangle.vertex1.position.x, 0, triangle.vertex1.position.y);
                var vertPos2 = new Vector3(triangle.vertex2.position.x, 0, triangle.vertex2.position.y);#1#

                Gizmos.DrawLine(vertPos0, vertPos1);
                Gizmos.DrawLine(vertPos1, vertPos2);
                Gizmos.DrawLine(vertPos2, vertPos0);
            }
            */
            

            if (_hullpPoints == null || _hullpPoints.Count == 0) return;

            Gizmos.color = Color.red;
            foreach (var trianglePoint in _hullpPoints)
            {
                Gizmos.DrawWireSphere(new Vector3((float) trianglePoint.X, 0, (float) trianglePoint.Y), 0.45f);
            }
        }
    }
}