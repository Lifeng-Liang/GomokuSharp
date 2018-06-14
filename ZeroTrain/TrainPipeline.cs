using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GomokuLib;

namespace ZeroTrain
{
    public class TrainPipeline
    {
        private int board_width;
        private int board_height;
        private int n_in_row;
        private Board board;
        private Game game;
        private double learn_rate;
        private double lr_multiplier;
        private double temp;
        private int n_playout;
        private int c_puct;
        private int buffer_size;
        private int batch_size;
        private List<Tuple<double[,,], double[], double>> data_buffer;
        private int play_batch_size;
        private int epochs;
        private double kl_targ;
        private int check_freq;
        private int game_batch_num;
        private int best_win_ratio;
        private int pure_mcts_playout_num;
        private Stopwatch timer;
        private PolicyValueNet policy_value_net;
        private Player mcts_player;
        private int episode_len;

        public TrainPipeline(int width, int nInRow)
        {
            this.board_width = width;
            this.board_height = width;
            this.n_in_row = nInRow;
            this.board = new Board(board_width, board_height, n_in_row);
            this.game = new Game(board);
            this.learn_rate = 2e-3;
            this.lr_multiplier = 1.0;  // adaptively adjust the learning rate based on KL
            this.temp = 1.0;  // the temperature param
            this.n_playout = 10000;  // num of simulations for each move
            this.c_puct = 5;
            this.buffer_size = 10000;
            this.batch_size = 512;  // mini-batch size for training
            this.data_buffer = new List<Tuple<double[,,], double[], double>>(buffer_size);
            this.play_batch_size = 1;
            this.epochs = 5;  // num of train_steps for each update
            this.kl_targ = 0.02;
            this.check_freq = 50;
            this.game_batch_num = 1500;
            this.best_win_ratio = 0;
            this.pure_mcts_playout_num = 1000;
            this.timer = new Stopwatch();
            timer.Start();
            this.policy_value_net = new PolicyValueNetCntk(board_width, board_height);
            this.mcts_player = new ZeroPlayer(policy_value_net.policy_value_fn, c_puct, n_playout, true);
        }

        public void Run()
        {
            try
            {
                for (int i = 0; i < game_batch_num; i++)
                {
                    this.collect_selfplay_data(play_batch_size);
                    Console.Write($"epoch:{i+1}, epi_len:{episode_len}, ");
                    if(data_buffer.Count > batch_size) {
                        this.policy_update();
                    }
                    if ((i + 1) % this.check_freq == 0)
                    {
                        Console.WriteLine($"current self-play batch: {i + 1}");
                        var winRatio = this.policy_evaluate();
                        this.policy_value_net.save_model("./current_policy.model");
                        if (winRatio > this.best_win_ratio)
                        {
                            Console.WriteLine("New best policy!!!!!!!!");
                            this.best_win_ratio = winRatio;
                            this.policy_value_net.save_model("./best_policy.model");
                            if (this.best_win_ratio == 1000 && this.pure_mcts_playout_num < 5000)
                            {
                                this.pure_mcts_playout_num += 1000;
                                this.best_win_ratio = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void collect_selfplay_data(int nGames = 1)
        {
            for (int i = 0; i < nGames; i++)
            {
                var playData = game.start_self_play(mcts_player, false, temp).Item2;
                this.episode_len = playData.Count;
                var playData2 = get_equi_data(playData);
                this.data_buffer.AddRange(playData2);
            }
        }

        private List<Tuple<double[,,], double[], double>> get_equi_data(List<Tuple<double[,,], double[], double>> play_data)
        {
            var extendData = new List<Tuple<double[,,], double[], double>>();
            foreach (var tp in play_data) { // state, mcts_porb, winner
	            for(int i=1; i<=4; i++) {
		            // rotate counterclockwise
		            var equiState = tp.Item1.rot90(i);
	                var equiMctsProb = tp.Item2.reshape(board_height, board_width).flipud().rot90(i);
		            extendData.Add(Tuple.Create(equiState, equiMctsProb.flipud().flatten().ToArray(), tp.Item3));
		            // flip horizontally
		            equiState = equiState.fliplr();
	                equiMctsProb = equiMctsProb.fliplr();
	                extendData.Add(Tuple.Create(equiState, equiMctsProb.flipud().flatten().ToArray(), tp.Item3));
	            }
            }
            return extendData;
        }

        private void policy_update()
        {
            var miniBatch = data_buffer.sample(batch_size).ToList();
            var stateBatch = miniBatch.Map(p => p.Item1).ToList();
            var winnerBatch = miniBatch.Map(p => p.Item3).ToArray();
            var oldProbsOldV = policy_value_net.policy_value(stateBatch);
            var oldProbs = oldProbsOldV.Item1;
            var oldV = oldProbsOldV.Item2;
            double kl = 0.0;
            double loss = 0.0;
            double entropy = 0.0;
            IList<IList<double>> newProbs = null;
            IList<IList<double>> newV = null;
            for (int i=0; i<epochs; i++) {
	            var lossEntropy = policy_value_net.train_step(miniBatch, learn_rate*lr_multiplier);
                loss = lossEntropy.Item1;
                entropy = lossEntropy.Item2;
	            var newProbsNewV = policy_value_net.policy_value(stateBatch);
                newProbs = newProbsNewV.Item1;
                newV = newProbsNewV.Item2;
                kl = ProbsMean(oldProbs, newProbs);
                if (kl > kl_targ * 4) {
		            break;
	            }
            }
            // adaptively adjust the learning rate
            if (kl > kl_targ*2 && lr_multiplier > 0.1)
            {
                lr_multiplier /= 1.5;
            }
            else if (kl < kl_targ/2 && lr_multiplier < 10)
            {
                lr_multiplier *= 1.5;
            }

            var explainedVarOld = 1 - winnerBatch.sub(oldV.flatten().ToArray()).variance() / winnerBatch.variance();
            var explainedVarNew = 1 - winnerBatch.sub(newV.flatten().ToArray()).variance() / winnerBatch.variance();
            
            var tdura = timer.Elapsed;
            timer.Restart();
            Console.WriteLine($"kl:{kl}, lr_mp:{lr_multiplier}, loss:{loss}, entropy:{entropy}, var_old:{explainedVarOld}, var_new:{explainedVarNew}, dura:{tdura}");
        }

        private double ProbsMean(IList<IList<double>> oldProbs, IList<IList<double>> newProbs)
        {
            // kl = np.mean(np.sum(old_probs * (np.log(old_probs + 1e-10) - np.log(new_probs + 1e-10)), axis = 1))
            double sum = 0;
            int count = 0;
            for (int y = 0; y < oldProbs.Count; y++)
            {
                count ++;
                var xx = oldProbs[y].Count;
                for (int x = 0; x < xx; x++)
                {
                    var op = oldProbs[y][x];
                    var np = newProbs[y][x];
                    var item = op * (Math.Log(op + 1e-10) - Math.Log(np + 1e-10));
                    sum += item;
                }
            }
            return sum/count;
        }

        private int policy_evaluate(int nGames = 10)
        {
            var currentMctsPlayer = new ZeroPlayer(policy_value_net.policy_value_fn, c_puct, n_playout);
            var pureMctsPlayer = new MctsPlayer(5, pure_mcts_playout_num);
            var winCnt = new int[3];
            for (int i = 0; i < nGames; i++)
            {
                var winner = game.start_play(currentMctsPlayer, pureMctsPlayer, i % 2, false);
                winCnt[winner == -1 ? 0 : winner]++;
            }
            var winRatio = 1.0 * (winCnt[1] + 0.5 * winCnt[0]) / nGames;
            Console.WriteLine($"num_playouts:{pure_mcts_playout_num}, win: {winCnt[1]}, lose: {winCnt[2]}, tie:{winCnt[0]}");
            return (int)(winRatio * 1000);
        }
    }
}
