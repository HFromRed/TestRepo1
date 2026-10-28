using System;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 改变玩家噪声值。Value 可为负数（逻辑牌降噪）或正数（噪声牌/失控牌）。
    /// 噪声是玩家资源，因此 Target 字段不会改变作用对象。
    /// </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "Variables Echo/Effects/Noise", fileName = "NoiseEffect")]
    public class NoiseEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName()
        {
            return $"Noise {(Value >= 0 ? "+" : "")}{Value}";
        }

        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        {
            EchoRuntime.AddNoise(Value);
        }
    }
}
