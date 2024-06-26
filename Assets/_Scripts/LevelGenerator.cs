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
using Unity.Mathematics;

namespace Game.Rooms
{
    public class LevelGrid
    {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public List<Vector2Int> Units { get; private set; }

        public LevelGrid(Vector2Int size)
        {
            Height = size.y;
            Width = size.x;
            Units = GenerateGrid(size);
        }

        private List<Vector2Int> GenerateGrid(Vector2Int size)
        {
            List<Vector2Int> grid = new List<Vector2Int>();

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
        //will do manually now.
        [SerializeField] private List<RoomView> _roomTemplates;

        [SerializeField] private List<RoomView> _rooms = new List<RoomView>();
        [SerializeField] [Range(1, 10)] private int _padding = 2;

        private List<Collider> _roomFloorColliders = new List<Collider>();
        private List<Vector3> _doorPositions = new List<Vector3>();
        private List<Triangle> _delaunayMesh = new List<Triangle>(); //the triangles that form the delaunay mesh
        private List<MstHelper.Edge> _hallWayEdges = new List<MstHelper.Edge>();
        
        private LevelGrid _levelGrid;
        private bool _isProcessingLevel;

        [Button]
        private async void GenerateLevel()
        {
            _isProcessingLevel = true;

            GenerateGrid();
            await GenerateRoom();
            Triangulate();
            SetHallways();

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
                Debug.Log(pos);
            }

            var triangulator = new Triangulation(points);
            _delaunayMesh = triangulator.triangles;
        }
        
        private List<Vector2Int> _pathDebug = new List<Vector2Int>();

        private void SetHallways()
        {
            List<Vector2> triPoints = new List<Vector2>();

            foreach (Triangle triangle in _delaunayMesh)
            {
                //can get and cache vertices here actually.
                var vertPos0 = new Vector2(triangle.vertex0.position.x, triangle.vertex0.position.y);
                var vertPos1 = new Vector2(triangle.vertex1.position.x, triangle.vertex1.position.y);
                var vertPos2 = new Vector2(triangle.vertex2.position.x, triangle.vertex2.position.y);

                triPoints.Add(vertPos0);
                triPoints.Add(vertPos1);
                triPoints.Add(vertPos2);
            }

            _hallWayEdges = MstHelper.KruskalMST(triPoints);

            //draw
            AStar aStar = new AStar(_levelGrid.Units);

            var posA = Vector2Int.RoundToInt(triPoints[_hallWayEdges[0].Source]);
            var posB = Vector2Int.RoundToInt(triPoints[_hallWayEdges[1].Source]);

            var pathNodes = aStar.FindPath(posA, posB);
            _pathDebug = pathNodes;
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

            Gizmos.color = Color.white * 0.5f;
            List<Vector2> triPoints = new List<Vector2>();

            if (_isProcessingLevel) return;


            foreach (Triangle triangle in _delaunayMesh)
            {
                //can get and cache vertices here actually.
                var vertPos0 = new Vector2(triangle.vertex0.position.x, triangle.vertex0.position.y);
                var vertPos1 = new Vector2(triangle.vertex1.position.x, triangle.vertex1.position.y);
                var vertPos2 = new Vector2(triangle.vertex2.position.x, triangle.vertex2.position.y);


                triPoints.Add((vertPos0));
                triPoints.Add((vertPos1));
                triPoints.Add((vertPos2));

                Gizmos.DrawLine(new Vector3(vertPos0.x, 0, vertPos0.y), new Vector3(vertPos1.x, 0, vertPos1.y));
                Gizmos.DrawLine(new Vector3(vertPos1.x, 0, vertPos1.y), new Vector3(vertPos2.x, 0, vertPos2.y));
                Gizmos.DrawLine(new Vector3(vertPos2.x, 0, vertPos2.y), new Vector3(vertPos0.x, 0, vertPos0.y));
            }

            Gizmos.color = Color.green;

            foreach (var edge in _hallWayEdges)
            {
                var source = new Vector3(triPoints[edge.Source].x, 1, triPoints[edge.Source].y);
                var destination = new Vector3(triPoints[edge.Destination].x, 1, triPoints[edge.Destination].y);
                Gizmos.DrawLine(source, destination);
            }

            if (_pathDebug == null) return;
            
            foreach (var pos in _pathDebug)
            {
                Gizmos.color = Color.red * 0.7f;
                Gizmos.DrawCube(new Vector3(pos.x, 0, pos.y), new Vector3(0.95f, 0.1f, 0.95f));
            }

            //TODO now I shall convert all this into a usable data set that corresponds to rooms - corridors etc... GG
            //%todo 15 percent chose random edges as extra corridors.
            //todo shall handle it for the delaunator somehow. it is better!
        }
    }
}