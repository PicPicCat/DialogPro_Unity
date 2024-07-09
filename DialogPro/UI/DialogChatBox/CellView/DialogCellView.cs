using ScrollerPro;
using UnityEngine;

namespace DialogPro.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class DialogCellView<T> : ScrollerCell where T : class
    {
        protected T _cellData;

        public virtual void clearData()
        {
            _cellData = null;
        }

        public abstract void SetData(T cellData);

        protected override void SetCell(RectTransform root, Vector2 pos, Vector2 size)
        {
            var rt = rectTransform;
            rt.SetParent(root);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}