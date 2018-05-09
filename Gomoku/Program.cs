using GomokuLib;

namespace Gomoku
{
    class Program
    {
        static void Main(string[] args)
        {
            const int w = 12;
            var mcts = new MctsPlayer(5, 100000);
            var human = new HumanPlayer();
            var game = new Game(new Board(w, w, 5));
            game.start_play(mcts, human, 0);
        }
    }
}
