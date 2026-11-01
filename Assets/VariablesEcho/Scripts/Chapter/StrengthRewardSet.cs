using System.Linq;
using CCGKit;
using UnityEngine;
namespace VariablesEcho
{
    // Stable IDs allow rewards to survive chapter changes and save/load.
    public static class StrengthRewardSet
    {
        public static readonly int[] Ids={7300,7301,7302,7303,7304,7305,7306,7307,7312,7319};
        static readonly string[] Names={"义体校准","血热电池","废铁重拳","双刃协议","三联点射","破甲凿击","应急装甲","拆机取电","液压重锤","人类余火"};
        static readonly int[] Cost={1,0,1,1,1,2,1,0,2,2};
        static readonly int[] Damage={0,0,8,4,2,14,0,0,22,0};
        static readonly int[] Hits={1,1,1,2,3,1,1,1,1,1};
        static readonly int[] Amp={2,2,0,0,0,0,1,1,0,6};
        static readonly int[] Shield={0,0,0,0,0,0,6,0,0,8};
        static readonly int[] Blood={0,3,0,0,0,0,0,2,0,4};
        public static bool IsRemoved(int id) { return id>=7300 && id<7320 && !Ids.Contains(id); }
        public static int MigrateId(int id)
        {
            switch(id){case 7308:case 7317:return 7306;case 7309:case 7311:return 7307;
                case 7310:case 7315:case 7316:return 7300;case 7313:case 7314:return 7304;case 7318:return 7312;default:return id;}
        }
        public static void MigrateSave(ChapterSave save)
        {
            save.deck=save.deck.Select(MigrateId).ToList();
            if(save.reward!=null)save.reward=save.reward.Select(MigrateId).Distinct().ToList();
        }
        public static void Install(EchoChapterData data)
        {
            // Apply the same current artwork to rewards, upgrades and battle templates.
            foreach(var existing in data.cards)
            {
                if(existing==null)continue;
                int id=existing.Id>=1201 && existing.Id<=1212?existing.Id-1000:existing.Id;
                string path=null;
                switch(id)
                {
                    case 201:path="StarterCards/strike";break;
                    case 202:path="StarterCards/defend";break;
                    case 203:path="StarterCards/bash";break;
                    case 204:path="StrengthCards/card-00";break;
                    case 205:path="StrengthCards/card-01";break;
                    case 206:case 210:path="StrengthCards/card-06";break;
                    case 207:path="StrengthCards/card-04";break;
                    case 208:path="StrengthCards/card-03";break;
                    case 209:path="StrengthCards/card-07";break;
                    case 211:path="StrengthCards/card-02";break;
                    case 212:path="StrengthCards/card-08";break;
                }
                if(path!=null) {var art=Resources.Load<Sprite>(path);if(art!=null)existing.Picture=art;}
            }
            data.cards.RemoveAll(c=>c!=null && IsRemoved(c.Id));
            data.rewards.RemoveAll(c=>c!=null && IsRemoved(c.Id));
            var source=data.cards.FirstOrDefault(c=>c!=null && c.Picture!=null);
            if(source==null)return;
            for(int i=0;i<Names.Length;i++)
            {
                var card=data.FindCard(Ids[i]);
                if(card==null)
                {
                    card=Object.Instantiate(source);card.Id=Ids[i];card.CardId="strength-"+(Ids[i]);card.Name=Names[i];card.Cost=Cost[i];
                    card.Upgrade=null;card.Effects=new System.Collections.Generic.List<Effect>();card.NoiseDelta=0;card.IsPermanent=false;card.IsSelfHarm=Blood[i]>0;
                    if(Blood[i]>0)Add<SelfDamageEffect>(card,Blood[i],EffectTargetType.Self);
                    if(Amp[i]>0)Add<AmplifyEffect>(card,Amp[i],EffectTargetType.Self);
                    for(int h=0;h<Hits[i] && Damage[i]>0;h++)Add<DealDamageEffect>(card,Damage[i],EffectTargetType.TargetEnemy);
                    if(Shield[i]>0)Add<GainShieldEffect>(card,Shield[i],EffectTargetType.Self);
                    if(i==7){var draw=ScriptableObject.CreateInstance<DrawCardsEffect>();draw.Amount=1;card.Effects.Add(draw);}
                    data.cards.Add(card);
                }
                var art=Resources.Load<Sprite>("StrengthCards/card-"+i.ToString("00"));
                if(art!=null)card.Picture=art;
                card.Rarity=i==9?EchoCardRarity.Rare:i==5 || i==8?EchoCardRarity.Uncommon:EchoCardRarity.Common;
                if(!data.rewards.Contains(card))data.rewards.Add(card);
            }
        }
        static void Add<T>(CardTemplate c,int amount,EffectTargetType target) where T:IntegerEffect
        {var e=ScriptableObject.CreateInstance<T>();e.Value=amount;e.Target=target;c.Effects.Add(e);}
    }
}
