// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// This system is responsible for managing the game's turn sequence, which always follows
    /// the order:
    ///     - Player turn.
    ///     - Enemies turn.
    /// It creates event-like entities with the Event component attached to them when the turn
    /// sequence advances (beginning of player turn, end of player turn, beginning of enemies
    /// turn, end of enemies turn).
    /// </summary>
    public class TurnManagementSystem : MonoBehaviour
    {
        public GameEvent PlayerTurnBegan;
        public GameEvent PlayerTurnEnded;
        public GameEvent EnemyTurnBegan;
        public GameEvent EnemyTurnEnded;

        private bool isEnemyTurn;
        private float accTime;

        private bool isEndOfGame;

        private const float EnemyTurnDuration = 3.0f;

        protected void Update()
        {
            if (isEndOfGame) return;
            if (isEnemyTurn)
            {
                accTime += Time.deltaTime;
                if (accTime >= EnemyTurnDuration)
                {
                    accTime = 0.0f;
                    EndEnemyTurn();
                    BeginPlayerTurn();
                }
            }
        }

        public void BeginGame()
        {
            BeginPlayerTurn();
        }

        public void BeginPlayerTurn()
        {
            if (isEndOfGame) return;
            EchoRuntime.OnPlayerTurnBegan();
            PlayerTurnBegan.Raise();

            
        }

        public void EndPlayerTurn()
        {
            if (isEndOfGame) return;
            PlayerTurnEnded.Raise();

            // === Variables Echo integration ===
            // 在 Kit 结算完回合结束事件之后、敌人行动之前，执行噪声衰减/追踪判定。
            EchoRuntime.OnPlayerTurnEnded();
            // === End Variables Echo integration ===

            BeginEnemyTurn();
        }

        public void BeginEnemyTurn()
        {
            if (isEndOfGame) return;
            EnemyTurnBegan.Raise();

            // === Variables Echo integration ===
            EchoRuntime.OnEnemyTurnBegan();
            // === End Variables Echo integration ===

            isEnemyTurn = true;
        }

        public void EndEnemyTurn()
        {
            if (isEndOfGame) return;
            EnemyTurnEnded.Raise();

            // === Variables Echo integration ===
            EchoRuntime.OnEnemyTurnEnded();
            // === End Variables Echo integration ===

            isEnemyTurn = false;
        }

        public void SetEndOfGame(bool value)
        {
            isEndOfGame = value;
        }

        public bool IsEndOfGame()
        {
            return isEndOfGame;
        }
    }
}




