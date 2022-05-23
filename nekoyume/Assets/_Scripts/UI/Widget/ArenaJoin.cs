﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.UI.Module.Arena;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaJoin : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaJoinSO _so;
#endif

        [SerializeField]
        private ArenaJoinSeasonScroll _scroll;

        [SerializeField]
        private ArenaJoinSeasonBarScroll _barScroll;

        [SerializeField]
        private int _barPointCount;

        [SerializeField]
        private ArenaJoinSeasonInfo _info;

        [SerializeField]
        private Button _joinButton;

        [SerializeField]
        private Button _paymentButton;

        [SerializeField]
        private Button _earlyPaymentButton;

        [SerializeField]
        private Button _backButton;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);
            _joinButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
            _paymentButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
            _earlyPaymentButton.onClick.AsObservable().Subscribe().AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            InitializeScrolls(_disposables);
            UpdateInfo();
            UpdateButtons();
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private IList<ArenaJoinSeasonItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.ScrollData;
            }
#endif

            return new List<ArenaJoinSeasonItemData>();
        }

        private void InitializeScrolls(IList<IDisposable> disposables)
        {
            var scrollData = GetScrollData();
            var selectedIndex = Random.Range(0, 2);
            _scroll.SetData(scrollData, selectedIndex);
            var barIndexOffset = (int)math.ceil(_barPointCount / 2f) - 1;
            _barScroll.SetData(
                GetBarScrollData(barIndexOffset),
                ReverseScrollIndex(selectedIndex));

            // NOTE: Scroll events should subscribe after set data. 
            _scroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _barScroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateButtons();
                })
                .AddTo(disposables);
            _barScroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _scroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateButtons();
                })
                .AddTo(disposables);
        }

        private IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barIndexOffset)
        {
            var cellCount = _barPointCount;
            return Enumerable.Range(0, cellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset,
                })
                .ToList();
        }

        private int ReverseScrollIndex(int scrollIndex) =>
            _barPointCount - scrollIndex - 1;

        private void UpdateInfo()
        {
            _info.SetData(
                _scroll.SelectedItemData.name,
                GetMedalId(),
                GetConditions(),
                GetRewardType());
        }

        private void UpdateButtons()
        {
            _joinButton.gameObject.SetActive(true);
            _paymentButton.gameObject.SetActive(false);
            _earlyPaymentButton.gameObject.SetActive(false);
        }

        private int GetMedalId()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.MedalId;
            }
#endif

            return 700000;
        }

        private (int max, int current) GetConditions()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.Conditions;
            }
#endif

            return (100, 0);
        }

        private ArenaJoinSeasonInfo.RewardType GetRewardType()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.RewardType;
            }
#endif

            return ArenaJoinSeasonInfo.RewardType.Medal;
        }
    }
}