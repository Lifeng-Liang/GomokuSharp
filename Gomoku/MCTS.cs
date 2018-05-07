using System;
using System.Collections.Generic;

namespace Gomoku
{
    public class MCTS
    {
        private TreeNode _root;
        private Func<Board, Tuple<IEnumerable<Tuple<int, double>>, int>> _policy;
        private int _c_puct;
        private int _n_playout;

        public MCTS(Func<Board, Tuple<IEnumerable<Tuple<int, double>>, int>> policyValueFn, int cPuct = 5, int nPlayout = 10000)
        {
            _root = new TreeNode(null, 1.0);
            _policy = policyValueFn;
            _c_puct = cPuct;
            _n_playout = nPlayout;
        }

        private void _playout(Board state)
        {
            var node = _root;
            while (true)
            {
                if (node.is_leaf())
                {
                    break;
                }
                // Greedily select next move.
                var kv = node.select(_c_puct);
                node = kv.Value;
                state.do_move(kv.Key);
            }

            var actionProbs = _policy(state).Item1;
            // Check for end of game
            var end = state.game_end().Item1;
            if (!end)
            {
                node.expand(actionProbs);
            }
            // Evaluate the leaf node by random rollout
            var leafValue = _evaluate_rollout(state);
            // Update value and visit count of nodes in this traversal.
            node.update_recursive(-leafValue);
        }

        private int _evaluate_rollout(Board state, int limit = 1000)
        {
            var player = state.get_current_player();
            for (int i = 0; i < limit; i++)
            {
                var ew = state.game_end();
                if (ew.Item1)
                {
                    if (ew.Item2 == -1)
                    {
                        return 0;
                    }
                    return ew.Item2 == player ? 1 : -1;
                }
                var maxAction = state.availables[Ext.Rand.Next(state.availables.Count)];
                //var actionProbs = rollout_policy_fn(state);
                //var maxAction = actionProbs.Max(p => p.Item2).Item1;
                state.do_move(maxAction);
            }
            Console.WriteLine("WARNING: rollout reached move limit");
            return 0;
        }

        private IEnumerable<Tuple<int,double>> rollout_policy_fn(Board board)
        {
            var actionProbs = board.availables.Count.NewArray(() => Ext.Rand.NextDouble());
            return Ext.zip(board.availables, actionProbs);
        }

        public int get_move(Board state)
        {
            for (var n=0; n<_n_playout; n++)
            {
                var stateCopy = state.DeepCopy();
                _playout(stateCopy);
            }
            return _root._children.Max(n => n.Value._n_visits).Key;
        }

        public void update_with_move(int lastMove)
        {
            TreeNode node;
            if (_root._children.TryGetValue(lastMove, out node))
            {
                _root = node;
                _root._parent = null;
            }
            else
            {
                _root = new TreeNode(null, 1.0);
            }
        }

        public override string ToString()
        {
            return "MCTS";
        }
    }
}
