using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OCCFRecSys
{
    public class SignedBeliefPropagation
    {
        int max_user_id;
        int max_item_id;
        BPMatrix edge_list;
        double[,] propagation_matrix;
        double[,] propagation_matrix_like;
        double[,] propagation_matrix_dislike;
        Dictionary<int, NodePotential> node_potential_like;
        Dictionary<int, NodePotential> node_potential_dislike;

        public SignedBeliefPropagation
            (BPMatrix edge_list, Dictionary<int, NodePotential> node_potential_like, Dictionary<int, NodePotential> node_potential_dislike, double[,] propagation_matrix_like, double[,] propagation_matrix_dislike, int max_user_id, int max_item_id)
        {
            this.edge_list = edge_list;
            this.node_potential_like = node_potential_like;
            this.node_potential_dislike = node_potential_dislike;
            this.propagation_matrix_like = propagation_matrix_like;
            this.propagation_matrix_dislike = propagation_matrix_dislike;
            this.max_user_id = max_user_id;
            this.max_item_id = max_item_id;
        }

        public void AssignNodePotential(int node_id)
        {
            foreach (KeyValuePair<int, Dictionary<int, Edge>> row in edge_list.bp_matrix)
            {
                if (node_potential_like[node_id].linkednode_list.Contains(row.Key))
                    node_potential_like[node_id].SetPriorScore(row.Key, 0.9);
                else if (row.Key == node_id)
                    node_potential_like[node_id].SetPriorScore(row.Key, 0.9);
                else if (node_potential_dislike.ContainsKey(node_id))
                {
                    if (node_potential_dislike[node_id].linkednode_list.Contains(row.Key))
                        node_potential_like[node_id].SetPriorScore(row.Key, 0.4);
                    else
                        node_potential_like[node_id].SetPriorScore(row.Key, 0.5);
                }
                else
                    node_potential_like[node_id].SetPriorScore(row.Key, 0.5);
            }
        }

        public void PassMessage(int node_id, int num_iter)
        {
            foreach (KeyValuePair<int, Dictionary<int, Edge>> outerEdge in edge_list.bp_matrix)
            {
                double sumMessage_Like = 1d;
                double sumMessage_Dislike = 1d;

                int mc = 0;
                foreach (KeyValuePair<int, Edge> innerEdge in outerEdge.Value)
                {
                    sumMessage_Like = sumMessage_Like * edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Like;
                    sumMessage_Dislike = sumMessage_Dislike * edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Dislike;
                    mc++;

                    if (num_iter > 0)
                    {
                        if (mc % 20 == 0)
                        {
                            sumMessage_Like *= 1000;
                            sumMessage_Dislike *= 1000;
                        }
                    }
                }

                foreach (KeyValuePair<int, Edge> innerEdge in outerEdge.Value)
                {
                    double Message_Like = 0d;
                    double Message_Dislike = 0d;
                    if (innerEdge.Value.GetLabel() == true)
                        propagation_matrix = propagation_matrix_like;
                    else
                        propagation_matrix = propagation_matrix_dislike;

                    Message_Like
                        = (node_potential_like[node_id].GetPriorScore(outerEdge.Key) * propagation_matrix[0, 0] * (sumMessage_Like / edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Like))
                        + ((1 - node_potential_like[node_id].GetPriorScore(outerEdge.Key)) * propagation_matrix[1, 0] * (sumMessage_Dislike / edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Dislike));
                    Message_Dislike
                        = (node_potential_like[node_id].GetPriorScore(outerEdge.Key) * propagation_matrix[0, 1] * (sumMessage_Like / edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Like))
                        + ((1 - node_potential_like[node_id].GetPriorScore(outerEdge.Key)) * propagation_matrix[1, 1] * (sumMessage_Dislike / edge_list.bp_matrix[innerEdge.Key][outerEdge.Key].PreviousMessage_Dislike));
                    
                    // Normalization 하는 버젼
                    edge_list.bp_matrix[outerEdge.Key][innerEdge.Key].Message_Like = Message_Like / (Message_Like + Message_Dislike);
                    edge_list.bp_matrix[outerEdge.Key][innerEdge.Key].Message_Dislike = 1 - (Message_Like / (Message_Like + Message_Dislike));
                }
            }

            foreach (KeyValuePair<int, Dictionary<int, Edge>> row in edge_list.bp_matrix)
            {
                foreach (KeyValuePair<int, Edge> col in row.Value)
                {
                    edge_list.bp_matrix[row.Key][col.Key].PreviousMessage_Like = edge_list.bp_matrix[row.Key][col.Key].Message_Like;
                    edge_list.bp_matrix[row.Key][col.Key].PreviousMessage_Dislike = edge_list.bp_matrix[row.Key][col.Key].Message_Dislike;                    
                }
            }
        }

        public List<Score> ComputeBeliefScore(int node_id, HashSet<int> items, int num_recommend, Dictionary<int, HashSet<int>> correct_user_item)
        {
            HashSet<Score> temp_belief_score_list = new HashSet<Score>();
            List<Score> belief_score_list = new List<Score>();
            Score scores;

            for (int i = max_user_id + 1; i <= max_item_id + 1; i++)
            {
                double score_like = Compute(node_id, i);
                scores = new Score(i, score_like);

                temp_belief_score_list.Add(scores);
            }

            belief_score_list = temp_belief_score_list.ToList();
            belief_score_list.Sort(Score.CompareScore);
            
            return belief_score_list;
        }

        public double Compute(int target_user, int target_item)
        {
            double sumMessage_Like = 1d;
            double sumMessage_Dislike = 1d;

            double score_like = 0d;
            if (edge_list.bp_matrix.ContainsKey(target_item))
            {
                int mc = 0;
                foreach (KeyValuePair<int, Edge> temp_edge in edge_list.bp_matrix[target_item])
                {
                    sumMessage_Like = sumMessage_Like * edge_list.bp_matrix[temp_edge.Key][target_item].Message_Like;
                    sumMessage_Dislike = sumMessage_Dislike * edge_list.bp_matrix[temp_edge.Key][target_item].Message_Dislike;
                    mc++;

                    if (mc % 20 == 0)
                    {
                        sumMessage_Like *= 1000;
                        sumMessage_Dislike *= 1000;
                    }
                }
                double tempScoreLike = node_potential_like[target_user].GetPriorScore(target_item) * sumMessage_Like;
                double tempScoreDislike = (1 - node_potential_like[target_user].GetPriorScore(target_item)) * sumMessage_Dislike;
                // normalization
                score_like = tempScoreLike / (tempScoreLike + tempScoreDislike);
            }

            return score_like;
        }

        public void Clean(int node_id)
        {
            node_potential_like.Remove(node_id);
        }
    }
}
