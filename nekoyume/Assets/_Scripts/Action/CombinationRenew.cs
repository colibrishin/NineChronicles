using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("combination_renew")]
    public class CombinationRenew : ActionBase
    {
        [Serializable]
        public struct ItemModel
        {
            public int Id;
            public int Count;

            public ItemModel(int id, int count)
            {
                Id = id;
                Count = count;
            }

            public ItemModel(UI.Model.CountEditableItem<UI.Model.Inventory.Item> item)
            {
                Id = item.Item.Value.Item.Data.id;
                Count = item.Count.Value;
                Debug.Log($"ItemModel | Id:{Id}, Count:{Count}");
            }
        }

        private struct ItemModelInventoryItemPair
        {
            public ItemModel ItemModel;
            public Inventory.InventoryItem InventoryItem;

            public ItemModelInventoryItemPair(ItemModel itemModel, Inventory.InventoryItem inventoryItem)
            {
                ItemModel = itemModel;
                InventoryItem = inventoryItem;
            }
        }

        public struct ResultModel
        {
            public int ErrorCode;
            public ItemModel Item;
        }

        private const string RecipePath = "Assets/Resources/DataTable/recipe.csv";

        public static readonly Subject<CombinationRenew> EndOfExecuteSubject = new Subject<CombinationRenew>();

        public List<ItemModel> Materials { get; private set; }
        public ResultModel Result { get; private set; }

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["Materials"] = ByteSerializer.Serialize(Materials),
            }.ToImmutableDictionary();

        public CombinationRenew()
        {
            Materials = new List<ItemModel>();
        }

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Materials = ByteSerializer.Deserialize<List<ItemModel>>((byte[]) plainValue["Materials"]);
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            Debug.Log($"CombinationRenew action called. Rehearsal : {actionCtx.Rehearsal}");
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer) ?? CreateNovice.CreateContext("dummy");
            if (actionCtx.Rehearsal)
            {
                return states.SetState(actionCtx.Signer, ctx);
            }

            // 인벤토리에 재료를 갖고 있는지 검증.
            var pairs = new List<ItemModelInventoryItemPair>();
            for (var i = Materials.Count - 1; i >= 0; i--)
            {
                var m = Materials[i];
                try
                {
                    var inventoryItem =
                        ctx.avatar.Items.First(item => item.Item.Data.id == m.Id && item.Count >= m.Count);
                    pairs.Add(new ItemModelInventoryItemPair(m, inventoryItem));
                }
                catch (Exception e)
                {
                    Result = new ResultModel() {ErrorCode = ErrorCode.Fail};
                    EndOfExecuteSubject.OnNext(this);

                    return states.SetState(actionCtx.Signer, ctx);
                }
            }

            // 조합식 테이블 로드.
            var recipeTable = new Table<Recipe>();
            var recipeTableRawDataPath = Path.Combine(Directory.GetCurrentDirectory(), RecipePath);
            recipeTable.Load(File.ReadAllText(recipeTableRawDataPath));

            // 조합식 검증.
            Recipe resultItem = null;
            var resultCount = 0;
            using (var e = recipeTable.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (!e.Current.Value.IsMatch(Materials))
                    {
                        continue;
                    }

                    resultItem = e.Current.Value;
                    resultCount = e.Current.Value.CalculateCount(Materials);
                    break;
                }
            }

            // 제거가 잘 됐는지 로그 찍기.
            ctx.avatar.Items.ForEach(item => Debug.Log($"제거 전 // Id:{item.Item.Data.id}, Count:{item.Count}"));
            
            // 사용한 재료를 인벤토리에서 제거.
            pairs.ForEach(pair =>
            {
                Debug.Log($"제거 // pair.InventoryItem.Count:{pair.InventoryItem.Count}, pair.ItemModel.Count:{pair.ItemModel.Count}");
                pair.InventoryItem.Count -= pair.ItemModel.Count;
                if (pair.InventoryItem.Count == 0)
                {
                    ctx.avatar.Items.Remove(pair.InventoryItem);
                }
            });
            
            // 제거가 잘 됐는지 로그 찍기.
            ctx.avatar.Items.ForEach(item => Debug.Log($"제거 후 // Id:{item.Item.Data.id}, Count:{item.Count}"));

            // 뽀각!!
            if (ReferenceEquals(resultItem, null) ||
                resultCount == 0)
            {
                Result = new ResultModel() {ErrorCode = ErrorCode.Fail};
                EndOfExecuteSubject.OnNext(this);

                Debug.Log($"Before: {states.UpdatedAddresses}");
                var delta2 = states.SetState(actionCtx.Signer, ctx);
                Debug.Log($"After: {delta2.UpdatedAddresses}");
                return delta2;
            }
            
            // 조합 결과 획득.
            {
                var itemTable = ActionManager.Instance.tables.ItemEquipment;
                ItemEquipment itemData;
                if (itemTable.TryGetValue(resultItem.Id, out itemData))
                {
                    try
                    {
                        var inventoryItem = ctx.avatar.Items.First(item => item.Item.Data.id == resultItem.Id);
                        inventoryItem.Count += resultCount;
                    }
                    catch (Exception e)
                    {
                        var itemBase = ItemBase.ItemFactory(itemData);
                        ctx.avatar.Items.Add(new Inventory.InventoryItem(itemBase, 1));   
                    }
                }
                else
                {
                    Result = new ResultModel() {ErrorCode = ErrorCode.KeyNotFoundInTable};
                    EndOfExecuteSubject.OnNext(this);
                    
                    Debug.Log($"Before: {states.UpdatedAddresses}");
                    var delta2 = states.SetState(actionCtx.Signer, ctx);
                    Debug.Log($"After: {delta2.UpdatedAddresses}");
                    return delta2;
                }
            }
            
            // 획득이 잘 됐는지 로그 찍기.
            ctx.avatar.Items.ForEach(item => Debug.Log($"획득 후 // Id:{item.Item.Data.id}, Count:{item.Count}"));

            Result = new ResultModel()
            {
                ErrorCode = ErrorCode.Success,
                Item = new ItemModel(resultItem.Id, resultCount)
            };
            EndOfExecuteSubject.OnNext(this);

            Debug.Log($"Before: {states.UpdatedAddresses}");
            var delta = states.SetState(actionCtx.Signer, ctx);
            Debug.Log($"After: {delta.UpdatedAddresses}");
            return delta;
        }
    }
}
