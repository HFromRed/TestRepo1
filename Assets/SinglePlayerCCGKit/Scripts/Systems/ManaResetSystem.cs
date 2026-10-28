// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// This system is responsible for resetting the hero's mana when their turn starts.
    /// </summary>
    public class ManaResetSystem : BaseSystem
    {
#pragma warning disable 649
        [SerializeField]
        private PlayableCharacterConfiguration playerConfig;
#pragma warning restore 649

        // Kit 原本写死的每回合法力上限。
        private const int BaseMana = 3;

        public void OnPlayerTurnBegan()
        {
            // === Variables Echo integration ===
            // 崩溃档噪声会让法力上限上下浮动 1 点；稳定/漂移档仍是 3。
            playerConfig.Mana.SetValue(EchoDistortion.RollMana(BaseMana));
            // === End Variables Echo integration ===
        }
    }
}
