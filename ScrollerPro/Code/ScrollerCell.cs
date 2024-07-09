using UnityEngine;

namespace ScrollerPro
{
    public interface IScrollerCell
    {
        float GetCellLen();
        void SetCell(RectTransform root, Vector2 pos, Vector2 size);
    }

    [RequireComponent(typeof(RectTransform))]
    public abstract class ScrollerCell : MonoBehaviour, IScrollerCell
    {
        public RectTransform rectTransform => _rectTransform ??= transform as RectTransform;
        private RectTransform _rectTransform;

        protected abstract float GetCellLen();
        protected abstract void SetCell(RectTransform root, Vector2 pos, Vector2 size);
       
        float IScrollerCell.GetCellLen()=>GetCellLen();
        void IScrollerCell.SetCell(RectTransform root, Vector2 pos, Vector2 size) => SetCell(root, pos, size);
    }
}