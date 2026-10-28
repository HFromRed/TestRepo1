using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 统一的伤害结算管线。DealDamageEffect 会优先调用这里，
    /// 以保证噪声加成、反伤和 Kit 原有护甲逻辑只在一个地方实现。
    /// </summary>
    public static class EchoCombatHooks
    {
        public static bool TryResolveDamage(RuntimeCharacter instigator, RuntimeCharacter target, int rawDamage)
        {
            if (EchoRuntime.Battle == null || instigator == null || target == null)
                return false;
            if (target.Hp == null || target.Shield == null || instigator.Hp == null)
                return false;

            var damage = rawDamage;
            if (instigator.Status != null)
                damage += instigator.Status.GetValue("Strength");
            else
                damage += 0;

            if (instigator.Status != null && instigator.Status.GetValue("Weak") > 0)
                damage = Mathf.FloorToInt(damage * 0.75f);

            if (EchoRuntime.Battle.PlayerRuntime == instigator)
            {
                var config = EchoRuntime.Config ?? EchoConfig.Instance;
                var steps = EchoRuntime.Battle.Noise.Value / 10;
                var multiplier = 1f + steps * config.damageBonusPerTenNoise;
                damage = Mathf.FloorToInt(damage * multiplier);
            }

            if (damage < 0)
                damage = 0;

            var hp = target.Hp.Value;
            var shield = target.Shield.Value;

            if (damage >= shield)
            {
                var damageToHp = damage - shield;
                target.Shield.SetValue(0);
                if (damageToHp > hp)
                    damageToHp = hp;
                target.Hp.SetValue(hp - damageToHp);
            }
            else
            {
                target.Shield.SetValue(shield - damage);
            }

            var reflect = target.Status != null ? target.Status.GetValue(EchoStatus.Reflect) : 0;
            if (reflect > 0 && instigator != target && damage > 0)
            {
                var reflectDamage = Mathf.Min(reflect, damage);
                var instigatorHp = instigator.Hp.Value;
                if (reflectDamage > instigatorHp)
                    reflectDamage = instigatorHp;

                instigator.Hp.SetValue(instigatorHp - reflectDamage);
                EchoEvents.RaiseReflected(instigator, reflectDamage);
            }

            EchoEvents.RaiseDamageDealt(instigator, target, damage);
            return true;
        }
    }
}
