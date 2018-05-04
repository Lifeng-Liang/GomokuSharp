using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gomoku
{
    public class MCTSPlayer : Player
    {
        private MCTS mcts;

        public MCTSPlayer(int cPuct = 5, int nPlayout = 2000)
        {
            mcts = new MCTS(policy_value_fn, cPuct, nPlayout);
        }

        public override Tuple<int, object> get_action(Board board, double temp = 0.001, bool returnProb = false)
        {
            var timer = new Stopwatch();
            timer.Start();
            if (board.availables.Count > 0)
            {
                var move = mcts.get_move(board);
                mcts.update_with_move(-1);
                timer.Stop();
                Console.WriteLine($"MCTSPlayer Elapsed : {timer.Elapsed}");
                return Tuple.Create<int, object>(move, null);
            }
            Console.WriteLine("WARNING: the board is full");
            return Tuple.Create<int, object>(-1, null);
        }

        public override void reset_player()
        {
            mcts.update_with_move(-1);
        }

        public override string ToString()
        {
            return $"MCTS {player}";
        }

        private static Tuple<IEnumerable<Tuple<int, double>>, int> policy_value_fn(Board board)
        {
            var n = board.availables.Count;
            var actionProbs = board.availables.Count.NewArray(() => 1.0/n);
            return Tuple.Create(Ext.zip(board.availables, actionProbs), 0);
        }
    }
}
