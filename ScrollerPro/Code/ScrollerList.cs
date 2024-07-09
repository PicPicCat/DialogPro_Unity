using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace ScrollerPro
{
    [RequireComponent(typeof(RectTransform))]
    public class ScrollerList : UIBehaviour, IBeginDragHandler, IEndDragHandler,
        IDragHandler, IPointerClickHandler,IScrollHandler
    {
       
       [SerializeField] private RectTransform contentRect;
       [SerializeField] private LayoutRootType layoutRoot = LayoutRootType.Up;
       [SerializeField] private float scrollSensitivity = 2.5f;
       [SerializeField] private float velocityDampStrength = 1f;
       [SerializeField] private bool disableDrag;
       [SerializeField] private bool disableElastic;
        public enum LayoutRootType
        {
            Down,
            Right,
            Up,
            Left,
        }
        
        private float _delta;
        private Vector2 _baseOffset;
        private Vector2 _deltaOffset;
        private Vector2 _overOffset;
        
        private IScrollerNode[] _nodes = Array.Empty<IScrollerNode>();
        private float[] _lens = Array.Empty<float>();
        
        private Vector2 _dragStartPos;
        private Vector2 _rawVelocity;
        private Vector2 _pointerVelocity;
        private List<Vector3> _pointerPosRecords = new();
        private OffsetModifierDelegate _offsetModifier;
        public delegate Vector2 OffsetModifierDelegate(Vector2 offset);
        
        private bool _isDragging;
        private bool _clickStop;
        private bool _resetHead;
        private float _pointerDownTime;
        private Action _resetHeadAction;
        
        /// <summary>
        /// 禁用弹性效果
        /// </summary>
        public bool DisableElastic
        {
            get => disableElastic;
            set => disableElastic = value;
        }
        
        /// <summary>
        /// 禁用鼠标拖拽
        /// </summary>
        public bool DisableDrag
        {
            get => disableDrag;
            set
            {
                disableDrag = value;
                if (!disableDrag) return;
                _isDragging = false;
                _pointerVelocity = Vector2.zero;
                _rawVelocity = Vector2.zero;
            }
        }
        
        /// <summary>
        /// 鼠标滚动灵敏度
        /// </summary>
        public float ScrollSensitivity
        {
            get => scrollSensitivity;
            set => scrollSensitivity = value;
        }

        /// <summary>
        /// 划动速度衰减强度
        /// </summary>
        public float VelocityDampStrength
        {
            get => velocityDampStrength;
            set => velocityDampStrength = value;
        }
        
        /// <summary>
        /// 列表排列的出发点
        /// </summary>
        public LayoutRootType LayoutRoot => layoutRoot;

        /// <summary>
        /// 拖拽位移输入的修改器
        /// </summary>
        public OffsetModifierDelegate OffsetModifier
        {
            set => _offsetModifier = value;
        }

        /// <summary>
        /// 情况列表
        /// </summary>
        public void ClearList()
        {
            foreach (var node in _nodes)
            {
                node.Dispose();
            }
            _nodes = Array.Empty<IScrollerNode>();
            _lens = Array.Empty<float>();
        }

        /// <summary>
        /// 获取头节点位置
        /// </summary>
        /// <param name="head">头节点</param>
        /// <param name="delta">头节点的偏移进度[0...1]</param>
        public void GetHeadPos(out IScrollerNode head, out float delta)
        {
            head = _nodes.Length <= 0 ? null : _nodes[0];
            delta = _nodes.Length <= 0 ? 0 : _delta;
        }

        /// <summary>
        /// 列表头节点的位置
        /// </summary>
        /// <param name="newHead">新的头节点</param>
        /// <param name="delta">头节点的偏移进度[0...1] 0=>完全显示,1=>完全遮挡</param>
        /// <param name="rightNow">立刻设置，否则改指令将在改该帧结束前执行</param>
        public void SetHeadPos(IScrollerNode newHead, float delta, bool rightNow = false)
        {
            _resetHead = true;
            _resetHeadAction = null;
            if (rightNow)
            {
                BuildList(newHead, delta, true);
            }
            else _resetHeadAction = () =>
            {
                BuildList(newHead, delta, true);
            };
        }

        /// <summary>
        /// 重构列表
        /// </summary>
        /// <param name="newHead">新的头节点</param>
        /// <param name="delta">头节点的偏移进度[0...1]</param>
        /// <param name="enforce">强制忽略过短情况对偏移进度的修改(适用当长度过短时将头节点部分遮挡)</param>
        private void BuildList(IScrollerNode newHead, float delta, bool enforce)
        {
            if (newHead == null)
            {
                if (_nodes.Length <= 0) return;
                newHead = _nodes[0];
            }

            _delta = Mathf.Clamp(delta, 0, 1);
            var headCell = newHead.Cell;
            var cellLen = headCell.GetCellLen();
            var deltaLen = Mathf.Lerp(0, cellLen, _delta);
            var viewLen = GetViewLen();
            
            
            var sumLen = deltaLen;
            var curt = newHead.Right;
            var nodes = new List<IScrollerNode> { newHead };
            var lens = new List<float> { cellLen };

            // get new cell or reuse
            while (curt != null)
            {
                var curtCell = curt.Cell;
                cellLen = curtCell.GetCellLen();
                nodes.Add(curt);
                lens.Add(cellLen);
                sumLen += cellLen;
                if (sumLen > viewLen + deltaLen + 0.01f) break;
                curt = curt.Right;
            }

            //clear old cell
            foreach (var node in _nodes)
            {
                if (nodes.Contains(node)) continue;
                node.Dispose();
            }

            _nodes = nodes.ToArray();
            _lens = lens.ToArray();
            ResetCells(enforce);
        }

        /// <summary>
        /// 拖拽列表（鼠标事件冲突时优先响应此方法）
        /// </summary>
        /// <param name="deltaOffset">拖拽位移</param>
        /// <param name="doNotModify">不对位移输入进行修饰</param>
        public void DragList(Vector2 deltaOffset, bool doNotModify)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _pointerVelocity = Vector2.zero;
                _rawVelocity = Vector2.zero;
            }

            if (!doNotModify) deltaOffset = GetModifyOffset(deltaOffset);
            ApplyOffset(deltaOffset);
        }
        
        /// <summary>
        /// 显示区域宽度
        /// </summary>
        public float GetViewWidth()
        {
            return layoutRoot switch
            {
                LayoutRootType.Down => viewRect.sizeDelta.x,
                LayoutRootType.Up => viewRect.sizeDelta.x,
                LayoutRootType.Right => viewRect.sizeDelta.y,
                LayoutRootType.Left => viewRect.sizeDelta.y,
                _ => 0f,
            };
        }
        /// <summary>
        /// 显示区域长度
        /// </summary>
        public float GetViewLen()
        {
            return layoutRoot switch
            {
                LayoutRootType.Down => viewRect.sizeDelta.y,
                LayoutRootType.Up => viewRect.sizeDelta.y,
                LayoutRootType.Right => viewRect.sizeDelta.x,
                LayoutRootType.Left => viewRect.sizeDelta.x,
                _ => 0f,
            };
        }
        /// <summary>
        /// 内容区域长度
        /// </summary>
        public float GetContentLen()
        {
            if (_nodes.Length <= 0) return 0;
            return layoutRoot switch
            {
                LayoutRootType.Down => contentRect.sizeDelta.y,
                LayoutRootType.Up => contentRect.sizeDelta.y,
                LayoutRootType.Right => contentRect.sizeDelta.x,
                LayoutRootType.Left => contentRect.sizeDelta.x,
                _ => 0f,
            };
        }
        public Vector2 ViewSize => viewRect.sizeDelta;
        public Vector2 ContentSize => contentRect.sizeDelta;

        private RectTransform viewRect => _viewRect ??= transform as RectTransform;
        private RectTransform _viewRect;
        private void LateUpdate()
        {
            
            if (_resetHead)
            {
                ResetCells(true);
                _resetHead = false;
                _resetHeadAction?.Invoke();
                _resetHeadAction = null;
            }
            else
            {
                ResetCells(false);
                if (_nodes.Length <= 0) return;
                if (_isDragging)
                {
                    _pointerVelocity = Vector2.Lerp(_pointerVelocity, _rawVelocity, 
                        10 * Time.unscaledDeltaTime);
                }
                else
                {
                    var outLen = GetOutRange();
                    var deltaOffset = Vector2.zero;
                    var axis = layoutRoot switch
                    {
                        LayoutRootType.Down => 1,
                        LayoutRootType.Right => 0,
                        LayoutRootType.Up => 1,
                        LayoutRootType.Left => 0,
                        _ => 0,
                    };
                    var velocity = _pointerVelocity[axis];

                    if (Mathf.Abs(outLen) >= 0.1f)
                    {
                        if (!disableElastic)
                        {
                            var damp = Mathf.Lerp(outLen, -outLen, 5.5f * Time.unscaledDeltaTime);
                            damp = outLen > 0
                                ? Mathf.Clamp(damp, 0, outLen)
                                : Mathf.Clamp(damp, outLen, 0);
                            deltaOffset[axis] = -Mathf.Sign(outLen) * Mathf.Abs(damp - outLen);
                            _pointerVelocity[axis] = 0;
                            ApplyOffset(deltaOffset);
                        }
                    }
                    else if (Mathf.Abs(velocity) > 0.1f)
                    {
                        var strength = Mathf.Max(0, velocityDampStrength);
                        velocity *= Mathf.Max(0, 1 - 2f * strength * Time.unscaledDeltaTime);
                        deltaOffset[axis] = velocity * Time.unscaledDeltaTime;
                        _pointerVelocity[axis] = velocity;
                        ApplyOffset(deltaOffset);
                    }
                }
            }
            contentRect.anchoredPosition = (_deltaOffset + _overOffset) * GetMask();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            if (!enabled || !gameObject.activeSelf) return;
            if (_nodes.Length <= 0) return;
            BuildList(_nodes[0], _delta,false);
        }

        private void ApplyOffset(Vector2 deltaOffset)
        {
            if (_nodes.Length <= 0) return;
            var deltaLen = layoutRoot switch
            {
                LayoutRootType.Down => -deltaOffset.y,
                LayoutRootType.Right => deltaOffset.x,
                LayoutRootType.Up => deltaOffset.y,
                LayoutRootType.Left => -deltaOffset.x,
                _ => 0,
            };
            if (Mathf.Abs(deltaLen) < 0.01f)
            {
                return;
            }
            if (AddDeltaLen(deltaLen, out var headNode, out var delta))
            {
                BuildList(headNode, delta,false);
            }
        }



        private float GetOutRange()
        {
            var viewLen = GetViewLen();
            var contentLen = GetContentLen();
            var overLen = GetOverLen();
            if (contentLen <= viewLen) return GetDir() * overLen;

            var deltaLen = GetDeltaLen();
            if (overLen < 0) return GetDir() * overLen;
            var top = contentLen - deltaLen - overLen;
            return GetDir() * Mathf.Max(0, viewLen - top);
        }

        private float GetDir()
        {
            return layoutRoot switch
            {
                LayoutRootType.Down => -1f,
                LayoutRootType.Right => 1f,
                LayoutRootType.Up => 1f,
                LayoutRootType.Left => -1f,
                _ => 0f,
            };
        }

        private Vector2 GetMask()
        {
            return layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(0, 1),
                LayoutRootType.Right => new Vector2(1, 0),
                LayoutRootType.Up => new Vector2(0, 1),
                LayoutRootType.Left => new Vector2(1, 0),
                _ => Vector2.zero,
            };
        }

        private float GetDeltaLen()
        {
            if (_nodes.Length <= 0) return 0;
            return _delta * _lens[0];
        }
        
        private float GetOverLen()
        {
            return layoutRoot switch
            {
                LayoutRootType.Down => -_overOffset.y,
                LayoutRootType.Right => _overOffset.x,
                LayoutRootType.Up => _overOffset.y,
                LayoutRootType.Left => -_overOffset.x,
                _ => 0f
            };
        }

        private void SetOverLen(float len)
        {
            switch (layoutRoot)
            {
                case LayoutRootType.Down:
                    _overOffset.y = -len;
                    break;
                case LayoutRootType.Right:
                    _overOffset.x = len;
                    break;
                case LayoutRootType.Up:
                    _overOffset.y = len;
                    break;
                case LayoutRootType.Left:
                    _overOffset.x = -len;
                    break;
            }
        }
        private void ResetCells(bool enforce)
        {
            if(_lens.Length<=0)return;
            var viewLen = GetViewLen();
            var viewWidth = GetViewWidth();
            var sumLen = _lens.Sum(t => t);

            var rectSize = layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(viewWidth, sumLen),
                LayoutRootType.Right => new Vector2(sumLen, viewWidth),
                LayoutRootType.Up => new Vector2(viewWidth, sumLen),
                LayoutRootType.Left => new Vector2(sumLen, viewWidth),
                _ => Vector2.zero,
            };
            var anchor = layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(0.5f, 0),
                LayoutRootType.Right => new Vector2(1, 0.5f),
                LayoutRootType.Up => new Vector2(0.5f, 1),
                LayoutRootType.Left => new Vector2(0, 0.5f),
                _ => Vector2.zero,
            };
            contentRect.anchorMin = anchor;
            contentRect.anchorMax = anchor;
            contentRect.pivot = anchor;
            contentRect.sizeDelta = rectSize;

            var deltaLen = _lens[0] * _delta;
            _deltaOffset = layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(0, -deltaLen),
                LayoutRootType.Right => new Vector2(deltaLen, 0),
                LayoutRootType.Up => new Vector2(0, deltaLen),
                LayoutRootType.Left => new Vector2(-deltaLen, 0),
                _ => Vector2.zero,
            };

            var overLen = GetOverLen();
            var shortContent = sumLen + Mathf.Abs(overLen) + 0.1f <= viewLen;
            if (!enforce && shortContent)
            {
                // too short! deltaOffset => overOffset
                _overOffset += _deltaOffset;
                _deltaOffset = Vector2.zero;
                deltaLen = 0;
                _delta = 0;
            }

            var posLen = 0f;
            
            var disPoseOutRangeCell = !shortContent;
            for (var i = 0; i < _nodes.Length; i++)
            {
                var cellLen = _lens[i];
                var minPos = posLen - overLen - deltaLen;
                var maxPos = minPos + cellLen;
                if (disPoseOutRangeCell && (maxPos <= -1e-3f || minPos >= viewLen + 1e-3f))
                {
                    _nodes[i].Dispose(); //out of Range
                }
                else
                {
                    var node = _nodes[i];
                    var cell = node.Cell;
                    SetCellPos(cell, sumLen, posLen, cellLen, viewWidth);
                }

                posLen += cellLen;
            }
        }

        private void SetCellPos(IScrollerCell cell, float sumLen, float posLen, float cellLen, float width)
        {
            var offset = -sumLen / 2f + posLen + cellLen / 2f;
            var pos = layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(0, offset),
                LayoutRootType.Right => new Vector2(-offset, 0),
                LayoutRootType.Up => new Vector2(0, -offset),
                LayoutRootType.Left => new Vector2(offset, 0),
                _ => Vector2.zero,
            };
            var size = layoutRoot switch
            {
                LayoutRootType.Down => new Vector2(width, cellLen),
                LayoutRootType.Right => new Vector2(cellLen, width),
                LayoutRootType.Up => new Vector2(width, cellLen),
                LayoutRootType.Left => new Vector2(cellLen, width),
                _ => Vector2.zero,
            };
            cell.SetCell(contentRect, pos, size);
        }
        
        private bool AddDeltaLen(float deltaLen, out IScrollerNode newHead, out float newDelta)
        {
            var overLen = GetOverLen();
            newHead = null;
            newDelta = 0;
            var newOverLen = overLen + deltaLen;
            if (overLen * newOverLen > 0)
            {
                SetOverLen(newOverLen);
                ResetCells(false);
                return false;
            }

            overLen = 0;
            SetOverLen(0);
            deltaLen += overLen;
            var node = _nodes[0];
            var delta = _delta;
            while (true)
            {
                var cell = node.Cell;
                var cellLen = cell.GetCellLen();
                var d = (cellLen * delta + deltaLen) / cellLen;
                if (d is <= 1 and >= 0)
                {
                    newHead = node;
                    newDelta = d;
                    return true;
                }

                d = d < 0 ? d : d - 1;
                deltaLen = cellLen * d;
                var nextNode = deltaLen < 0 ? node.Left : node.Right;
                if (nextNode == null)
                {
                    newHead = node;
                    newDelta = deltaLen < 0 ? 0 : 1;
                    SetOverLen(overLen + deltaLen);
                    return true;
                }

                node = nextNode;
                delta = deltaLen < 0 ? 1 : 0;
            }
        }

        private Vector2 GetModifyOffset(Vector2 deltaOffset)
        {
            var outLen = GetOutRange();
            var viewLen = GetViewLen();
            var r = Mathf.Abs(outLen) / viewLen;
            var p = 1 / (1f + 6.5f * r);
            deltaOffset = p * deltaOffset;
            if (_offsetModifier != null)
            {
                deltaOffset = _offsetModifier.Invoke(deltaOffset);
            }

            return deltaOffset;
        }
        
        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (disableDrag || _isDragging || _clickStop) return;
            var scrollOffset = -(scrollSensitivity * eventData.scrollDelta);
            var deltaOffset = GetModifyOffset(scrollOffset);
            ApplyOffset(deltaOffset);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (disableDrag) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnBeginDrag(eventData);
            _isDragging = true;
            _clickStop = false;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (disableDrag || _clickStop) return;
            if (!_isDragging) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _isDragging = false;
            _clickStop = false;
            OnEndDrag(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (disableDrag || _clickStop) return;
            if (!_isDragging) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnDrag(eventData);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (disableDrag || _isDragging) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _pointerVelocity = Vector2.zero;
            _clickStop = true;
        }

        private void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect,
                eventData.position, eventData.pressEventCamera,
                out _dragStartPos);
            var record = (Vector3)_dragStartPos;
            record.z = Time.unscaledTime;
            _pointerPosRecords.Clear();
            for (var i = 0; i < 4; i++)
            {
                _pointerPosRecords.Add(record);
            }
        }
        
        private void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position,
                    eventData.pressEventCamera, out var pointerCurtPos)) return;
            
            var pointerOffset = pointerCurtPos - _dragStartPos;
            var deltaOffset = GetModifyOffset(pointerOffset);
            
            _dragStartPos = pointerCurtPos;
            var record = (Vector3)_dragStartPos;
            record.z = Time.unscaledTime;
            _pointerPosRecords.RemoveAt(0);
            _pointerPosRecords.Add(record);
            
            
            ApplyOffset(deltaOffset);
            GetRawVelocity();
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position,
                    eventData.pressEventCamera, out var pointerCurtPos))
            {
                var record = (Vector3)pointerCurtPos;
                record.z = Time.unscaledTime;
                _pointerPosRecords.RemoveAt(0);
                _pointerPosRecords.Add(record);
            }

            var d = _pointerPosRecords[^1] - _pointerPosRecords[^2];
            var endVelocity = Mathf.Abs(d.z) > 1e-3f ? new Vector2(d.x / d.z, d.y / d.z) : _rawVelocity;
            var r = Mathf.Max(1e-3f, endVelocity.magnitude) /
                    Mathf.Max(1e-3f, _pointerVelocity.magnitude);
            var p = 1f / (r + 0.25f / r);
            _pointerVelocity = Vector2.Lerp(endVelocity, _pointerVelocity, p);
            _pointerPosRecords.Clear();
        }

        private void GetRawVelocity()
        {
            _rawVelocity = Vector2.zero;
            for (var i = 0; i < _pointerPosRecords.Count - 1; i++)
            {
                var d = _pointerPosRecords[i + 1] - _pointerPosRecords[i];
                var v = Mathf.Abs(d.z) > 1e-3f ? new Vector2(d.x / d.z, d.y / d.z) : _rawVelocity;
                _rawVelocity = i == 0 ? v : Vector2.Lerp(_rawVelocity, v, 0.5f);
            }
        }

    }
}