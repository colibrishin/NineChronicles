using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public abstract class ActionHandler
    {
        public bool Pending { get; set; }
        public Currency GoldCurrency { get; internal set; }

        public abstract void Start(ActionRenderer renderer);

        protected static bool HasUpdatedAssetsForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedFungibleAssets.ContainsKey(States.Instance.AgentState.address);
        }

        protected static bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.address);

        protected static bool ValidateEvaluationForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return !(States.Instance.AgentState is null) &&
                   evaluation.Signer.Equals(States.Instance.AgentState.address);
        }

        protected static AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetGoldBalanceState(agentAddress, GoldCurrency);
        }

        protected (MonsterCollectionState, int, FungibleAssetValue) GetMonsterCollectionState<T>(
            ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                agentAddress,
                States.Instance.AgentState.MonsterCollectionRound
            );
            if (evaluation.OutputStates.GetState(monsterCollectionAddress) is Bencodex.Types.Dictionary mcDict)
            {
                var balance =
                    evaluation.OutputStates.GetBalance(monsterCollectionAddress, GoldCurrency);
                var level =
                    TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                        agentAddress, balance);
                return (new MonsterCollectionState(mcDict), level, balance);
            }

            return (null, 0, new FungibleAssetValue());
        }

        protected (StakeState, int, FungibleAssetValue) GetStakeState<T>(
            ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            if (evaluation.OutputStates.GetState(
                    StakeState.DeriveAddress(agentAddress)) is
                Bencodex.Types.Dictionary serialized)
            {
                var state = new StakeState(serialized);
                var balance = evaluation.OutputStates.GetBalance(
                    state.address,
                    GoldCurrency);
                return (
                    state,
                    TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                        agentAddress,
                        balance),
                    balance);
            }

            return (null, 0, new FungibleAssetValue());
        }

        protected static CrystalRandomSkillState GetCrystalRandomSkillState<T>(
            ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var buffStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            if (evaluation.OutputStates.GetState(buffStateAddress) is
                Bencodex.Types.List serialized)
            {
                var state = new CrystalRandomSkillState(buffStateAddress, serialized);
                return state;
            }

            return null;
        }

        protected async UniTask UpdateAgentStateAsync<T>(
            ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            Debug.LogFormat(
                "Called UpdateAgentState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            await UpdateAgentStateAsync(GetAgentState(evaluation));
            try
            {
                UpdateGoldBalanceState(GetGoldBalanceState(evaluation));
            }
            catch (BalanceDoesNotExistsException)
            {
                UpdateGoldBalanceState(null);
            }
        }

        protected static async UniTask UpdateAvatarState<T>(
            ActionBase.ActionEvaluation<T> evaluation,
            int index)
            where T : ActionBase
        {
            Debug.LogFormat(
                "Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            if (evaluation.OutputStates.TryGetAvatarStateV2(
                    agentAddress,
                    avatarAddress,
                    out var avatarState,
                    out _))
            {
                await UpdateAvatarState(avatarState, index);
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (evaluation.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
            {
                await UpdateCurrentAvatarStateAsync(avatarState);
            }
            else
            {
                Debug.LogError($"Failed to get AvatarState: {agentAddress}, {avatarAddress}");
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatars =
                await Game.Game.instance.Agent.GetAvatarStates(new[] { avatarAddress });
            if (avatars.TryGetValue(avatarAddress, out var avatarState))
            {
                await UpdateCurrentAvatarStateAsync(avatarState);
            }
            else
            {
                Debug.LogError($"Failed to get AvatarState: {avatarAddress}");
            }
        }

        protected static void UpdateGameConfigState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = evaluation.OutputStates.GetGameConfigState();
            States.Instance.SetGameConfigState(state);
        }

        protected static void UpdateMonsterCollectionState(
            MonsterCollectionState mcState,
            GoldBalanceState stakedGoldBalance,
            int level)
        {
            if (mcState is { })
            {
                States.Instance.SetMonsterCollectionState(mcState, stakedGoldBalance, level);
            }
        }

        protected static void UpdateStakeState(
            StakeState state,
            GoldBalanceState stakedBalanceState,
            int level)
        {
            if (state is { })
            {
                States.Instance.SetStakeState(state, stakedBalanceState, level);
            }
        }

        protected static void UpdateCrystalRandomSkillState<T>(
            ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = GetCrystalRandomSkillState(evaluation);

            if (state is { })
            {
                States.Instance.SetCrystalRandomSkillState(state);
            }
        }

        private static UniTask UpdateAgentStateAsync(AgentState state)
        {
            UpdateCache(state);
            return States.Instance.SetAgentStateAsync(state);
        }

        private static void UpdateGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is { } &&
                Game.Game.instance.Agent.Address.Equals(goldBalanceState.address))
            {
                Game.Game.instance.CachedBalance[goldBalanceState.address] = goldBalanceState.Gold;
            }

            States.Instance.SetGoldBalanceState(goldBalanceState);
        }

        protected static void UpdateCrystalBalance<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var crystal = evaluation.OutputStates.GetBalance(evaluation.Signer, CrystalCalculator.CRYSTAL);
            var agentState = States.Instance.AgentState;
            if (evaluation.Signer.Equals(agentState.address))
            {
                States.Instance.SetCrystalBalance(crystal);
            }
        }

        private static UniTask UpdateAvatarState(AvatarState avatarState, int index) =>
            States.Instance.AddOrReplaceAvatarStateAsync(avatarState, index);

        public async UniTask UpdateCurrentAvatarStateAsync(AvatarState avatarState)
        {
            // When in battle, do not immediately update the AvatarState, but pending it.
            if (Pending)
            {
                Debug.Log($"[{nameof(ActionHandler)}] Pending AvatarState");
                Game.Game.instance.Stage.AvatarState = avatarState;
                return;
            }

            Game.Game.instance.Stage.AvatarState = null;
            var questList = avatarState.questList.Where(i => i.Complete && !i.IsPaidInAction).ToList();
            if (questList.Count >= 1)
            {
                if (questList.Count == 1)
                {
                    var quest = questList.First();
                    var format = L10nManager.Localize("NOTIFICATION_QUEST_COMPLETE");
                    var msg = string.Format(format, quest.GetContent());
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
                else
                {
                    var format = L10nManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
            }

            UpdateCache(avatarState);
            await UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey);
        }

        internal static void UpdateCombinationSlotState(int slotIndex, CombinationSlotState state)
        {
            States.Instance.UpdateCombinationSlotState(slotIndex, state);
            UpdateCache(state);
        }

        private static void UpdateCache(Model.State.State state)
        {
            if (state is null)
            {
                return;
            }

            if (Game.Game.instance.CachedStates.ContainsKey(state.address))
            {
                Game.Game.instance.CachedStates[state.address] = state.Serialize();
            }
        }
    }
}
