#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace VariablesEcho
{
    // Narrative/state tests intentionally simulate battle resolution; they do not claim combat coverage.
    public class CampaignFlowSmoke : MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static bool booted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(booted || Array.IndexOf(Environment.GetCommandLineArgs(),"--campaign-smoke")<0)return;
            booted=true;DontDestroyOnLoad(new GameObject("Campaign flow verification",typeof(CampaignFlowSmoke)));
        }
        void Check(bool ok,string text){if(!ok)throw new Exception("CAMPAIGN_FAIL "+text);Debug.Log("CAMPAIGN_OK "+text);}
        void Phase(EchoChapterController c,string phase)
        {typeof(EchoChapterController).GetField("screen",Private).SetValue(c,phase);c.State.phase=phase;}
        IEnumerator Start()
        {
            yield return null;
            try{Run();Finish(0);}catch(Exception e){Debug.LogException(e);Finish(2);}
        }
        void Finish(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
        void Run()
        {
            var c=EchoChapterController.Active;Check(c!=null,"entry controller");
            for(int ending=0;ending<3;ending++)
            {
                c.NewRun(914+ending);int steps=0;
                Check(c.State.phase=="Intro","new journey begins with intro");
                while(c.State.phase!="Complete" && steps++<120)
                {
                    if(c.State.phase=="Intro"){c.ContinueIntro();continue;}
                    if(c.State.phase=="Map")
                    {
                        var next=c.Data.nodes.FirstOrDefault(n=>c.CanVisit(n));Check(next!=null,"reachable next node act "+c.State.act);
                        c.Visit(next.id);continue;
                    }
                    if(c.State.phase=="Node")
                    {
                        var node=c.CurrentNode;
                        if(node.kind==ChapterNodeKind.Event)
                        {
                            int depth=c.State.depth;string id=node.id;
                            c.ChooseEvent(0);Check(c.State.phase=="Consequence" && !string.IsNullOrEmpty(c.State.resultText),"event result has dedicated page");
                            Check(c.State.depth==depth,"reading result does not advance route");
                            int after=c.State.memory;string result=c.State.resultText;
                            Check(c.Resume() && c.State.phase=="Consequence" && c.State.resultText==result,"result survives reload");
                            c.ChooseEvent(0);Check(c.State.memory==after,"result cannot grant reward twice");
                            c.ContinueConsequence();Check(c.State.depth==depth+1 && c.State.visitedNodeIds.Contains(id),"confirm advances once");
                            c.ContinueConsequence();Check(c.State.depth==depth+1,"double confirm safe");
                        }
                        else if(node.kind==ChapterNodeKind.Shop)c.LeaveShop();
                        else if(node.kind==ChapterNodeKind.Rest)c.Rest();
                        else if(node.kind==ChapterNodeKind.Boss){Phase(c,"Ending");}
                        else {Phase(c,"Reward");c.State.reward.Clear();c.ChooseReward(-1);}
                        continue;
                    }
                    if(c.State.phase=="Ending")
                    {
                        if(c.State.act<3)c.ResolveEnding(true);
                        else {c.ResolveFinal(0);}
                        continue;
                    }
                    if(c.State.phase=="ChapterComplete")
                    {
                        int act=c.State.act,deck=c.State.deck.Count,credits=c.State.credits,memory=c.State.memory;
                        Check(c.Resume() && c.State.act==act,"chapter completion survives reload");
                        c.BeginNextChapter();
                        Check(c.State.act==act+1 && c.State.depth==0 && c.State.phase=="Intro","chapter transition");
                        Check(c.State.deck.Count==deck && c.State.credits==credits && c.State.memory==memory,"cross chapter resources retained");
                        Check(c.State.visitedNodeIds.Count==0 && c.State.completedNodeId=="","route history resets");
                        Check(c.Resume() && c.State.phase=="Intro","next chapter intro survives reload");continue;
                    }
                    throw new Exception("Unexpected phase "+c.State.phase);
                }
                Check(steps<120 && c.State.act==3 && c.State.phase=="Complete" && !string.IsNullOrEmpty(c.State.epilogue),"complete ending "+ending);
                Check(c.Resume() && c.State.phase=="Complete","final epilogue persists");
            }
            Debug.Log("CAMPAIGN_FLOW_PASS: three acts, fixed ending; combat resolution simulated");
        }
    }
}
#endif
