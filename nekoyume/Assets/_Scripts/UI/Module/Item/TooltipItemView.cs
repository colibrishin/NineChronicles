﻿using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(BaseItemView))]
    public class TooltipItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        public void Set(ItemBase itemBase, int count)
        {
            baseItemView.Container.SetActive(true);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusTouchHandler.gameObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.DisableObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.SelectEnchantItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = baseItemView.GetItemIcon(itemBase);

            var data = baseItemView.GetItemViewData(itemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (itemBase is Equipment equipment && equipment.level > 0)
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

            baseItemView.OptionTag.Set(itemBase);

            baseItemView.CountText.gameObject.SetActive(itemBase.ItemType == ItemType.Material);
            baseItemView.CountText.text = count.ToString();
        }
    }
}