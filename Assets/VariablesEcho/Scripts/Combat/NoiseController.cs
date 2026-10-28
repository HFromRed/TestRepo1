using System;
using System.Collections.Generic;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 玩家噪声状态机。噪声是战斗内资源，战斗开始时重置。
    /// </summary>
    public sealed class NoiseController
    {
        public int Value { get; private set; }
        public bool IsTracked { get; private set; }
        public int HighNoiseStreak { get; private set; }

        public EchoNoiseTier Tier
        {
            get { return Config.TierFor(Value); }
        }

        /// <summary>1.0 = 完全可预测，0.0 = 完全紊乱。</summary>
        public float Confidence
        {
            get { return Mathf.Clamp01(1f - (float)Value / Mathf.Max(1, Config.maxNoise)); }
        }

        public EchoConfig Config { get; private set; }
        public event Action<NoiseController> Changed;

        private readonly List<string> history = new List<string>();
        public IReadOnlyList<string> History
        {
            get { return history; }
        }

        public NoiseController(EchoConfig config, int initialValue = 0)
        {
            Config = config ?? EchoConfig.Instance;
            SetValueInternal(Mathf.Clamp(initialValue, 0, Config.maxNoise));
        }

        public void Add(int delta, string source)
        {
            if (delta == 0)
                return;

            if (IsTracked && delta > 0 && Config.halvePositiveNoiseWhenTracked)
                delta = Mathf.RoundToInt(delta * 0.5f);

            var before = Value;
            SetValueInternal(Value + delta);

            if (Value != before)
            {
                history.Add($"{source}:{(delta > 0 ? "+" : "")}{delta}");
                if (history.Count > 20)
                    history.RemoveAt(0);
                RaiseChanged();
            }
        }

        public void SetValue(int value, string source)
        {
            var before = Value;
            SetValueInternal(value);
            if (Value != before)
            {
                history.Add($"{source}:set({Value})");
                if (history.Count > 20)
                    history.RemoveAt(0);
                RaiseChanged();
            }
        }

        /// <summary>回合结束：先自然衰减，再做追踪判定。</summary>
        public void EndTurn(EchoAiFaction opponent)
        {
            var decay = Config.DecayFor(opponent);
            SetValueInternal(Value - decay);

            var high = Value >= Config.trackedThreshold;
            HighNoiseStreak = high ? HighNoiseStreak + 1 : 0;

            var shouldTrack = high && HighNoiseStreak >= Config.trackedTurnThreshold;
            if (shouldTrack != IsTracked)
            {
                IsTracked = shouldTrack;
                history.Add(IsTracked ? "tracked:on" : "tracked:off");
                if (history.Count > 20)
                    history.RemoveAt(0);
            }

            RaiseChanged();
        }

        public void ResetTracking()
        {
            IsTracked = false;
            HighNoiseStreak = 0;
            RaiseChanged();
        }

        public void Reset()
        {
            Value = 0;
            IsTracked = false;
            HighNoiseStreak = 0;
            history.Clear();
            RaiseChanged();
        }

        private void SetValueInternal(int value)
        {
            Value = Mathf.Clamp(value, 0, Config.maxNoise);
        }

        private void RaiseChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler(this);
            EchoEvents.RaiseNoiseChanged(this);
        }
    }
}
