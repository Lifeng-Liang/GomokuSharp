using System;
using System.Collections.Generic;

namespace GomokuLib
{
    public abstract class PolicyValueNet
    {
        protected int Width;
        protected int Height;

        protected PolicyValueNet(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public abstract Tuple<IList<IList<double>>, IList<IList<double>>> policy_value(IEnumerable<double[,,]> stateBatch);

        public abstract Tuple<IEnumerable<Tuple<int, double>>, double> policy_value_fn(Board board);

        public abstract Tuple<double, double> train_step(IEnumerable<Tuple<double[,,], double[], double>> miniBatch, double lr);

        public abstract void save_model(string modelFileName);
    }
}
