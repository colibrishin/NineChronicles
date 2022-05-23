﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace Nekoyume.UI.Module.Arena
{
    using UniRx;

    public class ArenaJoinSeasonScroll :
        FancyScrollView<ArenaJoinSeasonItemData, ArenaJoinSeasonScrollContext>
    {
        [SerializeField]
        private UnityEngine.UI.Extensions.Scroller _scroller;

        [SerializeField]
        private GameObject _cellPrefab;

        protected override GameObject CellPrefab => _cellPrefab;

        private IList<ArenaJoinSeasonItemData> _data;

        public ArenaJoinSeasonItemData SelectedItemData => _data[Context.selectedIndex];

        private readonly Subject<int> _onSelectionChanged = new Subject<int>();
        public IObservable<int> OnSelectionChanged => _onSelectionChanged;

        public void SetData(IList<ArenaJoinSeasonItemData> data, int? index = null)
        {
            _data = data;
            UpdateContents(_data);
            _scroller.SetTotalCount(_data.Count);

            if (index.HasValue)
            {
                if (index.Value >= _data.Count)
                {
                    Debug.LogError($"Index out of range: {index.Value} >= {_data.Count}");
                    return;
                }

                UpdateSelection(index.Value, true);
                _scroller.JumpTo(index.Value);
            }
        }

        public void SelectCell(int index, bool invokeEvents)
        {
            if (index < 0 ||
                index >= ItemsSource.Count ||
                index == Context.selectedIndex)
            {
                return;
            }

            UpdateSelection(index, invokeEvents);
            _scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }

        protected override void Initialize()
        {
            base.Initialize();

            Context.onCellClicked = index => SelectCell(index, true);
            _scroller.OnValueChanged(UpdatePosition);
            _scroller.OnSelectionChanged(index => UpdateSelection(index, true));
        }

        private void UpdateSelection(int index, bool invokeEvents)
        {
            if (index == Context.selectedIndex)
            {
                return;
            }

            Context.selectedIndex = index;
            Refresh();

            if (invokeEvents)
            {
                _onSelectionChanged.OnNext(Context.selectedIndex);   
            }
        }
    }
}