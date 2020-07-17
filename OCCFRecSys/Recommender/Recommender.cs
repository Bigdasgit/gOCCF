using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OCCFRecSys
{   
    public class Recommender
    {
        protected StreamReader sr;
        protected StreamWriter sw;
        protected string[] line;
        protected int num_recommend;                                            
        protected HashSet<int> test_items;                                      
        protected Dictionary<int, HashSet<int>> correct_user_item;             
        protected int[] topks = { 5, 10, 15, 20, 25, 30, 50 };
        
        public virtual void Init(int num_recommend, StreamReader training, StreamReader test) { }

        public virtual void Recommend() {   }

        public virtual void ReadTrainingSet(StreamReader sr) { }

        public virtual void PrintResults(StreamWriter sw) { }

        public void ReadTestSet(StreamReader sr, string candidate_item)
        {
            test_items = new HashSet<int>();
            correct_user_item = new Dictionary<int, HashSet<int>>();
            while (!sr.EndOfStream)
            {
                string[] line = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int user_id = int.Parse(line[0].ToString());
                int item_id = int.Parse(line[1].ToString()) + Program.num_user;
                //if (rating < 5) continue;
              
                if (!correct_user_item.ContainsKey(user_id))
                {
                    HashSet<int> correct_item = new HashSet<int>();
                    correct_item.Add(item_id);
                    correct_user_item.Add(user_id, correct_item);
                }
                else
                    correct_user_item[user_id].Add(item_id);

                test_items.Add(item_id);
            }
        }
        public void ComputeAccuary(Dictionary<int, List<Score>> recommend_user_item, StreamWriter output)
        {
            double[] precision = new double[topks.Length];
            double[] recall = new double[topks.Length];
            double[] hit = new double[topks.Length];
            double[] f1 = new double[topks.Length];

            foreach (int user in correct_user_item.Keys)
            {
                int rank = 0;
                if (!recommend_user_item.ContainsKey(user)) continue;
                double[] user_hit = new double[topks.Length];
                int size = Math.Min(recommend_user_item[user].Count, topks[topks.Length - 1]);

                for (int i = 0; i < size; i++)
                {
                    int item_id = recommend_user_item[user][i].item_id;
                    if (correct_user_item[user].Contains(item_id))
                    {
                        rank = i + 1;
                        for (int j = 0; j < topks.Length; j++)
                            if (rank <= topks[j])
                                user_hit[j]++;
                    }
                }

                for (int i = 0; i < topks.Length; i++)
                {
                    hit[i] += user_hit[i];
                    recall[i] += user_hit[i] / (double)correct_user_item[user].Count;
                }
            }

            for (int i = 0; i < topks.Length; i++)
            {
                precision[i] = hit[i] / (double)(correct_user_item.Count * topks[i]);
                recall[i] = recall[i] / (double)correct_user_item.Count;
                f1[i] = (2 * precision[i] * recall[i]) / (precision[i] + recall[i]);
            }
    
            for (int i = 0; i < topks.Length; i++)
                output.WriteLine("Prec@{0}\t{1}", topks[i], precision[i]); //, hit[i], correct_user_item.Count * topks[i]
            for (int i = 0; i < topks.Length; i++)
                output.WriteLine("Recall@{0}\t{1}", topks[i], recall[i]);
            for (int i = 0; i < topks.Length; i++)
                output.WriteLine("F1@{0}\t{1}", topks[i], f1[i]);
        }

        public void ComputeMRR(Dictionary<int, List<Score>> recommend_user_item, StreamWriter output)
        {
            double[] mrr = new double[topks.Length];
            foreach (int user in correct_user_item.Keys)
            {
                if (!recommend_user_item.ContainsKey(user)) continue;
                int size = Math.Min(recommend_user_item[user].Count, topks[topks.Length - 1]);
                for (int i = 0; i < size; i++)
                {
                    int item_id = recommend_user_item[user][i].item_id;
                    if (correct_user_item[user].Contains(item_id))
                    {
                        for (int j = 0; j < topks.Length; j++)
                        {
                            if (topks[j] > i)
                                mrr[j] += 1 / (i + 1d);
                        }
                        break;
                    }
                }
            }

            for (int i = 0; i < topks.Length; i++)
                output.WriteLine("MRR@{0}\t{1}", topks[i], mrr[i] / (double)correct_user_item.Count);
        }

        public void ComputeNDCG(Dictionary<int, List<Score>> recommend_user_item, StreamWriter output)
        {
            double[] ndcg = new double[topks.Length];
            foreach (int user in correct_user_item.Keys)
            {
                if (!recommend_user_item.ContainsKey(user)) continue;
                for (int i = 0; i < topks.Length; i++)
                {
                    double dcg = 0;
                    double idcg = 0;

                    int size = Math.Min(correct_user_item[user].Count, topks[i]);
                    for (int j = 0; j < size; j++)
                        idcg += 1 / Math.Log(j + 2, 2);

                    size = Math.Min(recommend_user_item[user].Count, topks[i]);
                    for (int j = 0; j < size; j++)
                    {
                        int item_id = recommend_user_item[user][j].item_id;
                        if (!correct_user_item[user].Contains(item_id))
                            continue;

                        int rank = j + 1;
                        dcg += 1 / Math.Log(rank + 1, 2);
                    }

                    ndcg[i] += dcg / idcg;
                }
            }

            for (int i = 0; i < topks.Length; i++)
                output.WriteLine("NDCG@{0}\t{1}", topks[i], ndcg[i] / (double)correct_user_item.Count);
        }        

        public void ComputeHLU(Dictionary<int, List<Score>> recommend_user_item, StreamWriter output)
        {
            int beta = 5; // halif-life parameter
            double hlu = 0d; // expected utility of the ranked list for users
            double max_hlu = 0d; // maximally achievable utility if all true positive items are at the top of the ranked list
            foreach (int user in correct_user_item.Keys)
            {
                if (!recommend_user_item.ContainsKey(user)) continue;

                int rank = 1;
                foreach (Score items in recommend_user_item[user])
                {
                    if (correct_user_item[user].Contains(items.item_id))
                        hlu += 1 / Math.Pow(2, (rank - 1) * (beta - 1)); // numerator
                    max_hlu += 1 / Math.Pow(2, (rank - 1) * (beta - 1)); // denominator
                    rank++;
                }
            }

            output.WriteLine("HLU\t{0}", 100d * (hlu / max_hlu));
        }

    }
}
