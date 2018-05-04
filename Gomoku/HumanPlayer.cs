using System;

namespace Gomoku
{
    public class HumanPlayer : Player
    {
        public override Tuple<int, object> get_action(Board board, double temp = 0.001, bool returnProb = false)
        {
            int move;
            try
            {
                Console.Write("Your move: ");
                var loc = Console.ReadLine();
                var ss = loc.Split(',');
                var location = new [] {int.Parse(ss[0]), int.Parse(ss[1])};
                move = board.location_to_move(location);
            }
            catch (Exception)
            {
                move = -1;
            }
            if (move == -1 || !board.availables.Contains(move))
            {
                Console.WriteLine("invalid move");
                move = get_action(board).Item1;
            }
            return Tuple.Create<int, object>(move, null);
        }

        public override void reset_player()
        {
        }

        public override string ToString()
        {
            return $"Human {player}";
        }
    }
}
