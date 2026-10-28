#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CCGKit;
using UnityEngine;
namespace VariablesEcho
{
    // Exercises real Kit hand objects, mana payment, effects, turns, scenes and chapter saves.
    public class ChapterSmokeRunner : MonoBehaviour
    {
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--chapter-smoke")<0)return;
            var go=new GameObject("Chapter automated verification");DontDestroyOnLoad(go);go.AddComponent<ChapterSmokeRunner>();
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors++;}
        void Check(bool ok,string message){if(!ok){Debug.LogError("CHAPTER_CHECK_FAIL "+message);Application.Quit(2);throw new Exception(message);}Debug.Log("CHAPTER_CHECK_OK "+message);}
        IEnumerator Capture(string name)
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../docs/verification"));
            Directory.CreateDirectory(folder);
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture!=null){File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());Destroy(texture);}
            if(name=="03-battle" && Camera.main!=null)
            {
                // Explicit camera rendering works even when Windows hides/minimizes the test window.
                // This capture includes real world sprites, cards and camera-space UI; IMGUI is separate.
                var camera=Camera.main;var oldTarget=camera.targetTexture;
                var render=new RenderTexture(1280,800,24);render.Create();
                var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                var modes=canvases.Select(c=>c.renderMode).ToArray();
                var cameras=canvases.Select(c=>c.worldCamera).ToArray();
                var distances=canvases.Select(c=>c.planeDistance).ToArray();
                for(int i=0;i<canvases.Length;i++)if(modes[i]==RenderMode.ScreenSpaceOverlay){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
                Canvas.ForceUpdateCanvases();camera.targetTexture=render;camera.Render();
                var active=RenderTexture.active;RenderTexture.active=render;
                var pixels=new Texture2D(1280,800,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,800),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,"03-battle-camera.png"),pixels.EncodeToPNG());
                RenderTexture.active=active;camera.targetTexture=oldTarget;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
                // A separate close-up verifies the actual card renderer and printed rules.
                var hand=FindAnyObjectByType<HandPresentationSystem>();
                var list=(List<GameObject>)typeof(HandPresentationSystem).GetField("handCards",Fields).GetValue(hand);
                if(list.Count>0)
                {
                    var card=list[0];var transforms=card.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
                    var pos=card.transform.position;var rotation=card.transform.rotation;var scale=card.transform.localScale;
                    var cp=camera.transform.position;var ortho=camera.orthographicSize;var flags=camera.clearFlags;var background=camera.backgroundColor;var mask=camera.cullingMask;
                    for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=31;
                    card.transform.position=Vector3.zero;card.transform.rotation=Quaternion.identity;card.transform.localScale=Vector3.one;
                    camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=2.9f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.035f,.055f);camera.cullingMask=1<<31;
                    camera.targetTexture=render;camera.Render();RenderTexture.active=render;
                    pixels.ReadPixels(new Rect(0,0,1280,800),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"art-card-detail.png"),pixels.EncodeToPNG());
                    camera.targetTexture=oldTarget;RenderTexture.active=active;camera.transform.position=cp;camera.orthographicSize=ortho;camera.clearFlags=flags;camera.backgroundColor=background;camera.cullingMask=mask;
                    card.transform.position=pos;card.transform.rotation=rotation;card.transform.localScale=scale;
                    for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
                }
                Destroy(pixels);render.Release();Destroy(render);
            }
            yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            yield return Capture("01-title");
            var c=EchoChapterController.Active;Check(c!=null,"chapter entry loaded");
            bool elite=Array.IndexOf(Environment.GetCommandLineArgs(),"--chapter-elite")>=0;
            c.NewRun(elite?823:417);Check(c.State.deck.Count==10,"starter deck 10 cards");
            c.State.heroVariant=elite?1:0;
            c.Save();Check(c.Resume(),"checkpoint deserializes");
            yield return Capture("02-map");
            Time.timeScale=12;
            while(c.State.phase!="Complete" && c.State.phase!="Defeat")
            {
                if(c.State.phase=="Map")
                {
                    var node=elite?c.Data.nodes.Last(n=>n.depth==c.State.depth):c.Data.nodes.First(n=>n.depth==c.State.depth);
                    c.Visit(node.id);
                }
                if(c.State.phase=="Node")
                {
                    if(c.CurrentNode.enemy!=null)c.EnterBattle();
                    else if(c.CurrentNode.kind==ChapterNodeKind.Event)c.ChooseEvent(0);
                                        else if(c.CurrentNode.kind==ChapterNodeKind.Rest)c.Rest(elite && c.State.depth==5 ? 201 : -1);
                    else if(c.CurrentNode.kind==ChapterNodeKind.Shop)
                    {
                        int money=c.State.credits;
                        Check(c.RemoveCard(201),"shop removes chosen card");
                        Check(!c.RemoveCard(201),"shop rejects repeated removal");
                        Check(c.State.credits==money-c.Data.removePrice,"removal charges once");
                        Check(!c.BuyCard(),"shop rejects unaffordable purchase");
                        c.Save();Check(c.Resume() && c.State.shopRemoved,"shop purchase flags persist");
                        c.LeaveShop();
                    }
                }
                if(c.State.phase=="Battle")
                {
                    yield return new WaitForSeconds(3);
                    Check(EchoRuntime.Battle!=null,"battle initialized "+c.CurrentNode.id);
                    Check(EchoRuntime.Battle.Player.Template==c.CurrentHero,"selected protagonist persists into combat");
                    if(c.State.depth==0)yield return Capture("03-battle");
                    int turns=0;
                    while(c.State.phase=="Battle" && turns++<65)
                    {
                        var hand=FindAnyObjectByType<HandPresentationSystem>();
                        while(hand!=null && hand.IsAnimating())yield return null;
                        int plays=0;
                        while(c.State.phase=="Battle" && plays++<20)
                        {
                            var list=(List<GameObject>)typeof(HandPresentationSystem).GetField("handCards",Fields).GetValue(hand);
                            var battle=EchoRuntime.Battle;
                            var playable=list.Select(g=>g.GetComponent<CardObject>()).Where(o=>o.EffectiveCost<=battle.PlayerRuntime.Mana.Value).ToList();
                            // Simple mixed strategy: one block if shield is low, then spend on attacks and draw.
                            var card=playable.OrderByDescending(o=>Score(o,battle.PlayerRuntime.Shield.Value)).FirstOrDefault();
                            if(card==null)break;
                            bool targeted=CardUtils.CardHasTargetableEffect(card.Template);
                            CardSelectionSystem system=targeted?(CardSelectionSystem)FindAnyObjectByType<CardWithArrowSelectionSystem>():FindAnyObjectByType<CardWithoutArrowSelectionSystem>();
                            if(targeted)typeof(CardWithArrowSelectionSystem).GetField("selectedEnemy",Fields).SetValue(system,battle.Enemies[0].gameObject);
                            typeof(CardSelectionSystem).GetField("SelectedCard",Fields).SetValue(system,card.gameObject);
                            system.GetType().GetMethod("PlaySelectedCard",Fields).Invoke(system,null);
                            typeof(CardSelectionSystem).GetField("SelectedCard",Fields).SetValue(system,null);
                            yield return new WaitForSeconds(1.5f);
                        }
                        if(c.State.phase=="Battle") {FindAnyObjectByType<TurnManagementSystem>().EndPlayerTurn();yield return new WaitForSeconds(5);}
                    }
                    Check(turns<65,"battle terminates within 65 turns");
                    Debug.Log("CHAPTER_BALANCE "+c.CurrentNode.id+" turns="+turns+" hp="+c.State.hp);
                }
                if(c.State.phase=="Reward") {yield return new WaitForSeconds(2);c.ChooseReward(c.State.reward[0]);}
                if(c.State.phase=="Ending") {yield return new WaitForSeconds(2);yield return Capture("04-ending");c.ResolveEnding(!elite);}
                yield return null;
            }
            Check(c.State.phase=="Complete","route wins with unmodified HP and real cards");
            yield return Capture("05-complete");
            Check(c.Resume() && c.State.phase=="Complete","completed chapter persists");
            string completeSave=File.ReadAllText(EchoChapterController.SavePath);
            c.NewRun(111);c.State.credits=100;c.State.depth=3;c.Visit("market");
            Check(c.BuyCard() && !c.BuyCard(),"shop buys exactly once");
            c.LeaveShop();c.State.depth=5;c.Visit("rest");int oldDeck=c.State.deck.Count;
            c.Rest(201);Check(c.State.deck.Contains(1201) && c.State.deck.Count==oldDeck,"upgrade replaces one copy");
            c.NewRun(112);c.State.hp=2;c.Visit("gate");c.EnterBattle();
            yield return new WaitForSeconds(3);
            EchoRuntime.Battle.Noise.Add(100,"test-lethal-backlash");
            FindAnyObjectByType<TurnManagementSystem>().EndPlayerTurn();
            yield return new WaitForSeconds(2);
            Check(c.State.phase=="Defeat" && c.State.hp==0,"lethal backlash ends run without next enemy turn");
            File.WriteAllText(EchoChapterController.SavePath,completeSave);Check(c.Resume(),"restore completed fixture");
            Check(errors==0,"no runtime exceptions or errors");
            File.WriteAllText(Path.Combine(EchoSaveService.StorageRoot,"result.txt"),"PASS chapter full route; real card selection and damage; 417; HP "+c.State.hp);
            Debug.Log("CHAPTER_SMOKE_PASS");Application.Quit(0);
        }
        float Score(CardObject o,int shield)
        {
            float score=0;
            foreach(var e in o.Template.Effects)
            {
                if(e is GainShieldEffect s)score+=shield<6?s.Value*2:s.Value*.1f;
                if(e is DealDamageEffect d)score+=d.Value;
                if(e is DrawCardsEffect)score+=7;
                if(e is GainManaEffect)score+=20;
            }
            return score/(o.EffectiveCost+.6f);
        }
    }
}
#endif






