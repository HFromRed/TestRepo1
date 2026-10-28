using System;
using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    [Serializable]
    public class EchoCounterEntry
    {
        public EchoBehaviorTag tag = EchoBehaviorTag.None;
        public string label = "Counter";
        public Sprite icon;
        public List<Effect> effects = new List<Effect>();
        public int extraPredictLocks;
    }

    /// <summary>
    /// 每个敌人可选的“行为标签克制表”。挂到 EnemyTemplate.EchoProfile 上使用。
    /// 没有配置 Profile 时，敌人仍按 Kit 原有的 Pattern 循环行动。
    /// </summary>
    [CreateAssetMenu(menuName = "Variables Echo/Enemy Profile", fileName = "EnemyProfile")]
    public class EchoEnemyProfile : ScriptableObject
    {
        public EchoAiFaction faction = EchoAiFaction.NEXUS;

        public List<EchoCounterEntry> counters = new List<EchoCounterEntry>();

        [Tooltip("崩溃档时可选的随机 Pattern 池；留空则使用模板自身的 Patterns。")]
        public List<Pattern> collapsePool = new List<Pattern>();

        public EchoCounterEntry FindCounter(EchoBehaviorTag tag)
        {
            if (counters == null)
                return null;

            foreach (var entry in counters)
            {
                if (entry == null || entry.tag != tag)
                    continue;
                if (entry.effects == null || entry.effects.Count == 0)
                    continue;
                return entry;
            }

            return null;
        }
    }
}
