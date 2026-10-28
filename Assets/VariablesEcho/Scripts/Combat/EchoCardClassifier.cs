using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 从 CardTemplate 的效果列表推导出行为标签需要的结构信息。
    /// 如果项目里使用了自定义的 CardType 资产名，也做了一层名称回退。
    /// </summary>
    public static class EchoCardClassifier
    {
        public static PlayedCardRecord Describe(CardTemplate card)
        {
            var record = new PlayedCardRecord();
            if (card == null)
                return record;

            record.CardId = EchoRuntime.StableId(card);
            record.Faction = card.Faction;
            record.NoiseDelta = card.NoiseDelta;
            record.IsSelfHarm = card.IsSelfHarm;

            if (card.Effects != null)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect == null)
                        continue;

                    if (effect is DealDamageEffect)
                        record.IsAttack = true;
                    else if (effect is GainShieldEffect)
                        record.IsDefense = true;
                    else if (effect is DrawCardsEffect)
                        record.IsDraw = true;
                    else if (effect is SelfDamageEffect)
                        record.IsSelfHarm = true;
                }
            }

            if (!record.IsAttack && !record.IsDefense && card.Type != null)
            {
                var typeName = card.Type.name.ToLowerInvariant();
                if (typeName.Contains("attack"))
                    record.IsAttack = true;
                else if (typeName.Contains("skill") || typeName.Contains("defen") || typeName.Contains("guard"))
                    record.IsDefense = true;
            }

            return record;
        }
    }
}
