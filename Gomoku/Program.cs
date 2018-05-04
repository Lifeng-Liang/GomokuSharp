namespace Gomoku
{
    class Program
    {
        static void Main(string[] args)
        {
            const int w = 12;
            var player1 = new MCTSPlayer(5, 10000);
            var player2 = new HumanPlayer();
            var game = new Game(new Board(w, w, 5));
            game.start_play(player1, player2, 0);
        }
    }
}
