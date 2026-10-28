// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CCGKit
{
    public class EffectResolutionSystem : BaseSystem
    {
        public HandPresentationSystem HandPresentationSystem;
        public CardWithArrowSelectionSystem CardWithArrowSelectionSystem;

        private CharacterObject currentEnemy;

        public void SetCurrentEnemy(CharacterObject enemy)
        {
            currentEnemy = enemy;
        }

        public void ResolveCardEffects(RuntimeCard card, CharacterObject playerSelectedTarget)
        {
            foreach (var effect in card.Template.Effects)
            {
                // === Variables Echo integration ===
                var turns = FindObjectOfType<TurnManagementSystem>();
                if (turns != null && turns.IsEndOfGame()) return;
                // === End Variables Echo integration ===
                // === Variables Echo integration ===
                // Targeted attacks may also draw cards (e.g. Doubt).
                if (effect is DrawCardsEffect draw)
                    StartCoroutine(DrawCardsFromDeck(draw.Amount));
                // === End Variables Echo integration ===
                var targetableEffect = effect as TargetableEffect;
                if (targetableEffect != null)
                {
                    var targets = GetTargets(targetableEffect, playerSelectedTarget, true);
                    foreach (var target in targets)
                    {
                        targetableEffect.Resolve(Player.Character, target.Character);
                        foreach (var group in targetableEffect.SourceActions)
                        {
                            foreach (var action in group.Group.Actions)
                            {
                                action.Execute(Player.gameObject);
                            }
                        }
                        foreach (var group in targetableEffect.TargetActions)
                        {
                            foreach (var action in group.Group.Actions)
                            {
                                var enemy = CardWithArrowSelectionSystem.GetSelectedEnemy();
                                action.Execute(enemy.gameObject);
                            }
                        }
                    }
                }
            }
        }

        public void ResolveCardEffects(RuntimeCard card)
        {
            foreach (var effect in card.Template.Effects)
            {
                // === Variables Echo integration ===
                var turns = FindObjectOfType<TurnManagementSystem>();
                if (turns != null && turns.IsEndOfGame()) return;
                // === End Variables Echo integration ===
                var targetableEffect = effect as TargetableEffect;
                if (targetableEffect != null)
                {
                    targetableEffect.Resolve(Player.Character, Player.Character);
                    foreach (var group in targetableEffect.SourceActions)
                    {
                        foreach (var action in group.Group.Actions)
                        {
                            action.Execute(Player.gameObject);
                        }
                    }
                    foreach (var group in targetableEffect.TargetActions)
                    {
                        foreach (var action in group.Group.Actions)
                        {
                            action.Execute(currentEnemy.gameObject);
                        }
                    }
                }

                var drawCardsEffect = effect as DrawCardsEffect;
                if (drawCardsEffect != null)
                {
                    StartCoroutine(DrawCardsFromDeck(drawCardsEffect.Amount));
                }
            }
        }

        private IEnumerator DrawCardsFromDeck(int amount)
        {
            // Card drawing effects need to wait for the played card to be moved to the discard pile.
            yield return new WaitForSeconds(HandPresentationSystem.CardToDiscardPileAnimationTime + 0.1f);
            var deckDrawingSystem = FindObjectOfType<DeckDrawingSystem>();
            deckDrawingSystem.DrawCardsFromDeck(amount);
        }

        public void ResolveEnemyEffects(CharacterObject enemy, List<Effect> effects)
        {
            foreach (var effect in effects)
            {
                // === Variables Echo integration ===
                var turns = FindObjectOfType<TurnManagementSystem>();
                if (turns != null && turns.IsEndOfGame()) return;
                // === End Variables Echo integration ===
                var targetableEffect = effect as TargetableEffect;
                if (targetableEffect != null)
                {
                    var targets = GetTargets(targetableEffect, null, false);
                    foreach (var target in targets)
                    {
                        targetableEffect.Resolve(enemy.Character, target.Character);
                        foreach (var group in targetableEffect.SourceActions)
                        {
                            foreach (var action in group.Group.Actions)
                            {
                                action.Execute(enemy.gameObject);
                            }
                        }
                        foreach (var group in targetableEffect.TargetActions)
                        {
                            foreach (var action in group.Group.Actions)
                            {
                                action.Execute(target.gameObject);
                            }
                        }
                    }
                }
            }
        }

        private List<CharacterObject> GetTargets(
            TargetableEffect effect,
            CharacterObject playerSelectedTarget,
            bool playerInstigator)
        {
            var targets = new List<CharacterObject>(4);

            if (playerInstigator)
            {
                switch (effect.Target)
                {
                    case EffectTargetType.Self:
                        targets.Add(Player);
                        break;

                    case EffectTargetType.TargetEnemy:
                        targets.Add(playerSelectedTarget);
                        break;

                    case EffectTargetType.AllEnemies:
                        foreach (var enemy in Enemies)
                        {
                            targets.Add(enemy);
                        }
                        break;

                    case EffectTargetType.All:
                        targets.Add(Player);
                        foreach (var enemy in Enemies)
                        {
                            targets.Add(enemy);
                        }
                        break;
                }
            }
            else
            {
                switch (effect.Target)
                {
                    case EffectTargetType.Self:
                        targets.Add(currentEnemy);
                        break;

                    case EffectTargetType.TargetEnemy:
                    case EffectTargetType.AllEnemies:
                        targets.Add(Player);
                        break;

                    case EffectTargetType.All:
                        targets.Add(Player);
                        targets.Add(currentEnemy);
                        break;
                }
            }

            return targets;
        }
    }
}


