using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        private static readonly string DimmedString = nameof(ItemBase.ItemType.Material);
        
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

        public Shop(Game.Item.Inventory inventory, IDictionary<Address, List<Game.Item.ShopItem>> shopItems)
        {
            this.inventory.Value = new Inventory(inventory);
            this.shopItems.Value = new ShopItems(shopItems);
            itemInfo.Value = new ItemInfo();
            itemCountAndPricePopup.Value = new ItemCountAndPricePopup();

            state.Subscribe(OnState);
            this.inventory.Value.selectedItemView.Subscribe(OnSelectInventoryItem);
            this.shopItems.Value.selectedItem.Subscribe(OnSelectShopItem);
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

        private static bool ButtonEnabledFuncForBuy(InventoryItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   ReactiveAgentState.Gold.Value >= shopItem.price.Value;
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
        
        public void OnClickItemInfo(CountableItem inventoryItem)
        {
            switch (inventoryItem)
            {
                case null:
                    return;
                case ShopItem shopItem:
                {
                    if (state.Value == State.Buy)
                    {
                        // 구매하겠습니까?
                        itemCountAndPricePopup.Value.titleText.Value = "구매";
                        itemCountAndPricePopup.Value.submitText.Value = "확인";
                    }
                    else
                    {
                        // 판매 취소하겠습니까?
                        itemCountAndPricePopup.Value.titleText.Value = "판매 취소";
                        itemCountAndPricePopup.Value.submitText.Value = "확인";
                    }
                
                    itemCountAndPricePopup.Value.countEnabled.Value = false;
                    itemCountAndPricePopup.Value.price.Value = shopItem.price.Value;
                    itemCountAndPricePopup.Value.priceInteractable.Value = false;
                    itemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                        inventoryItem.item.Value,
                        shopItem.count.Value,
                        shopItem.count.Value,
                        shopItem.count.Value);

                    return;
                }
            }

            // 판매하겠습니까?
            itemCountAndPricePopup.Value.titleText.Value = "판매 설정";
            itemCountAndPricePopup.Value.submitText.Value = "판매";
            itemCountAndPricePopup.Value.countEnabled.Value = true;
            itemCountAndPricePopup.Value.price.Value = 1;
            itemCountAndPricePopup.Value.priceInteractable.Value = true;
            itemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                inventoryItem.item.Value,
                1,
                1,
                inventoryItem.count.Value);
        }

        private void OnSelectInventoryItem(InventoryItemView view)
        {
            if (itemInfo.Value.item.Value is ShopItem)
            {
                if (view is null)
                {
                    // 초기화 단계에서 `inventory.Value.selectedItem.Subscribe(OnSelectInventoryItem);` 라인을 통해
                    // 구독할 때, 한 번 반드시 이 라인에 들어옵니다.
                    // 이때 예외가 발생하지 않아야 해서 수정합니다.
                    return; // throw new UnexpectedOperationException();
                }
                
                shopItems.Value.DeselectAll();
            }
            
            itemInfo.Value.item.Value = view?.Model;
            itemInfo.Value.priceEnabled.Value = false;
        }

        private void OnSelectShopItem(ShopItem shopItem)
        {
            if (!(itemInfo.Value.item.Value is ShopItem))
            {
                if (ReferenceEquals(shopItem, null))
                {
                    // 초기화 단계에서 `shopItems.Value.selectedItem.Subscribe(OnSelectShopItem);` 라인을 통해
                    // 구독할 때, 한 번 반드시 이 라인에 들어옵니다.
                    // 이때 예외가 발생하지 않아야 해서 수정합니다.
                    return; // throw new UnexpectedOperationException();
                }
                
                inventory.Value.DeselectAll();
            }
            
            itemInfo.Value.item.Value = shopItem;

            if (ReferenceEquals(shopItem, null))
            {
                itemInfo.Value.priceEnabled.Value = false;    
            }
            else
            {
                itemInfo.Value.price.Value = shopItem.price.Value;
                itemInfo.Value.priceEnabled.Value = true;
            }
        }
    }
}
