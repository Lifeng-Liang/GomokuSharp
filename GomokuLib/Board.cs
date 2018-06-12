using System;
using System.Collections.Generic;

namespace GomokuLib
{
    public class Board
    {
        public readonly int width;
        public readonly int height;
        public readonly int size;
        protected int[] states;
        public int n_in_row;
        public int[] players;
        public List<int> availables;
        public int moved_size;
        public int current_player;
        public int last_move;

        public Board(int width = 8, int height = 8, int nInRow = 5)
        {
            this.width = width;
            this.height = height;
            this.size = width*height;
            moved_size = 0;
            this.states = new int[size];
            this.n_in_row = nInRow;
            this.players = new[] {1, 2};
        }

        public virtual void init_board(int startPlayer = 0)
        {
            if (this.width < this.n_in_row || this.height < this.n_in_row)
            {
                throw new Exception($"board width and height can not be less than {n_in_row}");
            }
            this.current_player = this.players[startPlayer];
            this.availables = new List<int>();
            for (int i = 0; i < size; i++)
            {
                availables.Add(i);
            }
            this.states = new int[size];
            this.last_move = -1;
        }

        public int GetState(int move)
        {
            return states[move];
        }

        public int[] move_to_location(int move)
        {
            var h = move/width;
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
            int count = 0;
            for (int i = 0; i < size; i++)
            {
                var p = states[i];
                if (p != 0)
                {
                    count++;
                    var index = p == current_player ? 0 : 1;
                    squareState[index, i / width, i % height] = 1.0;
                }
            }
            squareState[2, last_move/width, last_move%height] = 1.0;
            if (count%2 == 0)
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

        public virtual void do_move(int move)
        {
            this.states[move] = this.current_player;
            availables.Remove(move);
            moved_size++;
            this.current_player = current_player == players[1] ? players[0] : players[1];
            this.last_move = move;
        }

        public Tuple<bool, int> has_a_winner()
        {
            if (moved_size < n_in_row + 2)
            {
                return Tuple.Create(false, -1);
            }
            return Win();
        }

        protected virtual Tuple<bool, int> Win()
        {
            var h = last_move/width;
            var w = last_move%width;
            var p = current_player == players[1] ? players[0] : players[1];
            if (check(last_move - w, last_move + width - w - 1, 1, p))
            {
                return Tuple.Create(true, p);
            }
            if (check(last_move - h*width, size - 1, width, p))
            {
                return Tuple.Create(true, p);
            }
            var x = Math.Min(h, w);
            var y = width - w - 1;
            if (check(last_move - x*width - x, Math.Min(size - 1, last_move + y*width + y), width + 1, p))
            {
                return Tuple.Create(true, p);
            }
            x = Math.Min(h, width - w - 1);
            if (check(last_move - x*width + x, Math.Min(size - 1, last_move + w*width - w), width - 1, p))
            {
                return Tuple.Create(true, p);
            }
            return Tuple.Create(false, -1);
        }

        private bool check(int start, int end, int step, int player)
        {
            int count = 0;
            for (int i = start; i <= end; i+=step)
            {
                if (states[i] == player)
                {
                    count++;
                    if (count >= n_in_row)
                    {
                        return true;
                    }
                }
                else
                {
                    count = 0;
                }
            }
            return false;
        }

        public Tuple<bool, int> game_end()
        {
            var tp = has_a_winner();
            if (tp.Item1)
            {
                return Tuple.Create(true, tp.Item2);
            }
            if (moved_size == size)
            {
                return Tuple.Create(true, -1);
            }
            return Tuple.Create(false, -1);
        }

        public int get_current_player()
        {
            return current_player;
        }

        public virtual Board DeepCopy()
        {
            var b = new Board(width, height, n_in_row)
            {
                moved_size = moved_size,
                current_player = current_player,
                last_move = last_move,
                availables = new List<int>(),
            };
            foreach (var i in availables)
            {
                b.availables.Add(i);
            }
            for (int i = 0; i < size; i++)
            {
                b.states[i] = states[i];
            }
            return b;
        }
    }

    public class OldBoard : Board
    {
        public List<int> moved;

        public OldBoard(int width = 8, int height = 8, int nInRow = 5)
            : base(width, height, nInRow)
        {
            moved = new List<int>();
        }

        public override void init_board(int startPlayer = 0)
        {
            base.init_board(startPlayer);
            moved = new List<int>();
        }

        public override void do_move(int move)
        {
            base.do_move(move);
            moved.Add(move);
        }

        protected override Tuple<bool, int> Win()
        {
            var n = n_in_row;
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

        private bool check(int m, int n, int step = 1)
        {
            int len1 = 0, len2 = 0, empty = 0;
            for (int i = m; i < n; i += step)
            {
                int player = states[i];
                if (player != -1)
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

        public override Board DeepCopy()
        {
            var b = new OldBoard(width, height, n_in_row)
            {
                moved_size = moved_size,
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
            for (int i = 0; i < size; i++)
            {
                b.states[i] = states[i];
            }
            return b;
        }
    }
}
