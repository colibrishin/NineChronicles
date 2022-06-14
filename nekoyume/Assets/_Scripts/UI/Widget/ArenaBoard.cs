﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaBoard : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif

        [SerializeField] private ArenaBoardBillboard _billboard;

        [SerializeField] private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField] private Button _backButton;

        private ArenaSheet.RoundData _roundData;

        private RxProps.ArenaParticipant[] _boundedData;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<ArenaJoin>().Show();
                Close();
            }).AddTo(gameObject);
        }

        public async UniTaskVoid ShowAsync(
            ArenaSheet.RoundData roundData,
            bool ignoreShowAnimation = false) =>
            Show(
                roundData,
                await RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync(),
                ignoreShowAnimation);

        public void Show(
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false) =>
            Show(_roundData,
                arenaParticipants,
                ignoreShowAnimation);

        public void Show(
            ArenaSheet.RoundData roundData,
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            _roundData = roundData;
            _boundedData = arenaParticipants;
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();
            base.Show(ignoreShowAnimation);
        }

        public void OnRenderBattleArena(ActionBase.ActionEvaluation<BattleArena> eval)
        {
            if (eval.Exception is { })
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close();
            Find<ArenaBattleLoadingScreen>().Close();
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank,
                    _so.WinCount,
                    _so.LoseCount,
                    _so.CP,
                    _so.Rating);
                return;
            }
#endif
            var player = RxProps.PlayersArenaParticipant.Value;
            _billboard.SetData(
                "season",
                player.Rank,
                player.CurrentArenaInfo.Win,
                player.CurrentArenaInfo.Lose,
                player.CP,
                player.Score);
        }

        private void InitializeScrolls()
        {
            _playerScroll.OnClickChoice.Subscribe(index =>
                {
                    Debug.Log($"{index} choose!");

#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot battle when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

#endif
                    var data = _boundedData[index];
                    var avatarState = data.AvatarState;
                    Find<ArenaBattleLoadingScreen>().Show(
                        avatarState.NameWithHash,
                        avatarState.level,
                        avatarState.GetArmorId());
                    var inventory = States.Instance.CurrentAvatarState.inventory;
                    ActionRenderHandler.Instance.Pending = true;
                    ActionManager.Instance.BattleArena(
                            data.AvatarAddr,
                            inventory.Costumes
                                .Where(e => e.Equipped)
                                .Select(e => e.NonFungibleId)
                                .ToList(),
                            inventory.Equipments
                                .Where(e => e.Equipped)
                                .Select(e => e.NonFungibleId)
                                .ToList(),
                            _roundData.ChampionshipId,
                            _roundData.Round,
                            // TODO: Take the ticket count from the UI.
                            1)
                        .Subscribe();
                })
                .AddTo(gameObject);
        }

        private void UpdateScrolls()
        {
            _playerScroll.SetData(GetScrollData(), 0);
        }

        private List<ArenaBoardPlayerItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.ArenaBoardPlayerScrollData;
            }
#endif

            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            return RxProps.ArenaParticipantsOrderedWithScore.Value.Select(e =>
                new ArenaBoardPlayerItemData
                {
                    name = e.AvatarState.NameWithHash,
                    level = e.AvatarState.level,
                    armorId = e.AvatarState.GetArmorId(),
                    titleId = e.AvatarState.inventory.Costumes
                        .FirstOrDefault(costume =>
                            costume.ItemSubType == ItemSubType.Title
                            && costume.Equipped)?
                        .Id,
                    cp = e.AvatarState.GetCP(),
                    score = e.Score,
                    expectWinDeltaScore = e.ExpectDeltaScore.win,
                    interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                }).ToList();
        }
    }
}