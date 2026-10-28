using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 框架内部使用的状态名。直接写入 Status.Value 可以避免触发 Kit 的状态栏 UI，
    /// 需要显示时再通过 SetRawAndRaise 走公开事件。
    /// </summary>
    public static class EchoStatus
    {
        public const string Reflect = "Reflect";
        public const string PredictLock = "PredictLock";

        private static readonly Dictionary<string, StatusTemplate> templates = new Dictionary<string, StatusTemplate>();

        public static int Get(StatusVariable status, string name)
        {
            return status == null ? 0 : status.GetValue(name);
        }

        public static void AddRaw(StatusVariable status, string name, int delta)
        {
            if (status == null)
                return;
            status.Value[name] = status.GetValue(name) + delta;
        }

        public static void SetRaw(StatusVariable status, string name, int value)
        {
            if (status == null)
                return;
            status.Value[name] = value;
        }

        public static void SetRawAndRaise(StatusVariable status, string name, int value)
        {
            if (status == null)
                return;
            status.SetValue(GetTemplate(name), value);
        }

        public static StatusTemplate GetTemplate(string name)
        {
            StatusTemplate template;
            if (templates.TryGetValue(name, out template) && template != null)
                return template;

            template = ScriptableObject.CreateInstance<StatusTemplate>();
            template.Name = name;
            template.hideFlags = HideFlags.DontSave;
            templates[name] = template;
            return template;
        }

        /// <summary>会随回合自然递减的临时状态。Poison 由 Kit 的毒结算自行处理。</summary>
        private static readonly string[] Decaying = { "Weak", "Strength", "Fragile" };

        /// <summary>
        /// 回合结束时的状态衰减。Kit 本身没有实现状态递减，
        /// 若不补这一步，Bash 施加的 2 层虚弱会永久生效。
        /// </summary>
        public static void TickDecay(RuntimeCharacter character)
        {
            if (character == null || character.Status == null)
                return;

            var status = character.Status;
            foreach (var name in Decaying)
            {
                var value = status.GetValue(name);
                if (value <= 0 || !status.Template.ContainsKey(name))
                    continue;
                status.SetValue(name, value - 1);
            }
        }
    }
}
