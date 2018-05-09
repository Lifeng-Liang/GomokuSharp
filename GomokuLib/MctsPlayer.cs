using System;
using System.Collections.Generic;

namespace GomokuLib
{
    public class MctsPlayer : Player
    {
        protected Mcts mcts;

        public MctsPlayer(int cPuct = 5, int nPlayout = 2000)
            : this(new Mcts(policy_value_fn, cPuct, nPlayout))
        {
        }

        protected MctsPlayer(Mcts mcts)
        {
            this.mcts = mcts;
        }

        public override Tuple<int, double[]> get_action(Board board, double temp = 0.001, bool returnProb = false)
        {
            if (board.availables.Count > 0)
            {
                var move = mcts.get_move(board);
                mcts.update_with_move(-1);
                return Tuple.Create<int, double[]>(move, null);
            }
            Console.WriteLine("WARNING: the board is full");
            return Tuple.Create<int, double[]>(-1, null);
        }

        public override void reset_player()
        {
            mcts.update_with_move(-1);
        }

        public override string ToString()
        {
            return $"MCTS {player}";
        }

        private static Tuple<IEnumerable<Tuple<int, double>>, double> policy_value_fn(Board board)
        {
            var n = board.availables.Count;
            var actionProbs = board.availables.Count.NewArray(() => 1.0 / n);
            return Tuple.Create(Ext.zip(board.availables, actionProbs), 0.0);
        }
    }
}
