using System.Collections.Generic;
using UnityEngine;

//a star here is shiet
namespace Game.Rooms.Utils.Astar
{
    public class AStar
    {
        private class Node
        {
            public Vector2Int Position { get; set; }
            public Node Parent { get; set; }
            public float GCost { get; set; }
            public float HCost { get; set; }
            public float FCost => GCost + HCost;

            public Node(Vector2Int position)
            {
                Position = position;
            }
        }

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, HashSet<Vector2Int> grid, float cellSize = 1)
        {
            PriorityQueue<Node> openSet = new PriorityQueue<Node>(Comparer<Node>.Create((a, b) => a.FCost.CompareTo(b.FCost)));
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            Node startNode = new Node(start);
            Node targetNode = new Node(target);

            openSet.Enqueue(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.Dequeue();

                if (currentNode.Position == target)
                {
                    return RetracePath(startNode, currentNode);
                }

                closedSet.Add(currentNode.Position);

                foreach (Vector2Int neighborPos in GetNeighbors(currentNode.Position, grid))
                {
                    if (closedSet.Contains(neighborPos))
                    {
                        continue;
                    }

                    float tentativeGCost = currentNode.GCost + Vector2Int.Distance(currentNode.Position, neighborPos);
                    Node neighborNode = new Node(neighborPos)
                        {Parent = currentNode, GCost = tentativeGCost, HCost = Vector2Int.Distance(neighborPos, target)};

                    Node existingNode = openSet.Find(node => node.Position == neighborPos);
                    if (existingNode != null && tentativeGCost >= existingNode.GCost)
                    {
                        continue;
                    }

                    openSet.Enqueue(neighborNode);
                }
            }

            return null; // Path not found
        }

        private static List<Vector2Int> GetNeighbors(Vector2Int position, HashSet<Vector2Int> grid)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            Vector2Int[] directions = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = position + direction;
                if (grid.Contains(neighborPos))
                {
                    neighbors.Add(neighborPos);
                }
            }

            return neighbors;
        }

        private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }
    }

// PriorityQueue implementation
    public class PriorityQueue<T>
    {
        private List<T> data;
        private IComparer<T> comparer;

        public PriorityQueue(IComparer<T> comparer)
        {
            this.data = new List<T>();
            this.comparer = comparer;
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;

            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (comparer.Compare(data[childIndex], data[parentIndex]) >= 0) break;

                T tmp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = tmp;
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = data.Count - 1;
            T frontItem = data[0];

            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);
            lastIndex--;

            int parentIndex = 0;
            while (true)
            {
                int leftChildIndex = 2 * parentIndex + 1;
                if (leftChildIndex > lastIndex) break;

                int rightChildIndex = leftChildIndex + 1;
                int bestChildIndex =
                    (rightChildIndex > lastIndex || comparer.Compare(data[leftChildIndex], data[rightChildIndex]) < 0)
                        ? leftChildIndex
                        : rightChildIndex;

                if (comparer.Compare(data[parentIndex], data[bestChildIndex]) <= 0) break;

                T tmp = data[parentIndex];
                data[parentIndex] = data[bestChildIndex];
                data[bestChildIndex] = tmp;
                parentIndex = bestChildIndex;
            }

            return frontItem;
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public T Find(System.Predicate<T> match)
        {
            return data.Find(match);
        }

        public int Count => data.Count;
    }
}