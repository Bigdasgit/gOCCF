using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class RWR
    {
        private double[] score;
        private double D;
        private double T = -1.0d;
        private int I = 6;
        private WeightedUndirectedGraph graph;
        private double[] restart;

        public RWR(WeightedUndirectedGraph TargetGraph, double DampingFactor, int InitialNode)
            : this(TargetGraph, DampingFactor, InitialNode, -1)
        {
        }

        public RWR(WeightedUndirectedGraph TargetGraph, double DampingFactor, int InitialNode, double T)
        {
            this.D = DampingFactor;
            this.graph = TargetGraph;
            this.T = T;

            score = new double[graph.Size];
            restart = new double[graph.Size];
            for (int i = 0; i < score.Length; i++)
            {
                if (InitialNode == i)
                {
                    score[i] = 1.0d;
                    restart[i] = 1.0d;
                }
                else
                {
                    score[i] = 0.0d;
                    restart[i] = 0.0d;
                }
            }
        }

        public List<Weight> Calculate()
        {
            double stderr = double.MaxValue;
            int cnt = 1;

            List<Weight> result = new List<Weight>();

            if (T <= 0.0d)
                for (int i = 1; i <= I; i++)
                    stderr = Compute();
            else
                while (stderr > T)
                    stderr = Compute();

            for (int i = 0; i < score.Length; i++)
                result.Add(new Weight(i, score[i]));
            result.Sort(Weight.CompareWeightDesc);

            return result;
        }

        private double Compute()
        {
            HashSet<Weight> links;
            Dictionary<int, double> dlink = new Dictionary<int, double>(); 
            double dscore = 0.0d; 
            double[] tempscore = new double[score.Length];
            double sum;

            // initialize temp score vector
            for (int i = 0; i < tempscore.Length; i++)
                tempscore[i] = 0.0d;

            for (int i = 0; i < score.Length; i++)
            {
                // get outlinks
                links = graph.GetOutlinks(i);

                if (links == null)
                {
                    double s = (double)(score[i] / (double)(score.Length - 1)); 
                    dlink.Add(i, s); 
                    dscore += s;
                    continue;
                }
                else
                {
                    sum = 0.0d;
                    // get sum of out-link weights
                    foreach (Weight j in links)
                        sum += j.w;
                    // give score
                    foreach (Weight j in links)
                        tempscore[j.id] += (double)(score[i] * (j.w / sum));
                }
            }

            double stderr = 0, tmpscore;

            for (int i = 0; i < score.Length; i++)
            {
                if (dlink.ContainsKey(i))
                    tmpscore = ((tempscore[i] + dscore - dlink[i]) * D) + (restart[i] * (1.0d - D));
                else
                    tmpscore = ((tempscore[i] + dscore) * D) + (restart[i] * (1.0d - D));
                stderr += Math.Pow(score[i] - tmpscore, 2);
                score[i] = tmpscore;
            }
            return stderr;
        }

        public void PrintScore()
        {
            for (int i = 0; i < score.Length; i++)
                //Console.WriteLine(i.ToString() + "\t" + score[i].ToString());
                return;
        }
    }
}
