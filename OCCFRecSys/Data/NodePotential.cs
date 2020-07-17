using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class NodePotential
    {
        public HashSet<int> linkednode_list = new HashSet<int>();
        public Dictionary<int, double> prior_score = new Dictionary<int, double>();

        public bool AddNode(int node_id)
        {
            if (linkednode_list.Contains(node_id))
                return false;
            else
            {
                linkednode_list.Add(node_id);
                return true;
            }
        }

        public void SetPriorScore(int node_id, double score)
        {
            if (!prior_score.ContainsKey(node_id))
                prior_score.Add(node_id, score);
        }

        public double GetPriorScore(int node_id)
        {
            return prior_score[node_id];
        }

        public bool haveNode(int node_id)
        {
            return linkednode_list.Contains(node_id);
        }

    }
}
