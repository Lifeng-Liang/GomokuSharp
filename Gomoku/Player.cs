using System;

namespace Gomoku
{
    public abstract class Player
    {
        public int player { get; private set; }

        public void set_player_ind(int p)
        {
            player = p;
        }

        public abstract Tuple<int, object> get_action(Board board, double temp= 0.001, bool returnProb= false);

        public abstract void reset_player();
    }
}
