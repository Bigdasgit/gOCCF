using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class RWR_Recommender : Recommender
    {
        int fold;
        string candidate_item;
        // RWR variables
        double e, damping_factor;
        public int user_size, num_neighbor;                             // graph의 user 수, graph 생성 시 파라미터 (이웃 수)
        List<WeightedLink> links;                                       // graph의 links
        Dictionary<int, List<Score>> recommend_user_item;               // 타겟 유저들에게 추천할 아이템들과 점수 저장

        public RWR_Recommender(string candidate_item, int num_recommend, int fold, StreamReader training, StreamReader test)
        {
            this.candidate_item = candidate_item;
            this.fold = fold;

            Init(num_recommend, training, test);
        }

        public override void Init(int num_recommend, StreamReader training, StreamReader test)
        {
            num_neighbor = 50;  // graph 생성 시 파라미터 (이웃 수)
            // RWR variables
            e = 0.0d;
            damping_factor = 0.85d;
            this.num_recommend = num_recommend;

            // test set 읽기
            ReadTestSet(test, candidate_item);
            test.Close();
            Console.WriteLine("Read test set: Complete");

            // training set 읽기
            ReadTrainingSet(training);
            training.Close();
            Console.WriteLine("Read training set: Complete");
        }

        /// <summary>
        /// training set으로 사용할 links를 읽어옴
        /// </summary>
        /// <param name="sr">training set 파일 경로</param>
        public override void ReadTrainingSet(StreamReader sr)
        {
            links = new List<WeightedLink>();

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int user_id = int.Parse(line[0].ToString());
                int item_id = int.Parse(line[1].ToString()) + Program.num_user;
                //int rating = int.Parse(line[2].ToString());

                links.Add(new WeightedLink(user_id, item_id, 1)); // weight 추가시: double.Parse(line[2])
            }
        }

        public override void Recommend()
        {
            // RWR 점수 계산
            recommend_user_item = new Dictionary<int, List<Score>>();
            // 그래프 모델링 및 분석
            WeightedUndirectedGraph model_graph = new WeightedUndirectedGraph(links, e);
            user_size = model_graph.user_size;

            for (int i = 1; i < user_size; i++)
            {
                // 해당 사용자가 test user가 아니면 스킵
                if (!correct_user_item.ContainsKey(i)) continue;

                else if (i % 50 == 0)
                    Console.WriteLine("{0} Recommendation: user {1} / {2}", fold, i, (user_size - 1));

                List<Score> recommend_items = PerformRWR(model_graph, i);
                recommend_user_item.Add(i, recommend_items);
            }
            Console.WriteLine("Compute RWR scores of {0}: Complete", fold);
        }

        /// <summary>
        /// user-item 형태로 모델링된 bipartite graph를 사용하여 모든 test user에 대해 RWR 수행 
        /// </summary>
        /// <param name="model_graph">user-item에 대한 bipartite graph</param>
        /// <param name="sw">추천 결과(사용자 id, 아이템 id, 점수)가 출력될 파일</param>
        public List<Score> PerformRWR(WeightedUndirectedGraph model_graph, int target)
        {
            // test user가 기존에 갖고 있는 이력 리스트를 저장
            HashSet<int> i_items = new HashSet<int>();
            if (model_graph.graph.ContainsKey(target))
            {
                HashSet<Weight> i_weight = model_graph.graph[target];
                foreach (Weight items in i_weight)
                    i_items.Add(items.id);
            }

            // RWR 수행
            RWR ranker = new RWR(model_graph, damping_factor, target);
            List<Weight> score = ranker.Calculate();

            // RWR 점수를 기준으로 타겟 사용자가 선호할 만한 TOP N개의 아이템 출력
            List<Score> recommend_items = new List<Score>();
            //Dictionary<int, double> recommend_items = new Dictionary<int, double>();
            foreach (Weight s in score)
            {
                // ID가 user ID 이거나, 추천 아이템이 타겟 사용자의 기존 이력에 포함되어 있으면 TOP N에 포함시키지 않음  
                if (s.id < user_size || i_items.Contains(s.id)) continue;
                else if (recommend_items.Count < num_recommend)
                {
                    if (candidate_item == "Longtail_items")
                        if (Program.tophead_items.Contains(s.id)) continue;

                    Score user_score = new Score(s.id, s.w);
                    recommend_items.Add(user_score);
                }
                else
                    break;
            }

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
