using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveCollection<InventoryItem> items = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveProperty<InventoryItem> selectedItem = new ReactiveProperty<InventoryItem>(null);
        public readonly ReactiveProperty<Func<InventoryItem, bool>> dimmedFunc = new ReactiveProperty<Func<InventoryItem, bool>>();
        public readonly ReactiveProperty<Func<InventoryItem, ItemBase.ItemType, bool>> glowedFunc = new ReactiveProperty<Func<InventoryItem, ItemBase.ItemType, bool>>();

        public readonly Subject<InventoryItem> onDoubleClickItem = new Subject<InventoryItem>();
        
        public Inventory(Game.Item.Inventory inventory)
        {
            dimmedFunc.Value = DimmedFunc;
            glowedFunc.Value = GlowedFunc;

            foreach (var item in inventory.Items)
            {
                var inventoryItem = new InventoryItem(item.item, item.count);
                InitInventoryItem(inventoryItem);
                items.Add(inventoryItem);
            }
            
            items.ObserveAdd().Subscribe(added =>
            {
                InitInventoryItem(added.Value);
            });
            items.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            
            dimmedFunc.Subscribe(func =>
            {
                if (dimmedFunc.Value == null)
                {
                    dimmedFunc.Value = DimmedFunc;
                }
                
                foreach (var item in items)
                {
                    item.dimmed.Value = dimmedFunc.Value(item);
                }
            });
        }

        public void Dispose()
        {
            items.DisposeAll();
            selectedItem.DisposeAll();
            dimmedFunc.Dispose();
            glowedFunc.Dispose();
            
            onDoubleClickItem.Dispose();
        }

        public InventoryItem AddFungibleItem(ItemBase itemBase, int count)
        {
            if (TryGetFungibleItem(itemBase, out var inventoryItem))
            {
                inventoryItem.count.Value += count;
                return inventoryItem;
            }

            inventoryItem = new InventoryItem(itemBase, count);
            items.Add(inventoryItem);
            return inventoryItem;
        }
        
        // Todo. UnfungibleItem 개발 후 `ItemUsable itemBase` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public InventoryItem AddUnfungibleItem(ItemUsable itemBase)
        {
            var inventoryItem = new InventoryItem(itemBase, 1);
            items.Add(inventoryItem);
            return inventoryItem;
        }

        public void RemoveFungibleItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (ReferenceEquals(countEditableItem, null))
                {
                    continue;
                }

                RemoveFungibleItem(countEditableItem.item.Value.Data.id, countEditableItem.count.Value);
            }
        }

        public bool RemoveFungibleItem(int id, int count = 1)
        {
            if (!TryGetFungibleItem(id, out var outFungibleItem) ||
                outFungibleItem.count.Value < count)
            {
                return false;
            }

            outFungibleItem.count.Value -= count;
            if (outFungibleItem.count.Value == 0)
            {
                items.Remove(outFungibleItem);
            }

            return true;
        }
        
        // Todo. UnfungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public bool RemoveUnfungibleItem(ItemUsable itemUsable)
        {
            return TryGetUnfungibleItem(itemUsable, out var outFungibleItem) && items.Remove(outFungibleItem);
        }

        public void DeselectAll()
        {
            if (ReferenceEquals(selectedItem.Value, null))
            {
                return;
            }

            selectedItem.Value.selected.Value = false;
            selectedItem.Value = null;
        }
        
        public void SubscribeOnClick(InventoryItem inventoryItem)
        {
            if (!ReferenceEquals(selectedItem.Value, null))
            {
                selectedItem.Value.selected.Value = false;
            }

            selectedItem.Value = inventoryItem;
            selectedItem.Value.selected.Value = true;

            foreach (var item in items)
            {
                item.glowed.Value = false;
            }
        }
        
        private bool TryGetFungibleItem(ItemBase itemBase, out InventoryItem outInventoryItem)
        {
            return TryGetFungibleItem(itemBase.Data.id, out outInventoryItem);
        }
        
        private bool TryGetFungibleItem(int id, out InventoryItem outFungibleItem)
        {
            foreach (var fungibleItem in items)
            {
                if (fungibleItem.item.Value.Data.id != id)
                {
                    continue;
                }
                
                outFungibleItem = fungibleItem;
                return true;
            }

            outFungibleItem = null;
            return false;
        }
        
        // Todo. UnfungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        private bool TryGetUnfungibleItem(ItemUsable itemUsable, out InventoryItem outUnfungibleItem)
        {
            foreach (var fungibleItem in items)
            {
                if (fungibleItem.item.Value.Data.id != itemUsable.Data.id)
                {
                    continue;
                }
                
                outUnfungibleItem = fungibleItem;
                return true;
            }

            outUnfungibleItem = null;
            return false;
        }

        private void InitInventoryItem(InventoryItem item)
        {
            item.dimmed.Value = dimmedFunc.Value(item);
            item.onClick.Subscribe(SubscribeOnClick);
            item.onDoubleClick.Subscribe(onDoubleClickItem);
        }
        
        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private bool GlowedFunc(InventoryItem inventoryItem, Game.Item.ItemBase.ItemType type)
        {
            return false;
        }
    }
}
