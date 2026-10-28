using System;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 自伤：直接扣除目标 HP（不经过护甲，也不触发反伤）。
    /// 带有此效果的牌会被行为标签识别为 SelfHarm。
    /// </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "Variables Echo/Effects/Self Damage", fileName = "SelfDamageEffect")]
    public class SelfDamageEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName()
        {
            return $"Lose {Value} HP";
        }

        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        {
            if (target == null || target.Hp == null)
                return;

            target.Hp.SetValue(Mathf.Max(0, target.Hp.Value - Value));
        }
    }
}
