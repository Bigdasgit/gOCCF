using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace OCCFRecSys
{
    class Program
    {
        public static string file_path;
        public static StreamReader sr;
        public static StreamWriter sw;
        public static HashSet<int> tophead_items;
        public static int num_user;
        //public static double[] betas = { 0.9 };
        //public static double[] gammas = { 0.9 };
        //public static double beta = 0d;
        //public static double gamma = 0d;

        static void Main(string[] args)
        {           
            string[] candidate_items = { "All_items" }; //  "All_items", "Longtail_items"
            string[] algorithms = { "SignedRWR" }; //"RWR", "SeparateRWR", "SignedRWR", "SeparateBP", "SignedBP"

            //file_path = "D:\\Downloads\\DataFile\\gOCCF\\Movielens\\";
            //num_user = 943;
            //foreach (string algo in algorithms)
            //    foreach (string candidate_item in candidate_items)
            //        Experiments(algo, candidate_item);

            file_path = "D:\\Downloads\\DataFile\\gOCCF\\Watcha\\";
            num_user = 1391;
            foreach (string algo in algorithms)
                foreach (string candidate_item in candidate_items)
                    Experiments(algo, candidate_item);


            //file_path = "D:\\Downloads\\DataFile\\Ciao\\";
            //num_user = 996;
            //foreach (string algo in algorithms)
            //    foreach (string candidate_item in candidate_items)
            //        Experiments(algo, candidate_item);

            //file_path = "D:\\Downloads\\DataFile\\CiteULike\\";
            //num_user = 5551;
            //foreach (string algo in algorithms)
            //    foreach (string candidate_item in candidate_items)
            //        Experiments(algo, candidate_item);


            //foreach (double beta in betas)
            //    foreach (double gamma in gammas)
            //        foreach (string algo in algorithms)
            //            foreach (string candidate_item in candidate_items)
            //            {
            //                Program.beta = beta;
            //                Program.gamma = gamma;
            //                Experiments(algo, candidate_item);
            //            }
        }

        public static void Experiments(string algo, string candidate_item)
        {         
            int num_recommend = 50;
            for (int fold = 1; fold <= 1; fold++)
            {
                if (candidate_item == "Longtail_items")
                {
                    tophead_items = new HashSet<int>();
                    sr = new StreamReader(file_path + "raw\\longtail\\u" + fold + "_longtail_items.txt");
                    while (!sr.EndOfStream)
                    {
                        int item_id = int.Parse(sr.ReadLine().ToString()) + num_user;
                        tophead_items.Add(item_id);
                    }
                    sr.Close();
                }

                StreamReader training = new StreamReader(file_path + "unint\\basic\\u" + fold + "\\u" + fold + "_balance.base");
                StreamReader test = new StreamReader(file_path + "raw\\basic\\u" + fold + "\\u" + fold + ".test");
                StreamWriter result = new StreamWriter(file_path + "results\\" + algo + "\\unint\\origin\\u" + fold + "_" + algo + "_rankresult_balance(" + candidate_item + ").results");
                StreamWriter time = new StreamWriter(file_path + "results\\" + algo + "\\unint\\origin\\u" + fold + "_" + algo + "_rankresult_balance(" + candidate_item + ").time");

                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (algo == "SeparateRWR")
                {
                    var recsys = new SeparateRWR_Recommender(candidate_item, num_recommend, fold, training, test);
                    recsys.Recommend();
                    recsys.PrintResults(result);
                }
                else if (algo == "SignedRWR")
                {
                    double beta = 0.9d;
                    double gamma = 0.9d;                   
                    var recsys = new SignedRWR_Recommender(candidate_item, num_recommend, fold, training, test, beta, gamma);
                    recsys.Recommend();
                    recsys.PrintResults(result);
                }
                else if (algo == "SeparateBP")
                {
                    int num_iter = 5;
                    double propagation_alpha = 0.0001d;
                    var recsys = new SeparateBP_Recommender(candidate_item, num_iter, num_recommend, propagation_alpha, fold, training, test);
                    recsys.Recommend();
                    recsys.PrintResults(result);
                }
                else if (algo == "SignedBP")
                {
                    int num_iter = 5;
                    double propagation_alpha = 0.0001d;
                    var recsys = new SignedBP_Recommender(candidate_item, num_iter, num_recommend, propagation_alpha, fold, training, test);
                    recsys.Recommend();
                    recsys.PrintResults(result);
                }
                else if (algo == "RWR")
                {
                    var recsys = new RWR_Recommender(candidate_item, num_recommend, fold, training, test);
                    recsys.Recommend();
                    recsys.PrintResults(result);
                }
                sw.Stop();
                time.WriteLine(algo + "\t" + sw.ElapsedMilliseconds.ToString() + "ms");
                Console.WriteLine(algo + "\t" + sw.ElapsedMilliseconds.ToString() + "ms");

                time.Close();
                result.Close();
            }
        }        
    }

    public class Pairs
    {
        public Pairs(int user_id, int item_id,double score)
        {
            this.user_id = user_id;
            this.item_id = item_id;
            this.score = score;
        }

        public int user_id;
        public int item_id;
        public double score;

        public static int CompareScore(Pairs a, Pairs b)
        {
            int result = a.score.CompareTo(b.score);

            return result;
        }
    }



}