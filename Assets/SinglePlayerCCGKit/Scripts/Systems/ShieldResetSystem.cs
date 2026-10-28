// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;
using UnityEngine;

namespace CCGKit
{
    /// <summary>
    /// This system is responsible for resetting the hero's shield when their turn starts.
    /// </summary>
    public class ShieldResetSystem : BaseSystem
    {
#pragma warning disable 649
        [SerializeField]
        private PlayableCharacterConfiguration playerConfig;
        [SerializeField]
        private List<NonPlayableCharacterConfiguration> enemyConfigs;
#pragma warning restore 649

        public void OnPlayerTurnBegan()
        {
            playerConfig.Shield.SetValue(0);
        }

        public void OnEnemyTurnBegan()
        {
            foreach (var config in enemyConfigs)
            {
                config.Shield.SetValue(0);
            }
        }
    }
}
