using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 噪声驱动的敌方意图决策器。
    /// 由 EnemyBrainSystem 在玩家回合开始时调用；如果没有配置 EchoEnemyProfile，
    /// 仍然会按原 Kit 的 Pattern 循环行动，只是附带噪声档位与锁定信息。
    /// </summary>
    public sealed class EchoIntentProvider
    {
        private sealed class NormalChoice
        {
            public List<Effect> effects;
            public Sprite icon;
            public string label;
        }

        private readonly EchoBattleContext battle;
        private static Sprite unknownIcon;

        public EchoIntentProvider(EchoBattleContext battle)
        {
            this.battle = battle;
        }

        public bool TryBuildIntent(
            CharacterObject enemy,
            int index,
            EnemyBrain brain,
            EnemyTemplate template,
            out Sprite icon,
            out int displayValue)
        {
            icon = null;
            displayValue = 0;

            if (battle == null || enemy == null || brain == null || template == null)
                return false;

            var config = EchoRuntime.Config ?? EchoConfig.Instance;
            var noise = battle.Noise;
            var tag = battle.Tagger.Evaluate();
            var tier = noise.Tier;
            var profile = template.EchoProfile;

            var normal = SelectNormal(template, brain, tier, profile);
            var counter = profile != null ? profile.FindCounter(tag) : null;

            var chooseCounter = false;
            if (counter != null)
            {
                if (noise.IsTracked)
                {
                    chooseCounter = true;
                }
                else if (tier == EchoNoiseTier.Stable)
                {
                    chooseCounter = true;
                }
                else if (tier == EchoNoiseTier.Drift)
                {
                    var chance = config.useConfidenceForDrift ? noise.Confidence : 0.5f;
                    chooseCounter = EchoRuntime.Rng != null &&
                                    EchoRuntime.Rng.Roll(EchoRuntime.Rng.Combat, chance);
                }
                else
                {
                    // 崩溃档：玩家无法预测，敌人也不会展示“被读懂了”的克制。
                    chooseCounter = false;
                }
            }

            List<Effect> chosenEffects;
            Sprite chosenIcon;
            string chosenLabel;
            var isCounter = false;
            var extraLocks = 0;

            if (chooseCounter)
            {
                chosenEffects = counter.effects;
                chosenIcon = counter.icon;
                chosenLabel = string.IsNullOrEmpty(counter.label) ? "Counter" : counter.label;
                extraLocks = counter.extraPredictLocks;
                isCounter = true;
            }
            else if (normal != null)
            {
                chosenEffects = normal.effects;
                chosenIcon = normal.icon;
                chosenLabel = normal.label;
            }
            else
            {
                return false;
            }

            var intent = new EchoIntent
            {
                EnemyIndex = index,
                Tier = tier,
                IsTracked = noise.IsTracked,
                IsCounter = isCounter,
                PredictLocks = noise.IsTracked
                    ? config.maxPredictLocks
                    : Mathf.FloorToInt(noise.Confidence * config.maxPredictLocks),
                BonusPredictLocks = extraLocks +
                    (enemy.Character != null ? battle.GetBonusPredictLocks(enemy.Character) : 0),
                Effects = new List<Effect>(chosenEffects ?? new List<Effect>()),
                Icon = chosenIcon
            };

            int value;
            intent.Kind = ClassifyKind(intent.Effects, out value);
            intent.Value = value;
            intent.IsUnknown = tier == EchoNoiseTier.Collapse;

            if (tier == EchoNoiseTier.Drift)
            {
                if (chooseCounter && normal != null)
                    intent.Candidates.Add(DescribeEffects(normal.effects, false));
                else if (!chooseCounter && counter != null)
                    intent.Candidates.Add(DescribeEffects(counter.effects, true));
            }



            if (index >= 0 && index < battle.Intents.Length)
                battle.Intents[index] = intent;

            if (config.applyPredictLockStatus && enemy.Character != null)
                EchoStatus.SetRawAndRaise(enemy.Character.Status, EchoStatus.PredictLock, intent.TotalPredictLocks);

            brain.Effects = intent.Effects;

            icon = chosenIcon;
            displayValue = intent.Value;

            EchoEvents.RaiseIntentResolved(intent);
            return true;
        }

        private NormalChoice SelectNormal(
            EnemyTemplate template,
            EnemyBrain brain,
            EchoNoiseTier tier,
            EchoEnemyProfile profile)
        {
            if (tier == EchoNoiseTier.Collapse && profile != null && profile.collapsePool != null)
            {
                var pool = new List<Pattern>();
                foreach (var pooled in profile.collapsePool)
                {
                    if (pooled != null)
                        pool.Add(pooled);
                }

                if (pool.Count > 0)
                {
                    var idx = EchoRuntime.Rng != null
                        ? EchoRuntime.Rng.Combat.Next(0, pool.Count)
                        : UnityEngine.Random.Range(0, pool.Count);
                    // 崩溃档的随机池与模板自身的 Pattern 进度互不影响。
                    return ChoiceFromPattern(pool[idx], brain, false);
                }
            }

            if (template == null || template.Patterns == null || template.Patterns.Count == 0)
                return null;

            if (brain.Pattern < 0 || brain.Pattern >= template.Patterns.Count)
                brain.Pattern = 0;

            var pattern = template.Patterns[brain.Pattern];
            if (pattern == null)
            {
                brain.Pattern++;
                return null;
            }

            return ChoiceFromPattern(pattern, brain, true);
        }

        private NormalChoice ChoiceFromPattern(Pattern pattern, EnemyBrain brain, bool advance)
        {
            if (pattern == null)
                return null;

            if (pattern is RepeatPattern repeatPattern)
            {
                if (advance)
                {
                    brain.Count++;
                    if (brain.Count >= Mathf.Max(1, repeatPattern.Times))
                    {
                        brain.Count = 0;
                        brain.Pattern++;
                    }
                }

                return new NormalChoice
                {
                    effects = repeatPattern.Effects,
                    icon = repeatPattern.Sprite,
                    label = "Pattern"
                };
            }

            if (pattern is RepeatForeverPattern repeatForeverPattern)
            {
                return new NormalChoice
                {
                    effects = repeatForeverPattern.Effects,
                    icon = repeatForeverPattern.Sprite,
                    label = "Pattern"
                };
            }

            if (pattern is RandomPattern randomPattern)
            {
                if (advance)
                    brain.Pattern++;
                return PickRandom(randomPattern);
            }

            if (advance)
                brain.Pattern++;
            return new NormalChoice
            {
                effects = pattern.Effects,
                icon = null,
                label = "Pattern"
            };
        }

        private NormalChoice PickRandom(RandomPattern pattern)
        {
            var totalWeight = 0;
            if (pattern.Probabilities != null)
            {
                foreach (var probability in pattern.Probabilities)
                {
                    if (probability != null)
                        totalWeight += Mathf.Max(0, probability.Value);
                }
            }

            if (totalWeight <= 0 || pattern.Effects == null || pattern.Effects.Count == 0)
            {
                var fallback = new List<Effect>();
                if (pattern.Effects != null && pattern.Effects.Count > 0 && pattern.Effects[0] != null)
                    fallback.Add(pattern.Effects[0]);
                return new NormalChoice { effects = fallback, icon = null, label = "Pattern" };
            }

            var roll = EchoRuntime.Rng != null
                ? EchoRuntime.Rng.Combat.Next(0, totalWeight)
                : UnityEngine.Random.Range(0, totalWeight);

            var accumulated = 0;
            var chosenIndex = 0;
            for (var i = 0; i < pattern.Probabilities.Count; i++)
            {
                var weight = Mathf.Max(0, pattern.Probabilities[i].Value);
                accumulated += weight;
                if (roll < accumulated)
                {
                    chosenIndex = i;
                    break;
                }
            }

            var effects = new List<Effect>();
            if (chosenIndex < pattern.Effects.Count && pattern.Effects[chosenIndex] != null)
                effects.Add(pattern.Effects[chosenIndex]);

            var icon = chosenIndex < pattern.Probabilities.Count
                ? pattern.Probabilities[chosenIndex].Sprite
                : null;

            return new NormalChoice { effects = effects, icon = icon, label = "Pattern" };
        }

        private static EchoIntentKind ClassifyKind(List<Effect> effects, out int value)
        {
            value = 0;
            if (effects == null || effects.Count == 0)
                return EchoIntentKind.Unknown;

            var first = effects[0];
            var integerEffect = first as IntegerEffect;
            if (integerEffect != null)
                value = integerEffect.Value;

            if (first is DealDamageEffect)
                return EchoIntentKind.Attack;
            if (first is GainShieldEffect)
                return EchoIntentKind.Block;

            return EchoIntentKind.Status;
        }

        private static string DescribeEffects(List<Effect> effects, bool counter)
        {
            int value;
            var kind = ClassifyKind(effects, out value);
            var kindText = kind == EchoIntentKind.Attack ? "ATK" : kind == EchoIntentKind.Block ? "BLK" : "EFX";
            return $"{(counter ? "CTR" : "ALT")} {kindText} {value}";
        }

        public static Sprite UnknownIcon
        {
            get
            {
                if (unknownIcon == null)
                    unknownIcon = CreateUnknownIcon();
                return unknownIcon;
            }
        }

        private static Sprite CreateUnknownIcon()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Echo_UnknownIntent";
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Point;

            var dark = new Color(0.18f, 0.02f, 0.22f, 1f);
            var bright = new Color(0.95f, 0.2f, 0.85f, 1f);

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var checker = ((x / 8) + (y / 8)) % 2 == 0;
                    var diagonal = Mathf.Abs(x - y) < 2 || Mathf.Abs(x - (size - 1 - y)) < 2;
                    pixels[y * size + x] = diagonal || checker ? bright : dark;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.name = "Echo_UnknownIntent";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}

