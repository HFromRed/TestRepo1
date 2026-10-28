// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// The base class of the card selection systems. It contains the shared code of both
    /// selection systems (with and without a targeting arrow).
    /// </summary>
    public abstract class CardSelectionSystem : BaseSystem
    {
        public IntVariable PlayerMana;

        public TurnManagementSystem TurnManagementSystem;
        public DeckDrawingSystem DeckDrawingSystem;
        public HandPresentationSystem HandPresentationSystem;
        public EffectResolutionSystem EffectResolutionSystem;

        protected Camera MainCamera;
        protected LayerMask CardLayer;
        protected LayerMask EnemyLayer;

        protected GameObject SelectedCard;

        protected virtual void Start()
        {
            CardLayer = 1 << LayerMask.NameToLayer("Card");
            EnemyLayer = 1 << LayerMask.NameToLayer("Enemy");
            MainCamera = Camera.main;
        }

        protected virtual void PlaySelectedCard()
        {
            var cardObject = SelectedCard.GetComponent<CardObject>();
            cardObject.SetInteractable(false);
            var cardTemplate = cardObject.Template;
            // === Variables Echo integration ===
            // 崩溃档噪声会让卡牌实际费用发生偏移，必须按 CardObject 的费用扣法力。
            PlayerMana.SetValue(PlayerMana.Value - cardObject.EffectiveCost);
            // === End Variables Echo integration ===

            HandPresentationSystem.RearrangeHand(SelectedCard);
            HandPresentationSystem.RemoveCardFromHand(SelectedCard);
            HandPresentationSystem.MoveCardToDiscardPile(SelectedCard);

            DeckDrawingSystem.MoveCardToDiscardPile(cardObject.RuntimeCard);

            // === Variables Echo integration ===
            // 在效果结算之前记录出牌、结算卡牌噪声与行为标签。
            EchoRuntime.OnCardPlayed(cardTemplate);
            // === End Variables Echo integration ===
        }

        public bool HasSelectedCard()
        {
            return SelectedCard != null;
        }
    }
}
