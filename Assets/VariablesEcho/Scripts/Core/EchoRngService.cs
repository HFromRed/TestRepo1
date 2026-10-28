using System.Collections.Generic;

namespace VariablesEcho
{
    /// <summary>
    /// 分层随机服务：战斗 / 奖励 / 元进度三个互不干扰的通道。
    /// 同一个 run seed 会得到同一组随机序列，便于复现与 debug。
    /// </summary>
    public sealed class EchoRngService
    {
        public readonly System.Random Combat;
        public readonly System.Random Reward;
        public readonly System.Random Meta;

        public EchoRngService(int seed)
        {
            Combat = new System.Random(DeriveSeed(seed, "combat"));
            Reward = new System.Random(DeriveSeed(seed, "reward"));
            Meta = new System.Random(DeriveSeed(seed, "meta"));
        }

        public static int DeriveSeed(int seed, string channel)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = (hash ^ (uint)seed) * 16777619u;
                if (channel != null)
                {
                    for (var i = 0; i < channel.Length; i++)
                        hash = (hash ^ channel[i]) * 16777619u;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }

        public bool Roll(System.Random rng, float chance)
        {
            if (rng == null)
                return false;
            return rng.NextDouble() < chance;
        }

        public T Pick<T>(IReadOnlyList<T> list, System.Random rng) where T : class
        {
            if (list == null || list.Count == 0)
                return null;
            if (rng == null)
                return list[0];
            return list[rng.Next(0, list.Count)];
        }

        public void Shuffle<T>(List<T> list, System.Random rng)
        {
            if (list == null || list.Count < 2)
                return;
            if (rng == null)
                return;

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
