using System;
using System.Collections.Generic;

namespace VariablesEcho
{
    /// <summary>
    /// 单次跑图的状态（跨战斗、跨场景持久化）。
    /// </summary>
    [Serializable]
    public class EchoRunState
    {
        public int seed;
        public EchoAiFaction opponent = EchoAiFaction.NEXUS;
        public EchoPlayerClass playerClass = EchoPlayerClass.Warden;

        public EchoRoute route = EchoRoute.None;
        public int act = 1;
        public bool finalBattle;

        public int battlesWon;
        public int shardsThisRun;
        public int noiseCardsPlayed;
        public int selfHarmCount;
        public int calibrationRejectedThisRun;

        /// <summary>当前这场战斗中打出的、档案里从未出现过的新卡数量（用于 C 结局判定）。</summary>
        public int newCardsPlayedThisBattle;

        public List<string> cardsPlayedThisRun = new List<string>();
        public List<string> flags = new List<string>();

        public bool HasFlag(string flag)
        {
            return flags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (!flags.Contains(flag))
                flags.Add(flag);
        }

        public void ClearFlag(string flag)
        {
            flags.Remove(flag);
        }
    }
}
