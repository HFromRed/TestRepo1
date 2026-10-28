using System.Collections.Generic;

namespace VariablesEcho
{
    public sealed class PlayedCardRecord
    {
        public string CardId;
        public EchoCardFaction Faction;
        public int NoiseDelta;
        public bool IsAttack;
        public bool IsDefense;
        public bool IsDraw;
        public bool IsSelfHarm;

        public bool IsNoiseCard
        {
            get { return Faction == EchoCardFaction.Noise || NoiseDelta > 0; }
        }
    }

    /// <summary>
    /// 记录最近 3 回合的出牌结构，并推导玩家的行为标签。
    /// </summary>
    public sealed class BehaviorTagger
    {
        public const int WindowSize = 3;

        private readonly List<List<PlayedCardRecord>> turns = new List<List<PlayedCardRecord>>();
        private List<PlayedCardRecord> current = new List<PlayedCardRecord>();

        public int WindowTurnCount
        {
            get { return turns.Count; }
        }

        public int CurrentTurnPlays
        {
            get { return current.Count; }
        }

        public void BeginPlayerTurn()
        {
            current = new List<PlayedCardRecord>();
        }

        public void Record(PlayedCardRecord record)
        {
            if (record != null)
                current.Add(record);
        }

        public void EndPlayerTurn()
        {
            turns.Add(current);
            current = new List<PlayedCardRecord>();
            while (turns.Count > WindowSize)
                turns.RemoveAt(0);
        }

        public EchoBehaviorTag Evaluate()
        {
            var total = 0;
            var attack = 0;
            var defense = 0;
            var draw = 0;
            var selfHarm = false;

            foreach (var turn in turns)
            {
                foreach (var record in turn)
                {
                    total++;
                    if (record.IsAttack) attack++;
                    if (record.IsDefense) defense++;
                    if (record.IsDraw) draw++;
                    if (record.IsSelfHarm) selfHarm = true;
                }
            }

            if (total == 0)
            {
                return turns.Count > 0 ? EchoBehaviorTag.Idle : EchoBehaviorTag.None;
            }

            if (selfHarm)
                return EchoBehaviorTag.SelfHarm;

            var threshold = total * 0.6f;
            if (defense > 0 && defense >= threshold)
                return EchoBehaviorTag.Wall;

            if (attack > 0 && attack >= threshold)
                return EchoBehaviorTag.AllIn;

            if (draw >= 2)
                return EchoBehaviorTag.Draw;

            if (total <= 1)
                return EchoBehaviorTag.Idle;

            return EchoBehaviorTag.None;
        }
    }
}
