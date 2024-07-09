using UnityEngine;

namespace ScrollerPro
{
    public interface IScrollerNode
    {
        IScrollerNode Left { get; }
        IScrollerNode Right { get; }
        IScrollerCell Cell { get; }
        void Dispose();
    }
    
    public abstract class ScrollerNode : IScrollerNode
    {
        public ScrollerNode left;
        public ScrollerNode right;
        protected abstract IScrollerCell Cell { get; }
        protected abstract void Dispose();
        IScrollerNode IScrollerNode.Left => left;
        IScrollerNode IScrollerNode.Right => right;
        IScrollerCell IScrollerNode.Cell => Cell;
        void IScrollerNode.Dispose() => Dispose();
    }
    
    public abstract class ScrollerNode<TCell> : ScrollerNode  where TCell : ScrollerCell
    {
        public ScrollerNodePool pool;
        private TCell cell;

        public TCell RecycleCell()
        {
            var tmp = cell;
            cell = null;
            return tmp;
        }

        protected override IScrollerCell Cell
        {
            get
            {
                pool.RemoveDisposedNode(this);
                if (!cell)
                {
                    cell = pool.GetOrCreateCell() as TCell;
                    BeforeNewCellUse(cell);
                }

                cell.gameObject.SetActive(true);
                return cell;
            }
        }

        protected override void Dispose()
        {
            if (cell)
            {
                cell.gameObject.SetActive(false);
                pool.AddDisposedNode(this);
            }
        }

        protected abstract void BeforeNewCellUse(TCell cell);
    }
}