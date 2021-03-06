﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace GomokuLib
{
    public static class Ext
    {
        public static Random Rand = new Random();

        public static int Get(this Dictionary<int, int> dict, int key, int defaultValue)
        {
            int n;
            if (dict.TryGetValue(key, out n))
            {
                return n;
            }
            return defaultValue;
        }

        public static IEnumerable<Tuple<T1,T2>> zip<T1,T2>(IList<T1> list1, T2[] list2)
        {
            var min = Math.Min(list1.Count, list2.Length);
            for (int i = 0; i < min; i++)
            {
                yield return Tuple.Create(list1[i], list2[i]);
            }
        }

        public static IEnumerable<Tuple<T1, T2, T3>> zip<T1, T2, T3>(IList<T1> list1, IList<T2> list2, T3[] list3)
        {
            var min = Math.Min(Math.Min(list1.Count, list2.Count), list3.Length);
            for (int i = 0; i < min; i++)
            {
                yield return Tuple.Create(list1[i], list2[i], list3[i]);
            }
        }

        public static T[] NewArray<T>(this int n, Func<T> genItem)
        {
            var result = new T[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = genItem();
            }
            return result;
        }

        public static TV Max<TV, T>(this IEnumerable<TV> dict, Func<TV, T> key) where T : IComparable
        {
            var kv = default(TV);
            T cur = default(T);
            bool first = true;
            foreach (var item in dict)
            {
                var i = key(item);
                if (first)
                {
                    first = false;
                    cur = i;
                    kv = item;
                }
                else
                {
                    if (i.CompareTo(cur) > 0)
                    {
                        cur = i;
                        kv = item;
                    }
                }
            }
            return kv;
        }

        public static T choice<T>(List<T> list, IEnumerable<double> p)
        {
            int index = 0;
            var x = Rand.NextDouble();
            foreach (var d in p)
            {
                var y = x - d;
                if (y < 0)
                {
                    break;
                }
                x = y;
                index++;
            }
            return list[index];
        }

        public static IEnumerable<T> sample<T>(this List<T> population, int k)
        {
            for (int i = 0; i < k; i++)
            {
                var n = Rand.Next(population.Count);
                T item = population[n];
                population.RemoveAt(n);
                yield return item;
            }
        }

        public static double[,,] rot90(this double[,,] state, int n)
        {
            //TODO:
            return state;
        }

        public static double[,] rot90(this double[,] state, int n)
        {
            //TODO:
            return state;
        }

        public static double[,,] fliplr(this double[,,] state)
        {
            //TODO:
            return state;
        }

        public static double[,] fliplr(this double[,] state)
        {
            var xx = state.GetLength(0);
            var yy = state.GetLength(1);
            var result = new double[xx, yy];
            for (int y = 0; y < yy; y++)
            {
                for (int x = 0; x < xx; x++)
                {
                    var tx = xx - x - 1;
                    result[tx, y] = state[x, y];
                }
            }
            return result;
        }

        public static double[,,] flipud(this double[,,] state)
        {
            //TODO:
            return state;
        }

        public static double[,] flipud(this double[,] state)
        {
            var xx = state.GetLength(0);
            var yy = state.GetLength(1);
            var result = new double[xx, yy];
            for (int y = 0; y < yy; y++)
            {
                var ty = yy - y - 1;
                for (int x = 0; x < xx; x++)
                {
                    result[x, ty] = state[x, y];
                }
            }
            return result;
        }

        public static IEnumerable<double> flatten(this IList<IList<double>> state)
        {
            foreach (var yy in state)
            {
                foreach (var item in yy)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<double> flatten(this double[,,] state)
        {
            foreach (var d in state)
            {
                yield return d;
            }
        }

        public static IEnumerable<double> flatten(this double[,] state)
        {
            foreach (var d in state)
            {
                yield return d;
            }
        }

        public static double[] flatten(this double[] state)
        {
            return state;
        }

        public static double[,] reshape(this double[] state, int w1, int w2)
        {
            if (w1*w2 != state.Length)
            {
                throw new Exception();
            }
            double[,] result = new double[w1, w2];
            int i = 0;
            for (int y = 0; y < w2; y++)
            {
                for (int x = 0; x < w1; x++)
                {
                    result[x, y] = state[i];
                    i++;
                }
            }
            return result;
        }

        public static double variance(this IEnumerable<double> state)
        {
            int n = 0;
            double sum = 0;
            foreach (var s in state)
            {
                sum += s;
                n++;
            }
            double avg = sum/n;
            double v = 0.0;
            foreach (var s in state)
            {
                var d = s - avg;
                v += d*d;
            }
            return v / n;
        }

        public static IEnumerable<double> sub(this double[] first, double[] secend)
        {
            if (first.Length != secend.Length)
            {
                throw new Exception();
            }
            for (int i = 0; i < first.Length; i++)
            {
                yield return first[i] - secend[i];
            }
        }

        public static double mean(this IEnumerable<double> state)
        {
            int n = 0;
            double sum = 0;
            foreach (var s in state)
            {
                sum += s;
                n++;
            }
            return sum / n;
        }

        public static double[] sum(this double[,] state, int axis)
        {
            return null;
        }

        public static IList<IList<double>> exp(this IList<IList<double>> state)
        {
            var yy = state.Count;
            for (int y = 0; y < yy; y++)
            {
                var list = state[y];
                var xx = list.Count;
                for (int x = 0; x < xx; x++)
                {
                    list[x] = Math.Exp(list[x]);
                }
            }
            return state;
        }

        #region FP

        public static IEnumerable<int> To(this int start, int end, int step = 1)
        {
            for (int i = start; i < end; i += step)
            {
                yield return i;
            }
        }

        public static IEnumerable<int> Skip(this IEnumerable<int> list, int n)
        {
            foreach (var i in list)
            {
                if (n > 0)
                {
                    n--;
                    continue;
                }
                yield return i;
            }
        }

        public static IEnumerable<TR> MapObj<T, TR>(this IEnumerable list, Func<T, TR> cb)
        {
            foreach (T item in list)
            {
                yield return cb(item);
            }
        }

        public static IEnumerable<TR> Map<T, TR>(this IEnumerable<T> list, Func<T, TR> cb)
        {
            foreach (T item in list)
            {
                yield return cb(item);
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<T> list, Func<T, TR?> cb) where TR : struct
        {
            foreach (var item in list)
            {
                var n = cb(item);
                if (n.HasValue)
                {
                    yield return n.Value;
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<T> list, Func<T, TR> cb) where TR : class
        {
            foreach (var item in list)
            {
                var n = cb(item);
                if (n != null)
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<IEnumerable<T>> list, Func<T, TR?> cb) where TR : struct
        {
            foreach (var arr in list)
            {
                foreach (var item in arr)
                {
                    var n = cb(item);
                    if (n.HasValue)
                    {
                        yield return n.Value;
                    }
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<IEnumerable<T>> list, Func<T, TR> cb) where TR : class
        {
            foreach (var arr in list)
            {
                foreach (var item in arr)
                {
                    var n = cb(item);
                    if (n != null)
                    {
                        yield return n;
                    }
                }
            }
        }

        public static TR Reduce<T, TR>(this IEnumerable<T> list, Func<TR, T, TR> cb, TR baseValue)
        {
            TR prev = baseValue;
            foreach (T next in list)
            {
                prev = cb(prev, next);
            }
            return prev;
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (cb(item))
                {
                    yield return item;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> cb)
        {
            foreach (T item in list)
            {
                cb(item);
            }
        }

        public static bool Every<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (!cb(item))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Some<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (cb(item))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
