using System;
using CCGKit;

namespace VariablesEcho
{
    /// <summary>
    /// 框架对外广播的运行时事件。UI 或其他系统可以订阅这些事件，
    /// 无需修改 CCG Kit 的事件资产。
    /// </summary>
    public static class EchoEvents
    {
        public static event Action<EchoRunState> RunStarted;
        public static event Action<EchoBattleContext> BattleStarted;
        public static event Action<bool> BattleEnded;
        public static event Action<NoiseController> NoiseChanged;
        public static event Action<EchoIntent> IntentResolved;
        public static event Action<EchoBehaviorTag> BehaviorTagChanged;
        public static event Action<CardTemplate, PlayedCardRecord> CardPlayed;
        public static event Action<EchoEndingId> RunEnded;
        public static event Action<RuntimeCharacter, RuntimeCharacter, int> DamageDealt;
        public static event Action<RuntimeCharacter, int> Reflected;

        internal static void RaiseRunStarted(EchoRunState run)
        {
            var handler = RunStarted;
            if (handler != null) handler(run);
        }

        internal static void RaiseBattleStarted(EchoBattleContext battle)
        {
            var handler = BattleStarted;
            if (handler != null) handler(battle);
        }

        internal static void RaiseBattleEnded(bool playerDied)
        {
            var handler = BattleEnded;
            if (handler != null) handler(playerDied);
        }

        internal static void RaiseNoiseChanged(NoiseController noise)
        {
            var handler = NoiseChanged;
            if (handler != null) handler(noise);
        }

        internal static void RaiseIntentResolved(EchoIntent intent)
        {
            var handler = IntentResolved;
            if (handler != null) handler(intent);
        }

        internal static void RaiseBehaviorTagChanged(EchoBehaviorTag tag)
        {
            var handler = BehaviorTagChanged;
            if (handler != null) handler(tag);
        }

        internal static void RaiseCardPlayed(CardTemplate card, PlayedCardRecord record)
        {
            var handler = CardPlayed;
            if (handler != null) handler(card, record);
        }

        internal static void RaiseRunEnded(EchoEndingId ending)
        {
            var handler = RunEnded;
            if (handler != null) handler(ending);
        }

        internal static void RaiseDamageDealt(RuntimeCharacter instigator, RuntimeCharacter target, int damage)
        {
            var handler = DamageDealt;
            if (handler != null) handler(instigator, target, damage);
        }

        internal static void RaiseReflected(RuntimeCharacter target, int damage)
        {
            var handler = Reflected;
            if (handler != null) handler(target, damage);
        }
    }
}
