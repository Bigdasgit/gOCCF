using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class SignedRWR
    {
        private double[] score;
        private double[] pscore;
        private double[] nscore;
        private double D;
        private double T = -1.0d;
        private int I = 20;
        private WeightedUndirectedGraph graph;
        private double[] restart;
        private double beta;
        private double gamma;
        /// <param name="TargetGraph">
        /// Undirected web graph where the RWR will be calculated.
        /// </param>
        /// <param name="DampingFactor">
        /// The damping factor alpha.
        /// </param>
        /// <param name="InitialNode">
        /// List of initial nodes whose initial score set to be 1 and all others 0.
        /// </param>
        public SignedRWR(WeightedUndirectedGraph TargetGraph, double DampingFactor, int InitialNode, double beta, double gamma)
            : this(TargetGraph, DampingFactor, InitialNode, -1, beta, gamma)
        {
        }

        /// <param name="TargetGraph">
        /// Undirected web graph where the RWR will be calculated.
        /// </param>
        /// <param name="DampingFactor">
        /// The damping factor alpha.
        /// </param>
        /// <param name="InitialNode">
        /// List of initial nodes whose initial score set to be 1 and all others 0.
        /// </param>
        /// <param name="T">
        /// Error tolerance.
        /// </param>
        public SignedRWR(WeightedUndirectedGraph TargetGraph, double DampingFactor, int InitialNode, double T, double beta, double gamma)
        {
            this.D = DampingFactor;
            this.graph = TargetGraph;
            this.T = T;
            this.beta = beta;
            this.gamma = gamma;

            score = new double[graph.Size];
            nscore = new double[graph.Size];
            pscore = new double[graph.Size];
            restart = new double[graph.Size];
            for (int i = 0; i < score.Length; i++)
            {
                if (InitialNode == i)
                {
                    pscore[i] = 1.0d;
                    nscore[i] = 0.0d;
                    restart[i] = 1.0d;
                    score[i] = pscore[i] - nscore[i];
                }
                else
                {
                    pscore[i] = 0.0d;
                    nscore[i] = 0.0d;
                    score[i] = pscore[i] - nscore[i];
                    restart[i] = 0.0d;
                }
            }
        }

        public List<Weight> Calculate()
        {
            double stderr = double.MaxValue;
            int cnt = 1;

            //Console.WriteLine("Start: RWR...");

            List<Weight> result = new List<Weight>();

            if (T <= 0.0d)
                for (int i = 1; i <= I; i++)
                {
                    //Console.Write("Iteration {0}: ", i);
                    stderr = Compute();
                    //Console.Write(stderr);
                    //Console.WriteLine();
                }
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
            Dictionary<int, double> pdlink = new Dictionary<int, double>(); // 뎅글링링크 관련 데이터 저장
            Dictionary<int, double> ndlink = new Dictionary<int, double>();
            double pdscore = 0.0d; // 뎅글링링크에 의해 파급된 스코어 계
            double ndscore = 0.0d;
            double[] ptempscore = new double[score.Length];
            double[] ntempscore = new double[score.Length];
            double sum;

            // initialize temp score vector
            for (int i = 0; i < ptempscore.Length; i++)
            {
                ptempscore[i] = 0.0d;
                ntempscore[i] = 0.0d;
            }

            for (int i = 0; i < score.Length; i++)
            {
                // get outlinks
                links = graph.GetOutlinks(i);

                // 아웃링크 없는 경우(뎅글링링크)
                if (links == null)
                {
                    double s = (double)(pscore[i] / (double)(pscore.Length - 1)); // 네트워크 전반에 파급시킬 값
                    double n = (double)(nscore[i] / (double)(nscore.Length - 1));
                    pdlink.Add(i, s); // dlink에 자신이 네트워크 전반에 파급시킨 값을 저장
                    ndlink.Add(i, n);
                    pdscore += s;
                    ndscore += n;
                    continue;
                }
                // 아웃링크를 따라 파급
                else
                {
                    sum = 0.0d;
                    // get sum of out-link weights
                    foreach (Weight j in links)
                    {
                        if (j.w > 0)
                            sum += j.w;
                        else
                            sum -= j.w;
                    }
                    // give score
                    foreach (Weight j in links)
                    {
                        if (j.w > 0)
                        {
                            ptempscore[j.id] += (double)(pscore[i] * (j.w / sum));
                            ptempscore[j.id] += (1 - gamma) * (double)(nscore[i] * (j.w / sum));
                            ntempscore[j.id] += (gamma) * (double)(nscore[i] * (j.w / sum));
                        }
                        else
                        {
                            ptempscore[j.id] += (beta) * (double)(nscore[i] * (-j.w / sum));
                            ntempscore[j.id] += (double)(pscore[i] * (-j.w / sum));
                            ntempscore[j.id] += (1 - beta) * (double)(nscore[i] * (-j.w / sum));
                        }
                    }
                }
            }

            // 계산결과 적용
            double stderr = 0, ptmpscore, ntmpscore, tmpscore;

            for (int i = 0; i < score.Length; i++)
            {
                if (pdlink.ContainsKey(i))
                {
                    ptmpscore = ((ptempscore[i] + pdscore - pdlink[i]) * D) + (restart[i] * (1.0d - D));
                    ntmpscore = ((ntempscore[i] + ndscore - ndlink[i]) * D);
                }
                else
                {
                    ptmpscore = ((ptempscore[i] + pdscore) * D) + (restart[i] * (1.0d - D));
                    ntmpscore = ((ntempscore[i] + ndscore) * D);
                }
                tmpscore = ptmpscore - ntmpscore;
                stderr += Math.Pow(score[i] - tmpscore, 2);
                pscore[i] = ptmpscore;
                nscore[i] = ntmpscore;
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
