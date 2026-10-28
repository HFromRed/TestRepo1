// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections;
using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// This system handles the death of a character to end the battle and display the end-of-battle
    /// popup on the screen.
    /// </summary>
    public class CharacterDeathSystem : BaseSystem
    {
#pragma warning disable 649
        [SerializeField]
        private float endBattlePopupDelay = 1.0f;

        [SerializeField]
        private GameEvent enemyDiedEvent;
#pragma warning restore 649
        
        public void OnPlayerHpChanged(int hp)
        {
            if (hp <= 0)
            {
                EndGame(true);
            }
        }

        public void OnEnemy1HpChanged(int hp)
        {
            if (hp <= 0)
            {
                Enemies[0].IsDead = true;
                enemyDiedEvent.Raise();
                if (IsEndOfGame())
                {
                    EndGame(false);
                }
            }
        }

        public void OnEnemy2HpChanged(int hp)
        {
            if (hp <= 0)
            {
                Enemies[1].IsDead = true;
                enemyDiedEvent.Raise();
                if (IsEndOfGame())
                {
                    EndGame(false);
                }
            }
        }

        private bool IsEndOfGame()
        {
            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead)
                    return false;
            }

            return true;
        }

        private bool ended;
        public void EndGame(bool playerDied)
        {
            // === Variables Echo integration ===
            if (ended) return;
            ended = true;
            var turns = FindObjectOfType<TurnManagementSystem>();
            if (turns != null) turns.SetEndOfGame(true);
            // 记录本场战斗结果；死亡/最终 BOSS 胜利时会结算跑图。
            EchoRuntime.OnBattleEnded(playerDied);
            // === End Variables Echo integration ===

            if (EchoChapterController.Active == null) StartCoroutine(ShowEndBattlePopup(playerDied));
        }

        private IEnumerator ShowEndBattlePopup(bool playerDied)
        {
            yield return new WaitForSeconds(endBattlePopupDelay);

            var popupOverlay = FindObjectOfType<PopupOverlay>();
            var endBattlePopup = FindObjectOfType<EndBattlePopup>();
            if (popupOverlay != null && endBattlePopup != null)
            {
                popupOverlay.Show();
                endBattlePopup.Show();

                var gameInfo = FindObjectOfType<GameInfo>();

                if (playerDied)
                {
                    endBattlePopup.SetDefeatText();
                    gameInfo.PlayerWonEncounter = false;
                }
                else
                {
                    endBattlePopup.SetVictoryText();
                    gameInfo.PlayerWonEncounter = true;
                }

                var turnManagementSystem = FindObjectOfType<TurnManagementSystem>();
                turnManagementSystem.SetEndOfGame(true);
            }
        }
    }
}

