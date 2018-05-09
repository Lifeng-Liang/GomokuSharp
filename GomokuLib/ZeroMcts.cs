using System;
using System.Collections.Generic;

namespace GomokuLib
{
    public class ZeroMcts : Mcts
    {
        public ZeroMcts(Func<Board, Tuple<IEnumerable<Tuple<int, double>>, double>> policyValueFn, int cPuct = 5, int nPlayout = 10000)
            : base(policyValueFn, cPuct, nPlayout)
        {}

        protected override double Evaluate(Board state, TreeNode node)
        {
            var ap_lv = _policy(state); // action_probs, leaf_value
            var leafValue = ap_lv.Item2;
            var ew = state.game_end(); // end, winner
            if (!ew.Item1)
            {
                node.expand(ap_lv.Item1);
            }
            else
            {
                // for end state，return the "true" leaf_value
                leafValue = ew.Item2 == -1 ? 0.0 : (ew.Item2 == state.get_current_player() ? 1.0 : -1.0);
            }
            return leafValue;
        }

        public Tuple<List<int>, List<double>> get_move_probs(Board state, double temp = 1e-3)
        {
            for (var n = 0; n < _n_playout; n++)
            {
                var stateCopy = state.DeepCopy();
                _playout(stateCopy);
            }
            var acts = new List<int>();
            var visits = new List<double>();
            var max = double.MinValue;
            foreach (var kv in _root._children)
            {
                acts.Add(kv.Key);
                var visit = 1.0/temp*Math.Log(kv.Value._n_visits + 1e-10);
                visits.Add(visit);
                if (visit > max)
                {
                    max = visit;
                }
            }
            var actProbs = softmax(visits, max);
            return Tuple.Create(acts, actProbs);
        }

        private List<double> softmax(IEnumerable<double> list, double max)
        {
            var temp = new List<double>();
            var sum = 0.0;
            foreach (var visit in list)
            {
                var prob = Math.Exp(visit - max);
                sum += prob;
                temp.Add(prob);
            }
            var probs = new List<double>();
            foreach (var item in temp)
            {
                probs.Add(item/sum);
            }
            return probs;
        }

        public override string ToString()
        {
            return "ZERO";
        }
    }
}
