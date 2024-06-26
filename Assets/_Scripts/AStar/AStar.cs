using System.Collections.Generic;
using UnityEngine;

namespace Game.Rooms.Utils.Astar
{
    public class AStar
    {
        private List<Vector2Int> positions;
        private List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public AStar(List<Vector2Int> positions)
        {
            this.positions = positions;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            Node startNode = new Node(start);
            Node endNode = new Node(end);

            List<Node> openList = new List<Node>();
            HashSet<Node> closedList = new HashSet<Node>();

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].F < currentNode.F || openList[i].F == currentNode.F && openList[i].H < currentNode.H)
                    {
                        currentNode = openList[i];
                    }
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                if (currentNode.Position == end)
                {
                    return RetracePath(startNode, currentNode);
                }

                foreach (Vector2Int direction in directions)
                {
                    Vector2Int neighborPos = currentNode.Position + direction;
                    if (!IsValidPosition(neighborPos) || closedList.Contains(new Node(neighborPos)))
                    {
                        continue;
                    }

                    float newGCost = currentNode.G + GetDistance(currentNode.Position, neighborPos);
                    Node neighborNode = new Node(neighborPos)
                    {
                        G = newGCost,
                        H = GetDistance(neighborPos, end),
                        Parent = currentNode
                    };

                    if (newGCost < neighborNode.G || !openList.Contains(neighborNode))
                    {
                        neighborNode.G = newGCost;
                        neighborNode.Parent = currentNode;

                        if (!openList.Contains(neighborNode))
                        {
                            openList.Add(neighborNode);
                        }
                    }
                }
            }

            return null;
        }

        private bool IsValidPosition(Vector2Int pos)
        {
            return positions.Contains(pos);
        }

        private List<Vector2Int> RetracePath(Node startNode, Node endNode)
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

        private float GetDistance(Vector2Int a, Vector2Int b)
        {
            return Vector2Int.Distance(a, b);
        }
    }
}