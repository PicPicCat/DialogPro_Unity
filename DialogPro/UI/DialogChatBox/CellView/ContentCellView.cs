using TMPro;
using UnityEngine;

namespace DialogPro.UI
{
    public class DialogContentCellData
    {
        public bool first;
        public float cellLen;
        public PrintData speaker;
        public PrintData content;
    }

    public class ContentCellView : DialogCellView<DialogContentCellData>
    {
        public DialogChatBox chatBox;
        public TextMeshProUGUI text_speaker;
        public TextMeshProUGUI text_content;
        public float printSpeed;
        private DialogPrinter _printer;

        public override void SetData(DialogContentCellData cellData)
        {
            if (_cellData == cellData) return;
            _cellData = cellData;
            _printer = new DialogPrinter();

            if (cellData.first)
            {
                cellData.first = false;
                _printer.SetPrintData(cellData.speaker);
                _printer.Print(text_speaker);
                _printer.SetPrintData(cellData.content);
            }
            else
            {
                _printer.SetPrintData(cellData.speaker);
                _printer.Print(text_speaker, true);
                _printer.SetPrintData(cellData.content);
                _printer.Print(text_content, true);
                _printer = null;
            }
        }

        protected override float GetCellLen()=>_cellData.cellLen;

        private void Update()
        {
            if (_printer == null) return;
            var delta = Time.unscaledDeltaTime * printSpeed;
            if (!_printer.Print(delta, text_content)) return;
            _printer = null;
            chatBox.OnContentPrintFinish();
        }
    }
}