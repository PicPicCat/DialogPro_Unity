using System;
using TMPro;
using UnityEngine;

namespace DialogPro.UI
{
    [Serializable]
    public class DialogContentCellData
    {
        public bool first;
        public float cellLen;
        public DialogPrinter speaker;
        public DialogPrinter content;
    }

    public class ContentCellView : DialogCellView<DialogContentCellData>
    {
        public DialogChatBox chatBox;
        public TextMeshProUGUI text_speaker;
        public TextMeshProUGUI text_content;
        public float printSpeed;
        private bool _printContent;
        public override void SetData(DialogContentCellData cellData)
        {
            if (_cellData == cellData) return;
            _cellData = cellData;

            if (cellData.first)
            {
                cellData.first = false;
                text_speaker.text = cellData.speaker.Print();
                _printContent = true;
            }
            else
            {
                text_speaker.text = _cellData.speaker.Print();
                text_content.text = _cellData.content.Print();
                _printContent = false;
            }
        }

        protected override float GetCellLen()=>_cellData.cellLen;

        private void Update()
        {
            if (_cellData == null || !_printContent) return;
            var delta = Time.unscaledDeltaTime * printSpeed;
            text_content.text = _cellData.content.Print(delta, out var finish);
            if (finish) chatBox.OnContentPrintFinish();
        }
    }
}