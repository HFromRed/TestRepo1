// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// Miscellaneous card-related utilities.
    /// </summary>
    public static class CardUtils
    {
        public static bool CardCanBePlayed(CardTemplate card, IntVariable playerMana)
        {
            // The player can only play a card if it has a cost that is lower or
            // equal to the player's available mana.
            return card.Cost <= playerMana.Value;
        }

        // === Variables Echo integration ===
        /// <summary>
        /// 以 CardObject 的实际费用（含崩溃档噪声偏移）判定能否打出。
        /// </summary>
        public static bool CardCanBePlayed(CardObject card, IntVariable playerMana)
        {
            return card != null && card.EffectiveCost <= playerMana.Value;
        }
        // === End Variables Echo integration ===

        public static bool CardHasTargetableEffect(CardTemplate card)
        {
            // Checks if the card contains an effect that targets a specific enemy.
            // This is used to determine whether the targeting arrow needs to be
            // enabled or not.
            foreach (var effect in card.Effects)
            {
                if (effect is TargetableEffect targetableEffect)
                {
                    if (targetableEffect.Target == EffectTargetType.TargetEnemy)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
