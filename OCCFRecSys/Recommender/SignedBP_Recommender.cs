using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OCCFRecSys
{
    public class SignedBP_Recommender : Recommender
    {
        public int fold;
        string candidate_item;

        // BP variables        
        BPMatrix edge_list;
        public double propagation_alpha;
        public int num_iter, max_user_id, max_item_id;
        Dictionary<int, List<Score>> recommend_user_item;
        Dictionary<int, NodePotential> node_potential_like, node_potential_dislike;
        double[,] propagation_matrix_like = new double[2, 2] { { 0.5, 0.5 }, { 0.5, 0.5 } };
        double[,] propagation_matrix_dislike = new double[2, 2] { { 0.5, 0.5 }, { 0.5, 0.5 } };

        public SignedBP_Recommender(string candidate_item, int num_iter, int num_recommend, double propagation_alpha, int fold, StreamReader training, StreamReader test)
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
            for (int i = 0; i < 2; i++)
            {
                propagation_matrix_like[i, i] += propagation_alpha;
                if (i == 0)
                    propagation_matrix_like[i, i + 1] -= propagation_alpha;
                else
                    propagation_matrix_like[i, i - 1] -= propagation_alpha;
            }
            Console.WriteLine(propagation_matrix_like[0, 0] + " " + propagation_matrix_like[0, 1] + " " + propagation_matrix_like[1, 0] + " " + propagation_matrix_like[1, 1]);

            for (int i = 0; i < 2; i++)
            {
                propagation_matrix_dislike[i, i] -= propagation_alpha;
                if (i == 0)
                    propagation_matrix_dislike[i, i + 1] += propagation_alpha;
                else
                    propagation_matrix_dislike[i, i - 1] += propagation_alpha;
            }
            Console.WriteLine(propagation_matrix_dislike[0, 0] + " " + propagation_matrix_dislike[0, 1] + " " + propagation_matrix_dislike[1, 0] + " " + propagation_matrix_dislike[1, 1]);
            
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

            edge_list = new BPMatrix();
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
                    forward_edge.SetLabel(true);
                    edge_list.SetAt(user_id, item_id, forward_edge);

                    Edge backward_edge = new Edge();
                    backward_edge.SetLabel(true);
                    edge_list.SetAt(item_id, user_id, backward_edge);

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
                    forward_edge.SetLabel(false);
                    edge_list.SetAt(user_id, item_id, forward_edge);

                    Edge backward_edge = new Edge();
                    backward_edge.SetLabel(false);
                    edge_list.SetAt(item_id, user_id, backward_edge);

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
            SignedBeliefPropagation BP = new SignedBeliefPropagation(edge_list, node_potential_like, node_potential_dislike, 
                                                propagation_matrix_like, propagation_matrix_dislike, max_user_id, max_item_id);
            recommend_user_item = new Dictionary<int, List<Score>>();

            int num = 0;            
            foreach (int target_user in correct_user_item.Keys)
            {
                if (!node_potential_like.ContainsKey(target_user)) continue;
                if (num != 0)
                    edge_list.Clear();

                List<Score> recommend_items = new List<Score>();
                recommend_items = PerformBP(BP, target_user, num);

                recommend_user_item.Add(target_user, recommend_items);
                num++;

                if (num % 50 == 0)
                    Console.WriteLine("Compute BP score for {0} user / {1} users (fold{2}): Complete", num, correct_user_item.Count(), fold);
            }
            Console.WriteLine("Compute BP scores: Complete");
        }

        public List<Score> PerformBP(SignedBeliefPropagation BP, int target_user, int num)
        {
            List<Score> belief_score_list = new List<Score>();
            BP.AssignNodePotential(target_user);

            for (int iter = 0; iter < num_iter; iter++)
            {
                // message passing
                BP.PassMessage(target_user, iter);
                belief_score_list = BP.ComputeBeliefScore(target_user, test_items, num_recommend, correct_user_item);                
            }

            List<Score> recommend_items = new List<Score>();
            foreach (Score items in belief_score_list)
            {
                if (candidate_item == "Longtail_items")
                    if (Program.tophead_items.Contains(items.item_id)) continue;

                if (node_potential_like[target_user].linkednode_list.Contains(items.item_id)) continue;

                //if (node_potential_dislike.ContainsKey(target_user))
                //    if (node_potential_dislike[target_user].linkednode_list.Contains(items.item_id)) continue;
                
                if (recommend_items.Count < num_recommend)
                {
                    Score user_score = new Score(items.item_id, items.score);
                    recommend_items.Add(user_score);
                }
                else
                    break;           
            }

            // 메모리 부족으로 이미 BP 수행한 target user 관련 데이터 지움
            node_potential_like.Remove(target_user);
            BP.Clean(target_user);
            
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
