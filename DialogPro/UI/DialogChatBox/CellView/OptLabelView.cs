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
        public void SetData(int index, PrintData data)
        {
            _optIndex = index;
            var printer = new DialogPrinter();
            printer.SetPrintData(data);
            printer.Print(text_label);
            SetLabelState(OptLabelStateType.Normal);
        }
        public void SetPrintedData(PrintData data, bool selected)
        {
            var printer = new DialogPrinter();
            printer.SetPrintData(data);
            printer.Print(text_label, true);
            var state = selected 
                ? OptLabelStateType.Selected 
                : OptLabelStateType.UnSelected;
            SetLabelState(state);
        }
    }
}