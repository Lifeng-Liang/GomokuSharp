using System;
using System.Collections.Generic;

namespace Gomoku
{
    public class Board
    {
        public int width;
        public int height;
        public int size;
        public Dictionary<int, int> states;
        public int n_in_row;
        public int[] players;
        public List<int> availables;
        public List<int> moved;
        public int press_size;
        public int current_player;
        public int last_move;

        public Board(int width = 8, int height = 8, int nInRow = 5)
        {
            this.width = width;
            this.height = height;
            this.size = width*height;
            press_size = 0;
            this.states = new Dictionary<int, int>();
            this.n_in_row = nInRow;
            this.players = new[] {1, 2};
        }

        public void init_board(int startPlayer = 0)
        {
            if (this.width < this.n_in_row || this.height < this.n_in_row)
            {
                throw new Exception($"board width and height can not be less than {n_in_row}");
            }
            this.current_player = this.players[startPlayer];
            moved = new List<int>();
            this.availables = new List<int>();
            for (int i = 0; i < size; i++)
            {
                availables.Add(i);
            }
            this.states = new Dictionary<int, int>();
            this.last_move = -1;
        }

        public int[] move_to_location(int move)
        {
            var h = move;
            var w = move%width;
            return new[] {h, w};
        }

        public int location_to_move(int[] location)
        {
            if (location.Length != 2)
            {
                return -1;
            }
            var h = location[0];
            var w = location[1];
            var move = h*width + w;

            if (move < 0 || move >= size)
            {
                return -1;
            }
            return move;
        }

        public double[,,] current_state()
        {
            var squareState = new double[4, width, height];
            foreach (var s in states)
            {
                var index = s.Value == current_player ? 0 : 1;
                squareState[index, s.Key/width, s.Key%height] = 1.0;
            }
            squareState[2, last_move/width, last_move%height] = 1.0;
            if (states.Count%2 == 0)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        squareState[3, x, y] = 1.0;
                    }
                }
            }
            //TODO: reverse x
            return squareState;
        }

        public void do_move(int move)
        {
            this.states[move] = this.current_player;
            availables.Remove(move);
            moved.Add(move);
            press_size++;
            this.current_player = current_player == players[1] ? players[0] : players[1];
            this.last_move = move;
        }

        private bool check(int m, int n, int step = 1)
        {
            int len1 = 0, len2 = 0, empty = 0;
            for (int i = m; i < n; i += step)
            {
                int player;
                if (states.TryGetValue(i, out player))
                {
                    if (player == players[0])
                    {
                        len1 = 1;
                    }
                    else
                    {
                        len2 = 1;
                    }
                }
                else
                {
                    empty = 1;
                }
            }
            return len1 + len2 + empty == 1;
        }

        public Tuple<bool, int> has_a_winner()
        {
            var n = n_in_row;

            if (press_size < n_in_row + 2)
            {
                return Tuple.Create(false, -1);
            }
            foreach (int m in moved)
            {
                var h = m / width;
                var w = m % width;
                var player = states[m];
                var maxw = width - n + 1;
                var maxh = height - n + 1;
                if (w >= 0 && w < maxw && check(m, m + n))
                {
                    return Tuple.Create(true, player);
                }

                if (h >= 0 && h < maxh && check(m, m + n * width, width))
                {
                    return Tuple.Create(true, player);
                }

                if (w >= 0 && w < maxw && h >= 0 && h < maxh && check(m, m + n * (width + 1), width + 1))
                {
                    return Tuple.Create(true, player);
                }

                if (w >= n - 1 && w < width && h >= 0 && h < maxh && check(m, m + n * (width - 1), width - 1))
                {
                    return Tuple.Create(true, player);
                }
            }
            return Tuple.Create(false, -1);
        }

        public Tuple<bool, int> game_end()
        {
            var tp = has_a_winner();
            if (tp.Item1)
            {
                return Tuple.Create(true, tp.Item2);
            }
            if (press_size == size)
            {
                return Tuple.Create(true, -1);
            }
            return Tuple.Create(false, -1);
        }

        public int get_current_player()
        {
            return current_player;
        }

        public Board DeepCopy()
        {
            var b = new Board(width, height, n_in_row)
            {
                press_size = press_size,
                current_player = current_player,
                last_move = last_move,
                availables = new List<int>(),
                moved = new List<int>()
            };
            foreach (var i in moved)
            {
                b.moved.Add(i);
            }
            foreach (var i in availables)
            {
                b.availables.Add(i);
            }
            foreach (var state in states)
            {
                b.states[state.Key] = state.Value;
            }
            return b;
        }
    }
}
