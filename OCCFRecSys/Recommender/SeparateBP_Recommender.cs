using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OCCFRecSys
{
    public class SeparateBP_Recommender : Recommender
    {
        public int fold;
        string candidate_item;
        Dictionary<int, List<Score>> recommend_user_item;

        // BP variables
        public double propagation_alpha;
        public int num_iter, max_user_id, max_item_id;
        BPMatrix like_edges, dislike_edges;
        Dictionary<int, NodePotential> node_potential_like, node_potential_dislike;
        double[,] propagation_matrix = new double[2, 2] { { 0.5, 0.5 }, { 0.5, 0.5 } };

        public SeparateBP_Recommender(string candidate_item, int num_iter, int num_recommend, double propagation_alpha, int fold, StreamReader training, StreamReader test) //, double weight
        {
            this.candidate_item = candidate_item;
            this.num_iter = num_iter;
            this.fold = fold;
            this.propagation_alpha = propagation_alpha;

            Init(num_recommend, training, test);
        }
        
        public override void Init(int num_recommend, StreamReader training, StreamReader test)
        {
            this.num_recommend = num_recommend;
            // propagation matrix 
            for (int i = 0; i < 2; i++)
            {
                propagation_matrix[i, i] += propagation_alpha;
                if (i == 0)
                    propagation_matrix[i, i + 1] -= propagation_alpha;
                else
                    propagation_matrix[i, i - 1] -= propagation_alpha;
            }
            Console.WriteLine(propagation_matrix[0, 0] + " " + propagation_matrix[0, 1] + " " + propagation_matrix[1, 0] + " " + propagation_matrix[1, 1]);

            ReadTestSet(test, candidate_item);
            test.Close();
            Console.WriteLine("Read test set: Complete");

            ReadTrainingSet(training);
            training.Close();
            Console.WriteLine("Read train set: Complete");
        }

        public override void ReadTrainingSet(StreamReader sr)
        {
            max_user_id = int.MinValue;
            max_item_id = int.MinValue;

            like_edges = new BPMatrix();
            dislike_edges = new BPMatrix();
            node_potential_like = new Dictionary<int, NodePotential>();
            node_potential_dislike = new Dictionary<int, NodePotential>();

            while (sr.EndOfStream == false)
            {
                line = sr.ReadLine().Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int user_id = int.Parse(line[0].ToString());
                int item_id = int.Parse(line[1].ToString()) + Program.num_user;
                int rating = int.Parse(line[2].ToString());

                if (max_user_id < user_id)
                    max_user_id = user_id;
                if (max_item_id < item_id)
                    max_item_id = item_id;

                if (rating == 1)
                {
                    Edge forward_edge = new Edge();
                    like_edges.SetAt(user_id, item_id, forward_edge);
                    Edge backward_edge = new Edge();
                    like_edges.SetAt(item_id, user_id, backward_edge);

                    if (node_potential_like.ContainsKey(user_id))
                        node_potential_like[user_id].AddNode(item_id);
                    else
                    {
                        NodePotential temp_node = new NodePotential();
                        temp_node.AddNode(item_id);
                        node_potential_like.Add(user_id, temp_node);
                    }
                }
                else
                {
                    Edge forward_edge = new Edge();
                    dislike_edges.SetAt(user_id, item_id, forward_edge);
                    Edge backward_edge = new Edge();
                    dislike_edges.SetAt(item_id, user_id, backward_edge);

                    if (node_potential_dislike.ContainsKey(user_id))
                        node_potential_dislike[user_id].AddNode(item_id);
                    else
                    {
                        NodePotential temp_node = new NodePotential();
                        temp_node.AddNode(item_id);
                        node_potential_dislike.Add(user_id, temp_node);
                    }
                }
            }
        }

        public override void Recommend()
        {
            BeliefPropagation BP_like = new BeliefPropagation(like_edges, node_potential_like, propagation_matrix, max_user_id, max_item_id);
            BeliefPropagation BP_dislike = new BeliefPropagation(dislike_edges, node_potential_dislike, propagation_matrix, max_user_id, max_item_id);
            recommend_user_item = new Dictionary<int, List<Score>>();

            int num = 0;
            foreach (int target_user in correct_user_item.Keys)
            {
                if (!node_potential_like.ContainsKey(target_user) && !node_potential_dislike.ContainsKey(target_user)) continue;
                if (num != 0)
                {
                    like_edges.Clear();
                    dislike_edges.Clear();
                }

                Dictionary<int, double> like_items = PerformBP(BP_like, target_user, 0.9);
                Dictionary<int, double> dislike_items = PerformBP(BP_dislike, target_user, 0.7);

                double dislike_min = 1d;
                foreach (int item_id in dislike_items.Keys)
                {
                    if (dislike_items[item_id] == 0) continue;
                    if (dislike_min > dislike_items[item_id])
                        dislike_min = dislike_items[item_id];
                }

                List<Score> recommend_items = new List<Score>();
                foreach (int item_id in like_items.Keys)
                {
                    double final = 0d;

                    if (candidate_item == "Longtail_items")
                        if (Program.tophead_items.Contains(item_id)) continue;

                    if (node_potential_like[target_user].linkednode_list.Contains(item_id)) continue;
                    else if (node_potential_dislike.ContainsKey(target_user))
                        if (node_potential_dislike[target_user].linkednode_list.Contains(item_id)) continue;

                    if (like_items[item_id] == 0) continue;
                    if (dislike_items[item_id] == 0)
                        final = like_items[item_id] - dislike_min;
                    else
                        final = like_items[item_id] - dislike_items[item_id];

                    Score user_score = new Score(item_id, final);
                    recommend_items.Add(user_score);
                }

                recommend_items.Sort(Score.CompareScore);                
                recommend_user_item.Add(target_user, recommend_items);

                num++;
                if (num % 50 == 0)
                    Console.WriteLine("Compute BP score for {0} user / {1} users (fold{2}): Complete", num, correct_user_item.Count(), fold);
            }
            Console.WriteLine("Compute BP scores: Complete");
        }

        public Dictionary<int, double> PerformBP(BeliefPropagation BP, int target_user, double target_prior)
        {
            List<Score> belief_score_list = new List<Score>();
            BP.AssignNodePotential(target_user, target_prior);

            for (int iter = 0; iter < num_iter; iter++)
            {
                // message passing
                BP.PassMessage(target_user, iter);

                belief_score_list = BP.ComputeBeliefScore(target_user, test_items, num_recommend, correct_user_item);
            }

            Dictionary<int, double> recommend_items = new Dictionary<int, double>();
            for (int i = 0; i < belief_score_list.Count; i++)
                recommend_items.Add(belief_score_list[i].item_id, belief_score_list[i].score);
            
            return recommend_items;
        }

        public override void PrintResults(StreamWriter sw)
        {
            ComputeMRR(recommend_user_item, sw);
            ComputeHLU(recommend_user_item, sw);
            ComputeAccuary(recommend_user_item, sw);
            ComputeNDCG(recommend_user_item, sw);
            Console.WriteLine("Compute accuracy of recommendation: Complete");
        }
    }
}
