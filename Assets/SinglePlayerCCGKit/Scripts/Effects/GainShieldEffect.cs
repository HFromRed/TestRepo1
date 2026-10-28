// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// The type corresponding to the "Gain X shield" card effect.
    /// </summary>
    [Serializable]
    public class GainShieldEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName()
        {
            return $"Gain {Value.ToString()} Shield";
        }

        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        {
            var targetShield = target.Shield;
            targetShield.SetValue(targetShield.Value + Value);

            // === Variables Echo integration ===
            // 防守即降噪：玩家每获得 5 点护盾，噪声 -2。
            EchoRuntime.OnShieldGained(target, Value);
            // === End Variables Echo integration ===
        }
    }
}
