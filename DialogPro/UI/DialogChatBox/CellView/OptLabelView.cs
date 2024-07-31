using TMPro;
using UnityEngine;

namespace DialogPro.UI
{
    public abstract class OptLabelView : MonoBehaviour
    {
        public OptionsCellView optionsCell;
        public TextMeshProUGUI text_label;
        private int _optIndex;
        protected enum OptLabelStateType
        {
            Normal,
            Selected,
            UnSelected,
        }
        protected abstract void SetLabelState(OptLabelStateType type);
        
        public void OnClickSelect()
        {
            if (_optIndex < 0) return;
            var tmp = _optIndex;
            _optIndex = -1;
            optionsCell.OnClickSelect(tmp);
        }

        public void SetData(int index, DialogOptionsCellData data)
        {
            _optIndex = index;
            text_label.text = data.options[index].Print();
            var type = OptLabelStateType.Normal;
            if (!data.first)
            {
                type = data.selectedIndex == index
                    ? OptLabelStateType.Selected
                    : OptLabelStateType.UnSelected;
            }
            SetLabelState(type);
            
        }
    }
}