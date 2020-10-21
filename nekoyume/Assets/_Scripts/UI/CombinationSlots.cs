using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI
{
    public class CombinationSlots : XTweenWidget
    {
        public CombinationSlot[] slots;
        private long _blockIndex;
        private Dictionary<int, CombinationSlotState> _states;

        protected override void Awake()
        {
            base.Awake();
            CombinationSlotStatesSubject.CombinationSlotStates
                .Subscribe(SetSlots)
                .AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
            _blockIndex = Game.Game.instance.Agent.BlockIndex;
        }

        private void SetSlots(Dictionary<Address, CombinationSlotState> states)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            _states = states.ToDictionary(
                pair => avatarState.combinationSlotAddresses.IndexOf(pair.Key),
                pair => pair.Value);

            UpdateSlots();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateSlots();
        }

        private void UpdateSlots()
        {
            if (_states is null)
            {
                return;
            }

            foreach (var pair in _states?.Where(pair => !(pair.Value is null)))
            {
                slots[pair.Key].SetData(pair.Value, _blockIndex, pair.Key);
            }
        }
    }
}
