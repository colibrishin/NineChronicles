using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        private static readonly string DimmedString = ItemBase.ItemType.Material.ToString();
        
        public enum State
        {
            Buy, Sell
        }
        
        public readonly ReactiveProperty<State> state = new ReactiveProperty<State>();
        public readonly ReactiveProperty<Inventory> inventory = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ShopItems> shopItems = new ReactiveProperty<ShopItems>();
        public readonly ReactiveProperty<ItemInfo> itemInfo = new ReactiveProperty<ItemInfo>();
        public readonly ReactiveProperty<ItemCountAndPricePopup> itemCountAndPricePopup = new ReactiveProperty<ItemCountAndPricePopup>();
        
        public readonly Subject<Shop> onClickSwitchBuy = new Subject<Shop>();
        public readonly Subject<Shop> onClickSwitchSell = new Subject<Shop>();
        public readonly Subject<Shop> onClickClose = new Subject<Shop>();

        public Shop(List<Game.Item.Inventory.InventoryItem> items, Game.Shop shop)
        {
            inventory.Value = new Inventory(items);
            shopItems.Value = new ShopItems(shop);
            itemInfo.Value = new ItemInfo();
            itemCountAndPricePopup.Value = new ItemCountAndPricePopup();

            state.Subscribe(OnState);
            inventory.Value.selectedItem.Subscribe(OnSelectInventoryItem);
            shopItems.Value.selectedItem.Subscribe(OnSelectShopItem);
            itemInfo.Value.item.Subscribe(OnItemInfoItem);
            itemInfo.Value.onClick.Subscribe(OnClickItemInfo);

            onClickSwitchBuy.Subscribe(_ => state.Value = State.Buy);
            onClickSwitchSell.Subscribe(_ => state.Value = State.Sell);
        }
        
        public void Dispose()
        {
            state.Dispose();
            inventory.DisposeAll();
            shopItems.DisposeAll();
            itemInfo.DisposeAll();
            itemCountAndPricePopup.DisposeAll();
            
            onClickSwitchBuy.Dispose();
            onClickSwitchSell.Dispose();
            onClickClose.Dispose();
        }

        private void OnState(State value)
        {
            inventory.Value.DeselectAll();
            shopItems.Value.DeselectAll();
            
            switch (value)
            {
                case State.Buy:
                    inventory.Value.dimmedFunc.Value = null;
                    itemInfo.Value.buttonText.Value = "구매하기";
                    itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFuncForBuy;
                    break;
                case State.Sell:
                    inventory.Value.dimmedFunc.Value = DimmedFuncForSell;
                    itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFuncForSell;
                    break;
            }
        }
        
        private static bool DimmedFuncForSell(InventoryItem inventoryItem)
        {
            return inventoryItem.item.Value.Data.cls == DimmedString;
        }

        private bool ButtonEnabledFuncForBuy(InventoryItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   Nekoyume.Model.Agent.Gold.Value >= shopItem.price.Value;
        }

        private bool ButtonEnabledFuncForSell(InventoryItem inventoryItem)
        {
            switch (inventoryItem)
            {
                case null:
                    return false;
                case ShopItem _:
                    itemInfo.Value.buttonText.Value = "판매 취소";
                    return true;
                default:
                    itemInfo.Value.buttonText.Value = "판매하기";
                    return !inventoryItem.dimmed.Value;
            }
        }

        private void OnItemInfoItem(InventoryItem inventoryItem)
        {
            Debug.Log("OnItemInfoItem");
        }
        
        private void OnClickItemInfo(ItemInfo itemInfo)
        {
            if (ReferenceEquals(itemInfo, null) ||
                ReferenceEquals(itemInfo.item.Value, null))
            {
                return;
            }

            switch (state.Value)
            {
                case State.Buy:
                    // 구매하겠습니까?
                    return;
            }
            
            if (itemInfo.item.Value is ShopItem)
            {
                // 판매 취소하겠습니까?
            }
            else
            {
                itemCountAndPricePopup.Value.titleText.Value = "판매 설정";
                itemCountAndPricePopup.Value.submitText.Value = "판매";
                itemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                    itemInfo.item.Value.item.Value,
                    itemInfo.item.Value.count.Value,
                    0,
                    itemInfo.item.Value.count.Value,
                    "수정");
                itemCountAndPricePopup.Value.price.Value = 1;
            }
        }

        private void OnSelectInventoryItem(InventoryItem inventoryItem)
        {
            if (itemInfo.Value.item.Value is ShopItem)
            {
                if (ReferenceEquals(inventoryItem, null))
                {
                    return;
                }
                
                shopItems.Value.DeselectAll();
            }
            
            itemInfo.Value.item.Value = inventoryItem;
        }

        private void OnSelectShopItem(ShopItem shopItem)
        {
            if (!(itemInfo.Value.item.Value is ShopItem))
            {
                if (ReferenceEquals(shopItem, null))
                {
                    return;
                }
                
                inventory.Value.DeselectAll();
            }
            
            itemInfo.Value.item.Value = shopItem;
        }
    }
}
