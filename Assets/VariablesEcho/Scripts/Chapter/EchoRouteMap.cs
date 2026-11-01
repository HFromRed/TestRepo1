using System;
using UnityEngine;
namespace VariablesEcho
{
    public static class EchoRouteMap
    {
        static string selectedId;
        static ChapterSave viewedState;
        static int viewedDepth = -1;
        static string Kind(ChapterNodeKind k)
        {
            switch(k) { case ChapterNodeKind.Battle:return "战斗"; case ChapterNodeKind.Elite:return "精英";
                case ChapterNodeKind.Rest:return "休整"; case ChapterNodeKind.Shop:return "交易";
                case ChapterNodeKind.Event:return "事件"; default:return "首领"; }
        }
        static string Name(ChapterNode n) { return string.IsNullOrEmpty(n.title)?Kind(n.kind)+"节点":n.title; }
        static void Icon(Vector2 p,ChapterNodeKind kind,Color c)
        {
            // Five-by-five pixel glyphs, scaled by four. No antialiasing or font dependencies.
            string[] rows;
            switch(kind) {
                case ChapterNodeKind.Battle:rows=new[]{"10001","01010","00100","01010","10001"};break;
                case ChapterNodeKind.Elite:rows=new[]{"10101","01110","11111","01110","10101"};break;
                case ChapterNodeKind.Rest:rows=new[]{"00100","00100","11111","00100","00100"};break;
                case ChapterNodeKind.Shop:rows=new[]{"01110","10001","11111","10001","11111"};break;
                case ChapterNodeKind.Boss:rows=new[]{"10101","11111","11111","01110","00100"};break;
                default:rows=new[]{"01110","10001","00110","00000","00100"};break;
            }
            for(int y=0;y<5;y++)for(int x=0;x<5;x++)if(rows[y][x]=='1')EchoTerminalUI.Fill(new Rect(p.x-10+x*4,p.y-10+y*4,4,4),c);
        }
        public static void Draw(Rect area,EchoChapterController director,GUIStyle label,GUIStyle small)
        {
            var data=director.Data; var state=director.State;
            if(state==null || data.nodes.Count==0)return;
            var cyan=EchoTerminalUI.Cyan; var muted=EchoTerminalUI.Muted; var amber=EchoTerminalUI.Amber;
            float mapWidth=area.width;
            ChapterNode selected=data.nodes.Find(n=>n.id==selectedId);
            if(selected==null || viewedState!=state || viewedDepth!=state.depth)
                selected=data.nodes.Find(n=>director.CanVisit(n)) ?? data.nodes[0];
            viewedState=state; viewedDepth=state.depth; selectedId=selected.id;
            int maxDepth=0;
            foreach(var n in data.nodes)maxDepth=Mathf.Max(maxDepth,n.depth);
            Func<ChapterNode,Vector2> position=n=> {
                var peers=data.nodes.FindAll(other=>other.depth==n.depth);
                peers.Sort((a,b)=>a.mapPosition.y.CompareTo(b.mapPosition.y));
                int lane=peers.IndexOf(n);
                float y=peers.Count<=1?(area.height-100)/2:100+lane*(area.height-280)/Mathf.Max(1,peers.Count-1);
                return new Vector2(66+n.depth/(float)Mathf.Max(1,maxDepth)*(mapWidth-132),y);
            };
            string pending=null;
            GUI.BeginGroup(area);
            try {
                EchoTerminalUI.Frame(new Rect(0,0,mapWidth,area.height),EchoTerminalUI.Edge);
                for(int x=18;x<mapWidth-10;x+=20)for(int y=58;y<area.height-55;y+=20)
                    EchoTerminalUI.Fill(new Rect(x,y,1,1),EchoTerminalUI.Edge);
                foreach(var node in data.nodes) {
                    if(node.nextNodeIds==null)continue;
                    foreach(var id in node.nextNodeIds) {
                        var next=data.nodes.Find(n=>n.id==id);if(next==null)continue;
                        bool walked=state.visitedNodeIds.Contains(node.id)&&state.visitedNodeIds.Contains(id);
                        bool active=state.completedNodeId==node.id&&director.CanVisit(next);
                        var a=position(node);var b=position(next);float elbow=(a.x+b.x)/2;
                        Color color=walked?amber:active?cyan:EchoTerminalUI.Edge;
                        Vector2 c=new Vector2(elbow,a.y),d=new Vector2(elbow,b.y);
                        EchoTerminalUI.Line(a,c,color,2,!walked&&!active);EchoTerminalUI.Line(c,d,color,2,!walked&&!active);EchoTerminalUI.Line(d,b,color,2,!walked&&!active);
                    }
                }
                foreach(var node in data.nodes) {
                    var p=position(node);bool available=director.CanVisit(node),done=state.visitedNodeIds.Contains(node.id);
                    Color color=available?cyan:done?amber:muted;
                    var hit=new Rect(p.x-25,p.y-25,50,50);
                    if(node==selected) {
                        color=cyan;
                        EchoTerminalUI.Fill(new Rect(p.x-36,p.y-36,72,72),cyan);
                        EchoTerminalUI.Fill(new Rect(p.x-32,p.y-32,64,64),new Color(.08f,.28f,.28f));
                    }
                    EchoTerminalUI.Frame(hit,color);
                    if(node==selected)EchoTerminalUI.Fill(new Rect(hit.x+2,hit.y+2,hit.width-4,hit.height-4),new Color(.08f,.28f,.28f));
                    Icon(p,node.kind,color);
                    EchoTerminalUI.Label(new Rect(p.x-66,p.y+37,132,40),Name(node),small,16,color,TextAnchor.UpperCenter);

                    if(GUI.Button(new Rect(p.x-45,p.y-30,90,108),GUIContent.none,GUIStyle.none)) {EchoAudioFeedback.NodeSelect();selected=node;selectedId=node.id;}
                }
                bool can=director.CanVisit(selected);
                if(EchoTerminalUI.Button(new Rect((mapWidth-260)/2,area.height-62,260,48),can?"前往 · "+Name(selected):"未解锁",label,can))pending=selected.id;

            } finally { GUI.EndGroup(); }
            if(pending!=null)director.QueueMapVisit(pending);
        }
    }
}
