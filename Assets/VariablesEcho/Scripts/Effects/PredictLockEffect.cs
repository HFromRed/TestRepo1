using System;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 给目标叠加意图锁定层数。锁定层数会让下一次敌人意图展示更多信息，
    /// 并被记录在 EchoIntent.TotalPredictLocks 中。
    /// </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "Variables Echo/Effects/PredictLock", fileName = "PredictLockEffect")]
    public class PredictLockEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName()
        {
            return $"Predict lock +{Value}";
        }

        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        {
            EchoRuntime.AddPredictLocks(target, Value);

            if (EchoRuntime.Config != null && EchoRuntime.Config.applyPredictLockStatus)
                EchoStatus.SetRawAndRaise(target.Status, EchoStatus.PredictLock, Mathf.Max(0, Value));
        }
    }
}
