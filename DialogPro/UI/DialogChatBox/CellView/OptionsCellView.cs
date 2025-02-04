﻿using System;
using UnityEngine;

namespace DialogPro.UI
{
    [Serializable]
    public class DialogOptionsCellData
    {
        public bool first;
        public float cellLen;
        public int selectedIndex;
        public DialogPrinter[] options;
    }
    
    
    
    public class OptionsCellView:DialogCellView<DialogOptionsCellData>
    {
        public DialogChatBox chatBox;
        public RectTransform labelRoot;
        
        public override void SetData(DialogOptionsCellData cellData)
        {
            _cellData = cellData;
            for (var i = 0; i < labelRoot.childCount; i++)
            {
                var label=labelRoot.GetChild(i).GetComponent<OptLabelView>();
                if (i < cellData.options.Length)
                {
                    label.gameObject.SetActive(true);
                    label.SetData(i, cellData);
                    continue;
                }
                label.gameObject.SetActive(false);
            }
        }


        protected override float GetCellLen() => _cellData.cellLen;

        public void OnClickSelect(int index)
        {
            _cellData.first = false;
            _cellData.selectedIndex = index;
            chatBox.OnSelectOptionFinish(index);
        }
    }
}