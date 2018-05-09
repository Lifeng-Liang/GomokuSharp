using System;
using GomokuLib;

namespace PbrainMcts
{
    class Program
    {
        static void Main()
        {
            new GomocupEngine().main();
        }
    }

    class GomocupEngine : GomocupInterface
    {
        private Board _board;
        private Player _player = new MctsPlayer(5, 100000);
        const int MAX_BOARD = 100;
        int[,] board = new int[MAX_BOARD, MAX_BOARD];
        Random rand = new Random();

        public override string brain_about => "name=\"MCTS\", author=\"Lifeng\", version=\"1.0\", country=\"China\", www=\"https://github.com/Lifeng-Liang/GomokuSharp\"";

        public override void brain_init()
        {
            if (width < 5 || height < 5)
            {
                Response("ERROR size of the board");
                return;
            }
            if (width > MAX_BOARD || height > MAX_BOARD)
            {
                Response("ERROR Maximal board size is " + MAX_BOARD);
                return;
            }
            _board = new Board(width, height, 5);
            _board.init_board(0);
            Response("OK");
        }

        public override void brain_restart()
        {
            _board = new Board(width, height, 5);
            _board.init_board(0);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    board[x, y] = 0;

            Response("OK");
        }

        private bool isFree(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height && board[x, y] == 0;
        }

        public override void brain_my(int x, int y)
        {
            if (isFree(x, y))
            {
                board[x, y] = 1;
                _board.do_move(x * width + y);
            }
            else
            {
                Response($"ERROR my move [{x},{y}]");
            }
        }

        public override void brain_opponents(int x, int y)
        {
            if (isFree(x, y))
            {
                board[x, y] = 2;
                _board.do_move(x * width + y);
            }
            else
            {
                Response($"ERROR opponents's move [{x},{y}]");
            }
        }

        public override void brain_block(int x, int y)
        {
            if (isFree(x, y))
            {
                board[x, y] = 3;
            }
            else
            {
                Response($"ERROR winning move [{x},{y}]");
            }
        }

        public override int brain_takeback(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height && board[x, y] != 0)
            {
                board[x, y] = 0;
                return 0;
            }
            return 2;
        }

        public override void brain_turn()
        {
            var ap = _player.get_action(_board);
            var x = ap.Item1/width;
            var y = ap.Item1%width;
            do_mymove(x, y);
        }

        public override void brain_end()
        {
        }

        public override void brain_eval(int x, int y)
        {
        }
    }
}
