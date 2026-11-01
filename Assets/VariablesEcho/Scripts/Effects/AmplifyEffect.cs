using CCGKit;
using UnityEngine;
namespace VariablesEcho
{
    public class AmplifyEffect : IntegerEffect, IEntityEffect
    {
        public override string GetName() { return "获得 " + Value + " 点增幅\n本场战斗每段攻击额外造成等量伤害。"; }
        public override void Resolve(RuntimeCharacter instigator, RuntimeCharacter target)
        { EchoStatus.SetRawAndRaise(target.Status, "Amplify", target.Status.GetValue("Amplify") + Value); }
    }
}
