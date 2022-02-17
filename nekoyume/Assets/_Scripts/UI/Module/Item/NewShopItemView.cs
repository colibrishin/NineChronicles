﻿using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(BaseItemView))]
    public class NewShopItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(ShopItemViewModel model, Action<ShopItemViewModel> onClick)
        {
            if (model == null)
            {
                baseItemView.Container.SetActive(false);
                return;
            }

            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusTouchHandler.gameObject.SetActive(false);
            baseItemView.SelectEnchantItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.DisableObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = baseItemView.GetItemIcon(model.ItemBase);

            var data = baseItemView.GetItemViewData(model.ItemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (model.ItemBase is Equipment equipment && equipment.level > 0)
            {
                baseItemView.EnhancementText.gameObject.SetActive(true);
                baseItemView.EnhancementText.text = $"+{equipment.level}";
                if (equipment.level >= Util.VisibleEnhancementEffectLevel)
                {
                    baseItemView.EnhancementImage.material = data.EnhancementMaterial;
                    baseItemView.EnhancementImage.gameObject.SetActive(true);
                }
                else
                {
                    baseItemView.EnhancementImage.gameObject.SetActive(false);
                }
            }
            else
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
            }

            baseItemView.LevelLimitObject.SetActive(model.LevelLimited);

            baseItemView.OptionTag.Set(model.ItemBase);

            baseItemView.CountText.gameObject.SetActive(model.ItemBase.ItemType == ItemType.Material);
            baseItemView.CountText.text = model.OrderDigest.ItemCount.ToString();
            baseItemView.PriceText.text = model.OrderDigest.Price.GetQuantityString();

            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b)).AddTo(_disposables);
            model.Expired.Subscribe(b => baseItemView.ExpiredObject.SetActive(b)).AddTo(_disposables);
            model.View = GetComponent<RectTransform>();

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(onClick).AddTo(_disposables);
        }
    }
}