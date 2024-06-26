using System.Collections.Generic;
using UnityEngine;

namespace Game.Rooms.Utils.Mst
{
// Kruskal Algorithm 
    public static class MstHelper
    {
        public class Edge
        {
            public int Source;
            public int Destination;
            public float Weight;

            public Edge(int source, int destination, float weight)
            {
                Source = source;
                Destination = destination;
                Weight = weight;
            }
        }

        public class Subset
        {
            public int Parent;
            public int Rank;
        }

        private static int Find(Subset[] subsets, int i)
        {
            if (subsets[i].Parent != i)
                subsets[i].Parent = Find(subsets, subsets[i].Parent);

            return subsets[i].Parent;
        }

        // A function that does union of two sets of x and y
        private static void Union(Subset[] subsets, int x, int y)
        {
            int xRoot = Find(subsets, x);
            int yRoot = Find(subsets, y);

            if (subsets[xRoot].Rank < subsets[yRoot].Rank)
                subsets[xRoot].Parent = yRoot;
            else if (subsets[xRoot].Rank > subsets[yRoot].Rank)
                subsets[yRoot].Parent = xRoot;
            else
            {
                subsets[yRoot].Parent = xRoot;
                subsets[xRoot].Rank++;
            }
        }

        public static List<Edge> KruskalMST(List<Vector2> vertices)
        {
            int V = vertices.Count;
            List<Edge> result = new List<Edge>();
            List<Edge> edges = new List<Edge>();

            // Generate all edges and their weights (distances between vertices)
            for (int i = 0; i < V; i++)
            {
                for (int j = i + 1; j < V; j++)
                {
                    float weight = Vector2.Distance(vertices[i], vertices[j]);
                    edges.Add(new Edge(i, j, weight));
                }
            }

            // Sort all the edges in non-decreasing order of their weight
            edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            // Allocate memory for creating V subsets
            Subset[] subsets = new Subset[V];
            for (int v = 0; v < V; ++v)
            {
                subsets[v] = new Subset {Parent = v, Rank = 0};
            }

            // Number of edges to be taken is equal to V-1
            int e = 0;
            int iEdge = 0;

            while (e < V - 1 && iEdge < edges.Count)
            {
                Edge nextEdge = edges[iEdge++];
                int x = Find(subsets, nextEdge.Source);
                int y = Find(subsets, nextEdge.Destination);

                // If including this edge does not cause cycle
                if (x != y)
                {
                    result.Add(nextEdge);
                    e++;
                    Union(subsets, x, y);
                }
            }

            return result;
        }
    }
}