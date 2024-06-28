using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DelaunayTriangulation;
using Sirenix.OdinInspector;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Triangle = DelaunayTriangulation.Triangle;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Game.Rooms.Utils.Mst;
using Game.Rooms.Utils.Astar;
using Game.Utils;
using Unity.Mathematics;

namespace Game.Rooms
{
    public class LevelGrid
    {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public HashSet<Vector2Int> Units { get; private set; }

        public LevelGrid(Vector2Int size)
        {
            Height = size.y;
            Width = size.x;
            Units = GenerateGrid(size);
        }

        private HashSet<Vector2Int> GenerateGrid(Vector2Int size)
        {
            HashSet<Vector2Int> grid = new HashSet<Vector2Int>();

            int halfWidth = size.x / 2;
            int halfHeight = size.y / 2;

            for (int i = -halfWidth; i <= halfWidth; i++)
            {
                for (int j = -halfHeight; j <= halfHeight; j++)
                {
                    grid.Add(new Vector2Int(i, j));
                }
            }

            return grid;
        }
    }

    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private LevelLayoutData _layoutData;
        [SerializeField] [Range(1, 10)] private int _padding = 2;
        //will do manually now.
        [SerializeField] private List<RoomView> _roomTemplates;
        [SerializeField] private List<CorridorView> _corridorTemplates;
        [Space(25)]
        [SerializeField] private List<RoomView> _rooms = new List<RoomView>();
        private List<CorridorView> _corridors = new List<CorridorView>();

        private List<Collider> _roomFloorColliders = new List<Collider>();
        private List<Triangle> _delaunayMesh = new List<Triangle>(); //the triangles that form the delaunay mesh
        private List<MstHelper.Edge> _corridorPaths = new List<MstHelper.Edge>();
        private List<Vector2Int> _corridorFloorPositions = new List<Vector2Int>();
        private List<Vector2> _triPoints = new List<Vector2>();

        private LevelGrid _levelGrid;
        private bool _isProcessingLevel;

        [Button]
        private async void GenerateLevel()
        {
            DebugHelper.ClearLog();
            ClearLevel();
            _isProcessingLevel = true;

            GenerateGrid();
            await GenerateRoom();
            Triangulate();
            GenerateCorridors();

            _isProcessingLevel = false;
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

            if (_corridors != null)
            {
                foreach (var corridor in _corridors)
                {
                    DestroyImmediate(corridor.gameObject);
                }

                _corridors.Clear();
            }

            _triPoints.Clear();
        }

        private async Task GenerateRoom()
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

        private void GenerateGrid()
        {
            _levelGrid = new LevelGrid(_layoutData.Bounds - Vector2Int.one);
        }

        private void Triangulate()
        {
            List<Vertex> points = new List<Vertex>();

            for (int i = 0; i < _rooms.Count; i++)
            {
                var pos = new Vector2(_rooms[i].transform.position.x, _rooms[i].transform.position.z);
                points.Add(new Vertex(pos, i));
            }

            var triangulator = new Triangulation(points);
            _delaunayMesh = triangulator.triangles;
        }

        private void GenerateCorridors()
        {
            _triPoints = new List<Vector2>();

            foreach (Triangle triangle in _delaunayMesh)
            {
                //can get and cache vertices here actually.
                var vertPos0 = new Vector2(triangle.vertex0.position.x, triangle.vertex0.position.y);
                var vertPos1 = new Vector2(triangle.vertex1.position.x, triangle.vertex1.position.y);
                var vertPos2 = new Vector2(triangle.vertex2.position.x, triangle.vertex2.position.y);

                if (!_triPoints.Contains(vertPos0))
                    _triPoints.Add(vertPos0);
                if (!_triPoints.Contains(vertPos1))
                    _triPoints.Add(vertPos1);
                if (!_triPoints.Contains(vertPos2))
                    _triPoints.Add(vertPos2);
            }

            Debug.Log("  TRIANGLES COUNT  " + _delaunayMesh.Count);
            Debug.Log(" TRIPOINTS COUNT  " + _triPoints.Count);

            foreach (var point in _triPoints)
            {
                Debug.Log(" ---->  " + point);
            }

            _corridorPaths = MstHelper.KruskalMST(_triPoints);

            //draw base halls
            _corridorFloorPositions.Clear();

            Debug.Log(" HALLWAY COUNT  " + _corridorPaths.Count);
            Debug.Log("   ---> hallway points per edge.");
            var index = 0;

            List<Vector2Int> hallwayPoints = new List<Vector2Int>();
            foreach (var edge in _corridorPaths)
            {
                var source = Vector2Int.RoundToInt(_triPoints[edge.Source]);
                var destination = Vector2Int.RoundToInt(_triPoints[edge.Destination]);

                hallwayPoints.Add(source);
                hallwayPoints.Add(destination);

                Debug.Log("path " + index + " : " + source + "  " + destination);
                var pathNodes = AStar.FindPath(source, destination, _levelGrid.Units, 1);

                if (pathNodes != null)
                    _corridorFloorPositions.AddRange(pathNodes);

                index++;
            }

            //TODO SIDE HALLS > PICK RANDOM EDGES WITHIN MESH AND ADD AS ADDITIONAL CORRIDORS.
            var extraHallCount = _corridorPaths.Count * _layoutData.SideHallFrequency;
            extraHallCount = Mathf.RoundToInt(extraHallCount);
            Debug.Log("Extra corridor count are : " + extraHallCount);
            _additionalRouteUnitPositions = new List<Vector2Int>();

            for (int i = 0; i <= extraHallCount; i++)
            {
                var pointA = FindUnusedPoint();
                var pointB = FindUnusedPoint();
                var path = AStar.FindPath(pointA, pointB, _levelGrid.Units, 1);
                _additionalRouteUnitPositions.AddRange(path); // only for debugging.
                _corridorFloorPositions.AddRange(path);
            }

            //remove the positions that are within the bounds of any rooms

            _intersectionsDebug =
                LevelGenLayoutHelper.CheckPositionsWithinRoomBounds(_corridorFloorPositions, _roomFloorColliders);
            _corridorFloorPositions = _corridorFloorPositions.Except(_intersectionsDebug).ToList();

            //Generate hallway floors

            foreach (var corridorPos in _corridorFloorPositions)
            {
                var newCorridorItem = Instantiate(
                    _corridorTemplates[0],
                    new Vector3(corridorPos.x, 0, corridorPos.y),
                    quaternion.identity, transform);
                _corridors.Add(newCorridorItem);
                newCorridorItem.Init(corridorPos, _corridorFloorPositions);
            }
        }

        private List<Vector2Int> _intersectionsDebug = new List<Vector2Int>();
        private List<Vector2Int> _additionalRouteUnitPositions = new List<Vector2Int>();

        //TODO there are some awkward situations here. They overlap already existing paths ? Should check.
        //todo probably related with rounding stuff.
        private Vector2Int FindUnusedPoint()
        {
            HashSet<Vector2Int> attemptedPoints = new HashSet<Vector2Int>();
            Vector2Int pos;

            do
            {
                pos = Vector2Int.RoundToInt(_triPoints.GetRandomElement());

                // Add the point to attempted points
                attemptedPoints.Add(pos);

                // If all points have been attempted, break to avoid infinite loop
                if (attemptedPoints.Count >= _triPoints.Count)
                {
                    GenerateCorridors();
                    Debug.LogWarning("Exhausted the points trying again.");
                    break;
                    //throw new InvalidOperationException("All points have been used.");
                }
            } while (_corridorFloorPositions.Contains(pos) || _additionalRouteUnitPositions.Contains(pos));

            return pos;
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

                var floorCollider = room.GetFloorCollider();
                _roomFloorColliders.Add(floorCollider);

                //fix half offsets
                if (Mathf.RoundToInt(floorCollider.bounds.size.x) % 2 == 0) room.transform.position += Vector3.right * 0.5f;
                if (Mathf.RoundToInt(floorCollider.bounds.size.z) % 2 == 0) room.transform.position += Vector3.up * 0.5f;
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

            await FixCollidingRoomsAsync();
        }

        private void OnDrawGizmos()
        {
            //show bounds
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_layoutData.Bounds.x - 0.5f, 0, _layoutData.Bounds.y - 0.5f));

            //show grid

            if (_levelGrid == null) return;
            foreach (var unit in _levelGrid.Units)
            {
                Gizmos.color = Color.white * 0.15f;
                Gizmos.DrawWireCube(new Vector3(unit.x, 0, unit.y), new Vector3(1, 0, 1));
            }

            if (_delaunayMesh == null) return;

            ///ALL PATHS
            Gizmos.color = Color.white * 0.5f;
            List<Vector2> triPoints = new List<Vector2>();

            if (_isProcessingLevel) return;


            foreach (Triangle triangle in _delaunayMesh)
            {
                //can get and cache vertices here actually.
                var vertPos0 = new Vector2(triangle.vertex0.position.x, triangle.vertex0.position.y);
                var vertPos1 = new Vector2(triangle.vertex1.position.x, triangle.vertex1.position.y);
                var vertPos2 = new Vector2(triangle.vertex2.position.x, triangle.vertex2.position.y);

                if (!triPoints.Contains(vertPos0))
                    triPoints.Add(vertPos0);
                if (!triPoints.Contains(vertPos1))
                    triPoints.Add(vertPos1);
                if (!triPoints.Contains(vertPos2))
                    triPoints.Add(vertPos2);

                Gizmos.DrawLine(new Vector3(vertPos0.x, 0, vertPos0.y), new Vector3(vertPos1.x, 0, vertPos1.y));
                Gizmos.DrawLine(new Vector3(vertPos1.x, 0, vertPos1.y), new Vector3(vertPos2.x, 0, vertPos2.y));
                Gizmos.DrawLine(new Vector3(vertPos2.x, 0, vertPos2.y), new Vector3(vertPos0.x, 0, vertPos0.y));
            }

            Gizmos.color = Color.green;

            foreach (var edge in _corridorPaths)
            {
                var source = new Vector3(triPoints[edge.Source].x, 1, triPoints[edge.Source].y);
                var destination = new Vector3(triPoints[edge.Destination].x, 1, triPoints[edge.Destination].y);
                Gizmos.DrawLine(source, destination);
            }

            if (_corridorFloorPositions == null) return;

            /*Gizmos.color = Color.gray * 0.9f;
            foreach (var pos in _corridorFloorPositions)
            {
                Gizmos.DrawCube(new Vector3(pos.x, -2, pos.y), new Vector3(1f, 0.1f, 1));
            }*/

            /*
            Gizmos.color = Color.yellow * 0.75f;
            foreach (var pos in _additionalRouteUnitPositions)
            {
                Gizmos.DrawCube(new Vector3(pos.x, -3, pos.y), new Vector3(1f, 0.1f, 1));
            }

            //show intersections
            Gizmos.color = Color.red * 0.75f;
            foreach (var pos in _intersectionsDebug)
            {
                Gizmos.DrawSphere(new Vector3(pos.x, 1, pos.y), .5f);
            }
            */

            //TODO now I shall convert all this into a usable data set that corresponds to rooms - corridors etc... GG
            //%todo 15 percent chose random edges as extra corridors.
            //todo shall handle it for the delaunator somehow. it is better!
        }
    }
}