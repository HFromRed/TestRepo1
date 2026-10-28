using System.Collections.Generic;
using CCGKit;

namespace VariablesEcho
{
    /// <summary>
    /// 单场战斗的运行时上下文。战斗开始时创建，战斗结束或场景卸载时丢弃。
    /// </summary>
    public sealed class EchoBattleContext
    {
        public readonly CharacterObject Player;
        public readonly List<CharacterObject> Enemies;
        public readonly NoiseController Noise;
        public readonly BehaviorTagger Tagger;
        public readonly EchoIntentProvider IntentProvider;
        public readonly EchoIntent[] Intents;

        /// <summary>本场战斗的敌方阵营，决定噪声自然衰减速度。优先读取敌人的 EchoProfile。</summary>
        public readonly EchoAiFaction Opponent;

        private readonly Dictionary<RuntimeCharacter, int> bonusPredictLocks = new Dictionary<RuntimeCharacter, int>();

        public RuntimeCharacter PlayerRuntime
        {
            get { return Player != null ? Player.Character : null; }
        }

        public EchoBattleContext(
            CharacterObject player,
            List<CharacterObject> enemies,
            NoiseController noise,
            BehaviorTagger tagger,
            EchoAiFaction fallbackOpponent)
        {
            Player = player;
            Enemies = enemies ?? new List<CharacterObject>();
            Noise = noise;
            Tagger = tagger;
            Intents = new EchoIntent[Enemies.Count];
            IntentProvider = new EchoIntentProvider(this);
            Opponent = ResolveOpponent(Enemies, fallbackOpponent);
        }

        private static EchoAiFaction ResolveOpponent(List<CharacterObject> enemies, EchoAiFaction fallback)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;
                var template = enemy.Template as EnemyTemplate;
                if (template != null && template.EchoProfile != null)
                    return template.EchoProfile.faction;
            }

            return fallback;
        }

        public bool TryGetCharacter(RuntimeCharacter runtime, out CharacterObject character)
        {
            character = null;
            if (runtime == null)
                return false;

            if (Player != null && Player.Character == runtime)
            {
                character = Player;
                return true;
            }

            foreach (var enemy in Enemies)
            {
                if (enemy != null && enemy.Character == runtime)
                {
                    character = enemy;
                    return true;
                }
            }

            return false;
        }

        public bool IsPlayer(RuntimeCharacter runtime)
        {
            return runtime != null && Player != null && Player.Character == runtime;
        }

        public void AddPredictLocks(RuntimeCharacter target, int amount)
        {
            if (target == null || amount == 0)
                return;

            bonusPredictLocks[target] = GetBonusPredictLocks(target) + amount;
        }

        public int GetBonusPredictLocks(RuntimeCharacter target)
        {
            if (target == null)
                return 0;

            int value;
            return bonusPredictLocks.TryGetValue(target, out value) ? value : 0;
        }
    }
}
