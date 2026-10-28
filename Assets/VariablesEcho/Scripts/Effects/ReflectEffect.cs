using System;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 给目标叠加反伤层数。受到伤害时按层数对攻击者造成等量伤害，
    /// 反伤直接扣 HP，不经过护甲。
    /// </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "Variables Echo/Effects/Reflect", fileName = "ReflectEffect")]
    public class ReflectEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName()
        {
            return $"Reflect +{Value}";
        }

        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        {
            EchoStatus.AddRaw(target.Status, EchoStatus.Reflect, Value);
        }
    }
}
