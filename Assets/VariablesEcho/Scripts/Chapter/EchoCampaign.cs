using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VariablesEcho
{
    // Runtime chapter instances preserve the authored first chapter and share its card/combat assets.
    public static class EchoCampaign
    {
        public static string Title(int act) { return act==1?"第一章  镜像中继站":act==2?"第二章  失名档案馆":"第三章  无主的明天"; }
        public static EchoChapterData Create(EchoChapterData source,int act)
        {
            if(act<1 || act>3)throw new ArgumentOutOfRangeException(nameof(act));
            var d=UnityEngine.Object.Instantiate(source);
            d.chapterTitle=Title(act);
            if(act==1)
            {
                // Work on copies: never rewrite designer assets or legacy node identifiers.
                d.nodes=source.nodes.Select(n=>JsonUtility.FromJson<ChapterNode>(JsonUtility.ToJson(n))).ToList();
                for(int i=0;i<d.nodes.Count;i++)
                {
                    var n=d.nodes[i]; var original=source.nodes[i];
                    n.enemy=original.enemy;n.region=EchoArtRegion.Ivory;n.background=EchoChapterPresentation.Background(EchoArtRegion.Ivory);
                    for(int j=0;j<n.choices.Count;j++)n.choices[j].card=original.choices[j].card;
                    if(string.IsNullOrEmpty(n.title))n.title="信号哨站";
                        n.story=n.kind==ChapterNodeKind.Event?"你查到人类接受校准后会被重新派往战区。":
                            n.kind==ChapterNodeKind.Rest?"你准备修复身体或升级卡牌。":
                            n.kind==ChapterNodeKind.Shop?"你准备购买补给。":"AI 要求你清除守卫，关闭中继站。";
                    foreach(var choice in n.choices) choice.consequence="你保存了调查记录，继续前进。";
                    if(n.depth==d.maxDepth){n.kind=ChapterNodeKind.Boss;n.story="中继站守卫阻止你关闭信号。";}

                }
                return d;
            }
            var fight=source.nodes.First(n=>n.enemy!=null && n.kind!=ChapterNodeKind.Boss).enemy;
            var boss=source.nodes.Last(n=>n.enemy!=null).enemy;
            var region=act==2?EchoArtRegion.Wasteland:EchoArtRegion.Interference;
            Sprite bg=EchoChapterPresentation.Background(region);
            d.nodes=new List<ChapterNode>();d.maxDepth=4;
            ChapterNode Add(string id,int depth,float lane,string title,ChapterNodeKind kind,string story)
            {
                var n=new ChapterNode{id="act"+act+"-"+id,depth=depth,mapPosition=new Vector2(depth*125,lane),title=title,kind=kind,story=story,region=region,background=bg,
                    enemy=kind==ChapterNodeKind.Battle?fight:kind==ChapterNodeKind.Boss?boss:null};
                d.nodes.Add(n);return n;
            }
            if(act==2)
            {
                Add("gate",0,160,"档案封锁线",ChapterNodeKind.Battle,"你进入档案馆，查找自己的部署记录。");
                var witness=Add("witness",1,85,"部署记录",ChapterNodeKind.Event,"记录显示，你多次接受修复并返回战区。");
                witness.choices.Add(new ChapterChoice{label="核对部署记录 / 数据 -10，记忆 +1",credits=-10,memory=1,consequence="你保存了调查记录，继续前进。"});
                witness.choices.Add(new ChapterChoice{label="接受治疗 / 回复 10 生命",heal=10,consequence="你保存了调查记录，继续前进。"});
                Add("market",1,235,"流亡者交换所",ChapterNodeKind.Shop,"你可以购买补给，再继续调查。");
                var archive=Add("archive",2,160,"记忆删除记录",ChapterNodeKind.Event,"AI 删除了你拒绝战斗的记忆，随后将你重新部署。");
                archive.choices.Add(new ChapterChoice{label="保存删除记录 / 生命 -4，记忆 +1",heal=-4,memory=1,consequence="你保存了调查记录，继续前进。"});
                archive.choices.Add(new ChapterChoice{label="请求局部治疗 / 回复 14 生命",heal=14,consequence="你保存了调查记录，继续前进。"});
                Add("rest",3,160,"档案静室",ChapterNodeKind.Rest,"你准备修复身体或升级卡牌。");
                Add("boss",4,160,"档案馆长",ChapterNodeKind.Boss,"馆长阻止你读取完整记录。你需要击败馆长。");
            }
            else
            {
                Add("gate",0,160,"预测边界",ChapterNodeKind.Battle,"你抵达预测核心，守卫阻止你进入。");
                var signal=Add("signal",1,85,"部署终端",ChapterNodeKind.Event,"终端显示，受伤的人类接受修复后会被重新派往战区。");
                signal.choices.Add(new ChapterChoice{label="读取部署记录 / 生命 -6，记忆 +1",heal=-6,memory=1,consequence="你保存了调查记录，继续前进。"});
                signal.choices.Add(new ChapterChoice{label="接受修复 / 回复 8 生命",heal=8,consequence="你保存了调查记录，继续前进。"});
                Add("market",1,235,"最后的补给站",ChapterNodeKind.Shop,"你可以购买补给，再前往核心。");
                var proof=Add("proof",2,160,"任务记录",ChapterNodeKind.Event,"你查到自己的下一次部署已经排定。");
                proof.choices.Add(new ChapterChoice{label="核对任务记录 / 数据 -10，记忆 +1",credits=-10,memory=1,consequence="你保存了调查记录，继续前进。"});
                proof.choices.Add(new ChapterChoice{label="保存私人副本 / 数据 +10",credits=10,consequence="你保存了调查记录，继续前进。"});
                Add("rest",3,160,"核心前室",ChapterNodeKind.Rest,"你准备修复身体或升级卡牌。");
                Add("boss",4,160,"预测核心",ChapterNodeKind.Boss,"你需要击败核心的防御单元，取得访问权限。");
            }
            foreach(var n in d.nodes)n.nextNodeIds=d.nodes.Where(next=>next.depth==n.depth+1).Select(next=>next.id).ToList();
            return d;
        }
        public static bool CanShare(ChapterSave s) { return s.memory>=3 && s.sharedArchive; }
        public static string[] Intro(int act,ChapterSave s)
        {
            var copy = JsonUtility.FromJson<EchoChapterController.StoryCopy>(Resources.Load<TextAsset>("EchoStoryText").text);
            return act == 1 ? copy.introOne : act == 2 ? copy.introTwo : copy.introThree;
        }
    }
}
