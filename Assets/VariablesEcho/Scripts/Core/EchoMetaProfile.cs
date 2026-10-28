using System;
using System.Collections.Generic;

namespace VariablesEcho
{
    /// <summary>
    /// 跨跑图持久化的元进度档案（对应设计文档 3.7）。
    /// 使用 JsonUtility 序列化，因此只使用 public 字段。
    /// </summary>
    [Serializable]
    public class EchoMetaProfile
    {
        public int totalRuns;
        public int totalVictories;

        public int noiseCardsPlayed;
        public int selfHarmCount;
        public int calibrationRejected;

        public float resonancePollution = 12f;

        public string lastEndingId = "";
        public string lastRoute = "";

        public List<string> uniqueCardsPlayed = new List<string>();
        public List<string> unlockedEndings = new List<string>();
        public List<string> memoryFragments = new List<string>();
        public List<string> permanentCardsAcquired = new List<string>();

        public bool HasUnlocked(EchoEndingId ending)
        {
            return unlockedEndings.Contains(ending.ToString());
        }

        public bool HasPlayedCard(string cardId)
        {
            return uniqueCardsPlayed.Contains(cardId);
        }
    }
}
