using System;
using System.Collections.Generic;

namespace Gomoku
{
    public class Game
    {
        public Board board;

        public Game(Board board)
        {
            this.board = board;
        }

        public void graphic(Board board, int player1, int player2)
        {
            var width = board.width;
            var height = board.height;

            Console.WriteLine($"Player {player1} with X");
            Console.WriteLine($"Player {player2} with O");
            Console.WriteLine();
            Console.Write("\t");
            for (int x=0; x<width; x++)
            {
                Console.Write($"\t{x}");
            }
            Console.WriteLine("\r\n");
            for (int i = height - 1; i > -1; i -= 1)
            {
                Console.Write($"\t{i}");
                for (int j = 0; j < width; j++)
                {
                    var loc = i * width + j;
                    var p = board.states[loc];
                    if (p == player1)
                    {
                        Console.Write("\tX");
                    }
                    else if (p == player2)
                    {
                        Console.Write("\tO");
                    }
                    else
                    {
                        Console.Write("\t_");
                    }
                }
                Console.WriteLine("\r\n\r\n");
            }
        }

        public int start_play(Player player1, Player player2, int startPlayer = 0, bool isShown = true)
        {
            if (startPlayer < 0 || startPlayer > 1)
            {
                throw new Exception("start_player should be either 0 (player1 first) or 1 (player2 first)");
            }
            board.init_board(startPlayer);
            var p1 = board.players[0];
            var p2 = board.players[1];
            player1.set_player_ind(p1);
            player2.set_player_ind(p2);
            var players = new Dictionary<int, Player> {{p1, player1}, {p2, player2}};
            if (isShown)
            {
                graphic(board, player1.player, player2.player);
            }
            while (true)
            {
                var currentPlayer = board.get_current_player();
                var playerInTurn = players[currentPlayer];
                var move = playerInTurn.get_action(board);
                board.do_move(move.Item1);
                if (isShown)
                {
                    graphic(board, player1.player, player2.player);
                }
                var ew = board.game_end();
                if (ew.Item1)
                {
                    if (isShown)
                    {
                        if (ew.Item2 != -1)
                        {
                            Console.WriteLine($"Game end. Winner is {players[ew.Item2]}");
                        }
                        else
                        {
                            Console.WriteLine("Game end. Tie");
                        }
                    }
                    return ew.Item2;
                }
            }
        }

        public Tuple<int, List<object[]>> start_self_play(Player player, bool isShown= false, double temp= 0.001)
        {
            board.init_board();
            var p1 = board.players[0];
		    var p2 = board.players[1];
            var states = new List<double[,,]>();
            var mctsProbs = new List<object>();
            var currentPlayers = new List<int>();
            while(true) {
                var mp = player.get_action(board, temp, true);
                // store the data
                states.Add(board.current_state());
                mctsProbs.Add(mp.Item2);
                currentPlayers.Add(board.current_player);
                // perform a move
                board.do_move(mp.Item1);
                if(isShown) {
                    graphic(board, p1, p2);
			    }
                var ew = board.game_end();
                if(ew.Item1) {
                    // winner from the perspective of the current player of each state
                    var winnersZ = new double[currentPlayers.Count];
                    if(ew.Item2 != -1) {
                        for (int i = 0; i < winnersZ.Length; i++)
                        {
                            winnersZ[i] = currentPlayers[i] == ew.Item2 ? 1.0 : -1.0;
                        }
				    }
                    // reset MCTS root node
                    player.reset_player();
                    if (isShown) {
                        if(ew.Item2 != -1) {
                            Console.WriteLine($"Game end. Winner is player: {ew.Item2}");
					    }
                        else {
                            Console.WriteLine("Game end. Tie");
					    }
				    }
                    return Tuple.Create(ew.Item2, Ext.zip(states, mctsProbs, winnersZ));
                }
		    }
        }
    }
}
