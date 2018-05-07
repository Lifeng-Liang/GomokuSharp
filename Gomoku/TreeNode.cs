using System;
using System.Collections.Generic;

namespace Gomoku
{
    public class TreeNode
    {
        public TreeNode _parent;
        public Dictionary<int, TreeNode> _children;
        public int _n_visits;
        private double _Q;
        private double _u;
        private double _P;

        public TreeNode(TreeNode parent, double priorP)
        {
            this._parent = parent;
            this._children = new Dictionary<int, TreeNode>();
            this._n_visits = 0;
            this._Q = 0;
            this._u = 0;
            this._P = priorP;
        }

        public void expand(IEnumerable<Tuple<int, double>> actionPriors)
        {
            foreach (var ap in actionPriors)
            {
                if (!_children.ContainsKey(ap.Item1))
                {
                    _children[ap.Item1] = new TreeNode(this, ap.Item2);
                }
            }
        }

        public KeyValuePair<int, TreeNode> select(int cPuct)
        {
            return _children.Max(n => n.Value.get_value(cPuct));
        }

        public void update(double leafValue)
        {
            // Count visit.
            _n_visits += 1;
            // Update Q, a running average of values for all visits.
            _Q += 1.0*(leafValue - _Q)/_n_visits;
        }

        public void update_recursive(double leafValue)
        {
            // If it is not root, this node's parent should be updated first.
            _parent?.update_recursive(-leafValue);
            update(leafValue);
        }

        public double get_value(int cPuct)
        {
            _u = cPuct * _P * Math.Sqrt(_parent._n_visits)/(1 + _n_visits);
            return _Q + _u;
        }

        public bool is_leaf()
        {
            return _children.Count == 0;
        }

        public bool is_root()
        {
            return _parent == null;
        }
    }
}