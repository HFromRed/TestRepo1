using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 一个敌人的“已决定但未执行”的意图。
    /// </summary>
    public sealed class EchoIntent
    {
        public int EnemyIndex;
        public EchoIntentKind Kind;
        public int Value;
        public EchoNoiseTier Tier;

        public int PredictLocks;
        public int BonusPredictLocks;
        public bool IsTracked;
        public bool IsCounter;
        public bool IsUnknown;

        public Sprite Icon;
        public List<Effect> Effects = new List<Effect>();
        public List<string> Candidates = new List<string>();

        public int TotalPredictLocks
        {
            get { return PredictLocks + BonusPredictLocks; }
        }

        public string Describe()
        {
            if (IsUnknown)
                return $"E{EnemyIndex + 1} ??? [C]";

            var tier = Tier == EchoNoiseTier.Stable ? "A" : Tier == EchoNoiseTier.Drift ? "B" : "C";
            var kind = Kind == EchoIntentKind.Attack ? "ATK" : Kind == EchoIntentKind.Block ? "BLK" : "EFX";

            var text = $"E{EnemyIndex + 1} {kind} {Value} [{tier}]";
            if (TotalPredictLocks > 0)
                text += $" LOCK{TotalPredictLocks}";
            if (IsCounter)
                text += " CTR";
            if (Candidates.Count > 0)
                text += " | " + string.Join(" / ", Candidates);

            return text;
        }
    }
}
