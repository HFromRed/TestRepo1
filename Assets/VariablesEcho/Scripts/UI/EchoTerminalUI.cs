using UnityEngine;

namespace VariablesEcho
{
    // Shared terminal vocabulary. Draw only axis-aligned pixels: no GUI matrix mutation,
    // so lines remain clipped correctly inside IMGUI groups and scroll views.
    public static class EchoTerminalUI
    {
        public static readonly Color Ink = new Color(.025f,.043f,.067f);
        public static readonly Color Panel = new Color(.045f,.075f,.105f);
        public static readonly Color Edge = new Color(.16f,.27f,.32f);
        public static readonly Color Cyan = new Color(.32f,.94f,.83f);
        public static readonly Color Amber = new Color(.94f,.72f,.39f);
        public static readonly Color Text = new Color(.83f,.91f,.90f);
        public static readonly Color Muted = new Color(.40f,.54f,.59f);
        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }
        public static void Frame(Rect r, Color edge)
        {
            Fill(r, Panel);
            Fill(new Rect(r.x+4,r.y,r.width-8,1),edge);
            Fill(new Rect(r.x+4,r.yMax-1,r.width-8,1),edge);
            Fill(new Rect(r.x,r.y+4,1,r.height-8),edge);
            Fill(new Rect(r.xMax-1,r.y+4,1,r.height-8),edge);
            Fill(new Rect(r.x,r.y,4,4),edge);
            Fill(new Rect(r.xMax-4,r.yMax-4,4,4),edge);
        }
        public static void Label(Rect r,string text,GUIStyle source,int size,Color color,TextAnchor align=TextAnchor.UpperLeft)
        {
            var style = new GUIStyle(source) { fontSize=size, alignment=align, wordWrap=true, clipping=TextClipping.Clip };
            style.normal.textColor=color; GUI.Label(r,text,style);
        }
        public static bool Button(Rect r,string text,GUIStyle source,bool enabled=true)
        {
            bool hover=enabled && r.Contains(Event.current.mousePosition);
            Frame(r,enabled?Cyan:Edge);
            if(hover) Fill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),new Color(.10f,.25f,.27f));
            Label(r,text,source,16,enabled?Text:Muted,TextAnchor.MiddleCenter);
            bool old=GUI.enabled; GUI.enabled=old && enabled;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none); GUI.enabled=old;
            if(clicked) EchoAudioFeedback.Button();
            if(clicked) EchoAudioFeedback.Button();
            return clicked;
        }
        public static void Line(Vector2 a,Vector2 b,Color c,int width=2,bool dashed=false)
        {
            if(Event.current.type!=EventType.Repaint)return;
            int steps=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y))/2);
            for(int i=0;i<=steps;i++)
            {
                if(dashed && i%6>=3)continue;
                Vector2 p=Vector2.Lerp(a,b,steps==0?0:(float)i/steps);
                Fill(new Rect(Mathf.Round(p.x),Mathf.Round(p.y),width,width),c);
            }
        }
    }
}
