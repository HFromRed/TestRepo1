// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;
using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// This system is responsible for drawing cards from the player's deck into the player's hand.
    /// Drawing cards from the deck can happen in the following situations:
    ///     - At the beginning of a game.
    ///     - At the beginning of the player's turn.
    ///     - As the result of a card effect ("Draw X cards").
    /// This system processes request-like entities with the component DrawCardsFromDeck attached to
    /// them. This component has an Amount field indicating the number of cards to draw. If there are
    /// not enough cards to draw from the deck, the discard pile is shuffled into the deck and cards
    /// are then drawn again from it.
    /// </summary>
    public class DeckDrawingSystem : MonoBehaviour
    {
        public HandPresentationSystem HandPresentationSystem;

        private List<RuntimeCard> deck;
        private List<RuntimeCard> discardPile;
        private List<RuntimeCard> hand;

        // === Variables Echo integration ===
        // 永久牌（服从牌）暂存区：不参与普通弃牌循环，但会在洗回牌库时重新进入牌库。
        private readonly List<RuntimeCard> permanentPile = new List<RuntimeCard>(8);
        // === End Variables Echo integration ===

        private DeckWidget deckWidget;
        private DiscardPileWidget discardPileWidget;

        // Change these values to the ones that make the most sense for your game.
        private const int InitialDeckCapacity = 30;
        private const int InitialDiscardPileCapacity = 30;
        private const int InitialHandCapacity = 30;

        public void Initialize(DeckWidget deck, DiscardPileWidget discardPile)
        {
            deckWidget = deck;
            discardPileWidget = discardPile;
        }

        private void Start()
        {
            deck = new List<RuntimeCard>(InitialDeckCapacity);
            discardPile = new List<RuntimeCard>(InitialDiscardPileCapacity);
            hand = new List<RuntimeCard>(InitialHandCapacity);
        }

        public List<RuntimeCard> GetDeck()
        {
            return deck;
        }

        public List<RuntimeCard> GetDiscardPile()
        {
            return discardPile;
        }

        public int LoadDeck(List<CardTemplate> playerDeck)
        {
            var deckSize = 0;

            foreach (var template in playerDeck)
            {
                // Skip over invalid entries.
                if (template == null)
                    continue;

                var card = new RuntimeCard 
                {
                    Template = template
                };
                deck.Add(card);

                ++deckSize;
            }

            deckWidget.SetAmount(deck.Count);
            discardPileWidget.SetAmount(0);

            return deckSize;
        }

        public void ShuffleDeck()
        {
            // === Variables Echo integration ===
            if (EchoRuntime.Rng != null)
                deck.Shuffle(EchoRuntime.Rng.Combat);
            else
                deck.Shuffle();
            // === End Variables Echo integration ===
        }

        public void DrawCardsFromDeck(int amount)
        {
            var deckSize = deck.Count;
            // If there are enough cards in the deck, just draw the cards from it.
            if (deckSize >= amount)
            {
                var prevDeckSize = deckSize;

                var drawnCards = new List<RuntimeCard>(amount);
                for (var i = 0; i < amount; i++)
                {
                    var card = deck[0];
                    deck.RemoveAt(0);
                    hand.Add(card);
                    drawnCards.Add(card);
                }

                HandPresentationSystem.CreateCardsInHand(drawnCards, prevDeckSize);
            }
            // If there are not enough cards in the deck, first shuffle the discard pile into
            // the deck and then draw from the deck normally.
            else
            {
                for (var i = 0; i < discardPile.Count; i++)
                    deck.Add(discardPile[i]);

                discardPile.Clear();

                // === Variables Echo integration ===
                // 永久牌也随弃牌堆一起洗回牌库。
                for (var i = 0; i < permanentPile.Count; i++)
                    deck.Add(permanentPile[i]);
                permanentPile.Clear();
                ShuffleDeck(); // Recycled cards must not retain the order they were played.
                // === End Variables Echo integration ===

                HandPresentationSystem.UpdateDiscardPileSize(discardPile.Count);

                // Prevent trying to draw more cards than those available.
                if (amount > deck.Count + discardPile.Count)
                {
                    amount = deck.Count + discardPile.Count;
                }
                DrawCardsFromDeck(amount);
            }
        }

        public void MoveCardToDiscardPile(RuntimeCard card)
        {
            var idx = hand.IndexOf(card);
            if (idx >= 0)
                hand.RemoveAt(idx);

            // === Variables Echo integration ===
            if (card != null && card.Template != null && card.Template.IsPermanent)
            {
                if (!permanentPile.Contains(card))
                    permanentPile.Add(card);
                return;
            }
            // === End Variables Echo integration ===

            discardPile.Add(card);
        }

        public void MoveCardsToDiscardPile()
        {
            // === Variables Echo integration ===
            foreach (var card in hand)
            {
                if (card != null && card.Template != null && card.Template.IsPermanent)
                {
                    if (!permanentPile.Contains(card))
                        permanentPile.Add(card);
                }
                else
                {
                    discardPile.Add(card);
                }
            }
            // === End Variables Echo integration ===

            hand.Clear();
        }

        // === Variables Echo integration ===
        /// <summary>
        /// 供黑市/事件删除卡牌使用。默认不会移除 IsPermanent 的服从牌。
        /// </summary>
        public bool TryRemoveCard(string cardId, bool allowPermanent = false)
        {
            if (string.IsNullOrEmpty(cardId))
                return false;

            if (RemoveCardFromList(deck, cardId, allowPermanent))
            {
                if (deckWidget != null)
                    deckWidget.SetAmount(deck.Count);
                return true;
            }

            if (RemoveCardFromList(discardPile, cardId, allowPermanent))
            {
                if (discardPileWidget != null)
                    discardPileWidget.SetAmount(discardPile.Count);
                return true;
            }

            return false;
        }

        private static bool RemoveCardFromList(List<RuntimeCard> list, string cardId, bool allowPermanent)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var card = list[i];
                if (card == null || card.Template == null)
                    continue;
                if (!allowPermanent && card.Template.IsPermanent)
                    continue;
                if (EchoRuntime.StableId(card.Template) != cardId)
                    continue;

                list.RemoveAt(i);
                return true;
            }

            return false;
        }
        // === End Variables Echo integration ===
    }
}

