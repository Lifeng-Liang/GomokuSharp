using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GomokuLib
{
    public class ZeroPlayer : MctsPlayer
    {
        private readonly bool _isSelfplay;

        public ZeroPlayer(Func<Board, Tuple<IEnumerable<Tuple<int, double>>, double>> fn, int cPuct = 5, int nPlayout = 2000, bool isSelfplay = false)
            : base(new ZeroMcts(fn, cPuct, nPlayout))
        {
            _isSelfplay = isSelfplay;
        }

        public override Tuple<int, double[]> get_action(Board board, double temp = 0.001, bool returnProb = false)
        {
            var timer = new Stopwatch();
            timer.Start();
            if (board.availables.Count > 0)
            {
                var ap = ((ZeroMcts)mcts).get_move_probs(board, temp); // acts, probs
                var acts = ap.Item1;
                var probs = ap.Item2;

                var moveProbs = new double[board.width*board.height];
                for (int i = 0; i < acts.Count; i++)
                {
                    moveProbs[acts[i]] = probs[i];
                }

                int move;
                if (_isSelfplay)
                {
                    var pp = probs.Count.NewArray(() => 0.3);
                    move = Ext.choice(acts, GetPros(probs, dirichlet(pp)));
                    mcts.update_with_move(move);
                }
                else
                {
                    move = Ext.choice(acts, probs);
                    mcts.update_with_move(-1);
                }
                timer.Stop();
                Console.WriteLine($"MCTSPlayer Elapsed : {timer.Elapsed}");
                return Tuple.Create(move, returnProb ? moveProbs : null);
            }
            Console.WriteLine("WARNING: the board is full");
            return Tuple.Create<int, double[]>(-1, null);
        }

        private static IEnumerable<double> GetPros(List<double> probs, double[] diri)
        {
            var size = probs.Count;
            for (int i = 0; i < size; i++)
            {
                yield return 0.75*probs[i] + 0.25*diri[i];
            }
        }

        private double[] dirichlet(double[] alpha)
        {
            var totsize = alpha.Length;
            var valData = new double[totsize];
            var i = 0;
            while(i < totsize)
            {
                var acc = 0.0;
                for (int j = 0; j < totsize; j++)
                {
                    valData[i + j] = rk_standard_gamma(alpha[j]);
                    acc = acc + valData[i + j];
                }
                var invacc = 1/acc;
                for (int j = 0; j < totsize; j++)
                {
                    valData[i + j] = valData[i + j]*invacc;
                }
                i = i + totsize;
            }
            return valData;
        }

        private double rk_standard_gamma(double i)
        {
            //TODO:
            return i * Ext.Rand.NextDouble();
        }
    }
}
