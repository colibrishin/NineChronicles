﻿using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ArenaBoard : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif

        [SerializeField]
        private ArenaBoardBillboard _billboard;

        [SerializeField]
        private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField]
        private Button _backButton;

        protected override void Awake()
        {
            base.Awake();

            _playerScroll.OnClickChoice.Subscribe(index =>
            {
                Debug.Log($"{index} choose!");
            }).AddTo(gameObject);

            _backButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Find<ArenaJoin>().Show();
                    Close();
                }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            InitializeScrolls();
            base.Show(ignoreShowAnimation);
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank.ToString(),
                    $"{_so.WinCount}/{_so.LoseCount}",
                    _so.CP.ToString(),
                    _so.Rating.ToString());
                return;
            }
#endif

            _billboard.SetData(
                "season",
                "rank",
                "win/Lose",
                "cp",
                "rating");
        }

        private void InitializeScrolls()
        {
            _playerScroll.SetData(GetScrollData(), 0);
        }

        private IList<ArenaBoardPlayerItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.ArenaBoardPlayerScrollData;
            }
#endif

            return new List<ArenaBoardPlayerItemData>();
        }
    }
}