using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class BPMatrix
    {
        public Dictionary<int, Dictionary<int, Edge>> bp_matrix = new Dictionary<int, Dictionary<int, Edge>>();

        public bool SetAt(int row, int col, Edge element)
        {
            if (bp_matrix.ContainsKey(row))
            {
                if (bp_matrix[row].ContainsKey(col))
                    return false;
                else
                {
                    bp_matrix[row].Add(col, element);
                    return true;
                }
            }
            else
            {
                Dictionary<int, Edge> temp_dict = new Dictionary<int, Edge>();
                temp_dict.Add(col, element);
                bp_matrix.Add(row, temp_dict);
                List<int> temp_list = new List<int>();
                temp_list.Add(col);
                return true;
            }
        }

        public Edge GetAt(int row, int col)
        {
            if (bp_matrix.ContainsKey(row))
            {
                if (bp_matrix[row].ContainsKey(col))
                {
                    return bp_matrix[row][col];
                }
                else
                    return null;
            }
            else
                return null;
        }

        public bool RemoveAt(int row, int col)
        {
            if (bp_matrix.ContainsKey(row))
            {
                if (bp_matrix[row].ContainsKey(col))
                {
                    bp_matrix[row].Remove(col);
                    if (bp_matrix[row].Count() == 0)
                    {
                        bp_matrix.Remove(row);
                    }
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public void Clear()
        {
            foreach (KeyValuePair<int, Dictionary<int, Edge>> row in bp_matrix)
                foreach (KeyValuePair<int, Edge> col in row.Value)
                {
                    bp_matrix[row.Key][col.Key].Message_Like = 1d;
                    bp_matrix[row.Key][col.Key].Message_Dislike = 1d;
                    bp_matrix[row.Key][col.Key].PreviousMessage_Like = 1d;
                    bp_matrix[row.Key][col.Key].PreviousMessage_Dislike = 1d;
                }
        }
    }
}
