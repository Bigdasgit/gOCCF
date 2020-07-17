using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class WeightedUndirectedGraph
    {
        public Dictionary<int, HashSet<Weight>> graph;
        public HashSet<int> fromids;                        
        public int Size;
        public int Edges;
        public int user_size;
        private double MinWeight = double.MaxValue;
        private double MaxWeight = double.MinValue;

        /// <param name="k"># of nearest neighbors</param>
        /// <param name="e">threshold of weight</param>
        public WeightedUndirectedGraph(List<WeightedLink> Links, double e)
        {
            int size = -1, edges = 0;
            user_size = -1;

            graph = new Dictionary<int, HashSet<Weight>>();
            fromids = new HashSet<int>();

            // build graph(adjacency list) from links
            foreach (WeightedLink i in Links)
            {
                // if the row not exists
                if (!fromids.Contains(i.x))
                {
                    fromids.Add(i.x);
                    graph.Add(i.x, new HashSet<Weight>());
                }
                if (!fromids.Contains(i.y))
                {
                    fromids.Add(i.y);
                    graph.Add(i.y, new HashSet<Weight>());
                }

                // find size(last node id) of graph
                if (size < i.x)
                    size = i.x;
                if (size < i.y)
                    size = i.y;

                if (user_size < i.x)
                    user_size = i.x;

                graph[i.x].Add(new Weight(i.y, i.w));
                graph[i.y].Add(new Weight(i.x, i.w));
                if (i.w > MaxWeight)
                    MaxWeight = i.w;
                if (i.w < MinWeight)
                    MinWeight = i.w;
                edges++;
            }
            this.Edges = edges;
            this.Size = size + 1;
            this.user_size += 1;
        }

        public WeightedUndirectedGraph(List<WeightedLink> Links)
        {
            int size = -1, edges = 0;
            user_size = -1;

            graph = new Dictionary<int, HashSet<Weight>>();
            fromids = new HashSet<int>();

            // build graph(adjacency list) from links
            foreach (WeightedLink i in Links)
            {
                // if the row not exists
                if (!fromids.Contains(i.x))
                {
                    fromids.Add(i.x);
                    graph.Add(i.x, new HashSet<Weight>());
                }
                if (!fromids.Contains(i.y))
                {
                    fromids.Add(i.y);
                    graph.Add(i.y, new HashSet<Weight>());
                }

                // find size(last node id) of graph
                if (size < i.x)
                    size = i.x;
                if (size < i.y)
                    size = i.y;

                if (user_size < i.x)
                    user_size = i.x;

                graph[i.x].Add(new Weight(i.y, i.w));
                graph[i.y].Add(new Weight(i.x, i.w));
                if (i.w > MaxWeight)
                    MaxWeight = i.w;
                if (i.w < MinWeight)
                    MinWeight = i.w;
                edges++;
            }
            this.Edges = edges;
            this.Size = size + 1;
            this.user_size += 1;
        }

        public HashSet<Weight> GetOutlinks(int i)
        {
            if (fromids.Contains(i))
                return graph[i];
            else
                return null;
        }
    }

    public struct WeightedLink
    {
        public WeightedLink(int a, int b, double c)
        {
            x = a;
            y = b;
            w = c;
        }

        public int x;
        public int y;
        public double w;

        public static int CompareWeightedLink(WeightedLink a, WeightedLink b)
        {
            int result = a.w.CompareTo(b.w);

            if (result == 0)
                return a.x.CompareTo(b.x);
            else
                return result;
        }
        public static int CompareWeightedLinkDesc(WeightedLink a, WeightedLink b)
        {
            int result = b.w.CompareTo(a.w);

            if (result == 0)
                return a.x.CompareTo(b.x);
            else
                return result;
        }
    }

    public struct Weight
    {
        public Weight(int ID, double Weight)
        {
            id = ID;
            w = Weight;
        }

        public int id;
        public double w;

        public static int CompareWeight(Weight a, Weight b)
        {
            int result = a.w.CompareTo(b.w);

            if (result == 0)
                return a.id.CompareTo(b.id);
            else
                return result;
        }
        public static int CompareWeightDesc(Weight a, Weight b)
        {
            int result = b.w.CompareTo(a.w);

            if (result == 0)
                return a.id.CompareTo(b.id);
            else
                return result;
        }
        public static int CompareID(Weight a, Weight b)
        {
            return a.id.CompareTo(b.id);
        }
    }
}
