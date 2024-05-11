﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Trainworks.ConstantsV2;
using Trainworks.BuildersV2;
using ShinyShoe;
using System.Linq;
using UnityEngine;
using HarmonyLib;


namespace CustomEffectsPathogens
{
    internal class CardEffectScaleOnShards : CardEffectBase
    {
        /*public override IEnumerator ApplyEffect(CardEffectState cardEffectState, CardEffectParams cardEffectParams)
        {
           
            int shardCount = cardEffectParams.selfTarget.GetStatusEffectStacks(VanillaStatusEffectIDs.Shard);
            foreach (CharacterState target in cardEffectParams.targets)
            {
                NotifyHealthEffectTriggered(cardEffectParams.saveManager, cardEffectParams.popupNotificationManager, GetActivatedDescription(cardEffectState), target.GetCharacterUI());
                target.BuffDamage(cardEffectState.GetParamInt() * shardCount);
                yield return target.BuffMaxHP(cardEffectState.GetParamInt() * shardCount, triggerOnHeal: false);
            }
        }*/
        public override bool TestEffect(CardEffectState cardEffectState, CardEffectParams cardEffectParams)
        {
            return cardEffectParams.targets.Count > 0;
        }

        public override IEnumerator ApplyEffect(CardEffectState cardEffectState, CardEffectParams cardEffectParams)
        {
            int shardCount = cardEffectParams.selfTarget.GetStatusEffectStacks(VanillaStatusEffectIDs.Shard);
            foreach (CharacterState target in cardEffectParams.targets)
            {
                CardUpgradeState upgradeState = new CardUpgradeState();
                upgradeState.Setup(cardEffectState.GetParamCardUpgradeData());
                Traverse.Create((object)upgradeState).Field("attackDamage").SetValue((object)(upgradeState.GetAttackDamage() * shardCount));
                Traverse.Create((object)upgradeState).Field("additionalHP").SetValue((object)(upgradeState.GetAdditionalHP() * shardCount));
                if (cardEffectParams.playedCard != null && !upgradeState.IsUnitSynthesisUpgrade())
                {
                    foreach (CardTraitState traitState in cardEffectParams.playedCard.GetTraitStates())
                    {
                        traitState.OnApplyingCardUpgradeToUnit(cardEffectParams.playedCard, target, upgradeState, cardEffectParams.cardManager);
                    }
                }
                int attackDamage = upgradeState.GetAttackDamage();
                int additionalHP = upgradeState.GetAdditionalHP();
                int additionalSize = upgradeState.GetAdditionalSize();
                string text = ((attackDamage != 0) ? GetAttackNotificationText(upgradeState) : null);
                string text2 = ((additionalHP != 0) ? GetHPNotificationText(upgradeState) : null);
                string text3 = ((additionalSize != 0) ? GetSizeNotificationText(upgradeState) : null);
                string[] array = new string[3] { text, text2, text3 };
                int num = array.Count((string n) => n != null);
                string text4 = string.Empty;
                switch (num)
                {
                    case 3:
                        text4 = string.Format("TextFormat_SpacedItems3".Localize(), array[0], array[1], array[2]);
                        break;
                    case 2:
                        {
                            List<string> list = array.Where((string n) => !string.IsNullOrEmpty(n)).ToList();
                            text4 = string.Format("TextFormat_SpacedItems".Localize(), list[0], list[1]);
                            break;
                        }
                    case 1:
                        text4 = array.Where((string n) => !string.IsNullOrEmpty(n)).ToList()[0];
                        break;
                }
                if (text4 != null)
                {
                    NotifyHealthEffectTriggered(cardEffectParams.saveManager, cardEffectParams.popupNotificationManager, text4, target.GetCharacterUI());
                }
                yield return target.ApplyCardUpgrade(upgradeState);
                
                CardState spawnerCard = target.GetSpawnerCard();
                if (spawnerCard != null && !cardEffectParams.saveManager.PreviewMode && (target.GetSourceCharacterData() == spawnerCard.GetSpawnCharacterData() || spawnerCard.GetSpawnCharacterData() == null))
                {
                    CardAnimator.CardUpgradeAnimationInfo type = new CardAnimator.CardUpgradeAnimationInfo(spawnerCard, upgradeState);
                    CardAnimator.DoAddRecentCardUpgrade.Dispatch(type);
                    spawnerCard.GetTemporaryCardStateModifiers().AddUpgrade(upgradeState);
                    spawnerCard.UpdateCardBodyText();
                    cardEffectParams.cardManager?.RefreshCardInHand(spawnerCard);
                }
            }
        }

        private string GetAttackNotificationText(CardUpgradeState upgradeState)
        {
            return CardEffectBuffDamage.GetNotificationText(upgradeState.GetAttackDamage());
        }

        private string GetHPNotificationText(CardUpgradeState upgradeState)
        {
            int additionalHP = upgradeState.GetAdditionalHP();
            if (additionalHP >= 0)
            {
                return CardEffectBuffMaxHealth.GetNotificationText(additionalHP);
            }
            return CardEffectDebuffMaxHealth.GetNotificationText(Mathf.Abs(additionalHP));
        }

        private string GetSizeNotificationText(CardUpgradeState upgradeState)
        {
            return string.Format("SizeNotificationText".Localize(), string.Format("TextFormat_Add".Localize(), upgradeState.GetAdditionalSize()));
        }

        public override void GetTooltipsStatusList(CardEffectState cardEffectState, ref List<string> outStatusIdList)
        {
            GetTooltipsStatusList(cardEffectState.GetSourceCardEffectData(), ref outStatusIdList);
        }

        public static void GetTooltipsStatusList(CardEffectData cardEffectData, ref List<string> outStatusIdList)
        {
            foreach (StatusEffectStackData statusEffectUpgrade in cardEffectData.GetParamCardUpgradeData().GetStatusEffectUpgrades())
            {
                outStatusIdList.Add(statusEffectUpgrade.statusId);
            }
        }

        public string GetTipTooltipKey(CardEffectState cardEffectState)
        {
            if (cardEffectState.GetParamCardUpgradeData() != null && cardEffectState.GetParamCardUpgradeData().HasUnitStatUpgrade())
            {
                return "TipTooltip_StatChangesStick";
            }
            return null;
        }
    }

}
