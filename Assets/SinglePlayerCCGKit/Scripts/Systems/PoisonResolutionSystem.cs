// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

namespace CCGKit
{
    /// <summary>
    /// This system is responsible for managing the poison status on characters.
    /// It deals poison damage at the beginning of the turn, and reduces the poison
    /// stack at the end of the turn.
    /// </summary>
    public class PoisonResolutionSystem : BaseSystem
    {
        public void OnPlayerTurnBegan()
        {
            var player = Player.Character;
            var poison = player.Status.GetValue("Poison");
            if (poison > 0)
            {
                var playerHp = player.Hp;
                playerHp.SetValue(playerHp.Value-1);
            }
        }

        public void OnPlayerTurnEnded()
        {
            var player = Player.Character;
            var poison = player.Status.GetValue("Poison");
            if (poison > 0)
            {
                player.Status.SetValue("Poison", poison-1);
            }
        }

        public void OnEnemyTurnBegan()
        {
            foreach (var enemy in Enemies)
            {
                var character = enemy.Character;
                var poison = character.Status.GetValue("Poison");
                if (poison > 0)
                {
                    var enemyHp = character.Hp;
                    if (enemyHp.Value > 0)
                    {
                        enemyHp.SetValue(enemyHp.Value-1);
                    }
                }
            }
        }

        public void OnEnemyTurnEnded()
        {
            foreach (var enemy in Enemies)
            {
                var character = enemy.Character;
                var poison = character.Status.GetValue("Poison");
                if (poison > 0)
                {
                    character.Status.SetValue("Poison", poison-1);
                }
            }
        }
    }
}
