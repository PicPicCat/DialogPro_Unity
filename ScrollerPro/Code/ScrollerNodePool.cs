using System.Collections.Generic;
using UnityEngine;

namespace ScrollerPro
{
    public abstract class ScrollerNodePool
    {
        public abstract void RemoveDisposedNode(IScrollerNode node);
        public abstract void AddDisposedNode(IScrollerNode node);
        public abstract IScrollerCell GetOrCreateCell();
    }
    public class ScrollerNodePool<TNode, TCell> : ScrollerNodePool
        where TNode : ScrollerNode<TCell> where TCell : ScrollerCell
    {
        public GameObject prefab;
        public Transform root;
        public int poolSize;
        private HashSet<TNode> _disposedNodes = new();

        public void Clear()
        {
            foreach (var node in _disposedNodes)
            {
                var deleteCell = node.RecycleCell();
                Object.Destroy(deleteCell.gameObject);
            }

            _disposedNodes.Clear();
        }

        public override void RemoveDisposedNode(IScrollerNode node)
        {
            _disposedNodes.Remove(node as TNode);
        }

        public override void AddDisposedNode(IScrollerNode node)
        {
            _disposedNodes.Add(node as TNode);
        }
        public override IScrollerCell GetOrCreateCell()
        {
            TCell cell = null;
            var count = _disposedNodes.Count;
            var removeList = new List<TNode>();
            foreach (var node in _disposedNodes)
            {
                if (!cell)
                {
                    cell = node.RecycleCell();
                    removeList.Add(node);
                }
                else if (count > poolSize)
                {
                    var deleteCell = node.RecycleCell();
                    Object.Destroy(deleteCell.gameObject);
                    removeList.Add(node);
                }
                else break;
            }

            foreach (var node in removeList)
            {
                _disposedNodes.Remove(node);
            }

            if (cell) return cell;

            var go = Object.Instantiate(prefab, root);
            return go.GetComponent<TCell>();
        }
    }
}