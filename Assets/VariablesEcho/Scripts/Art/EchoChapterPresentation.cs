using UnityEngine;
namespace VariablesEcho
{
    public enum EchoArtRegion { Ivory, Wasteland, Interference }
    public static class EchoChapterPresentation
    {
        public static Color Accent(EchoArtRegion region)
        {
            return region==EchoArtRegion.Wasteland ? new Color(1,.40f,.20f) : region==EchoArtRegion.Interference ? new Color(.68f,.98f,.17f) : new Color(.34f,.86f,.80f);
        }
        public static Sprite Background(EchoArtRegion region)
        {
            var t=EchoArtTheme.Instance;if(t==null)return null;
            return region==EchoArtRegion.Wasteland?t.wastelandBackground:region==EchoArtRegion.Interference?t.neonBackground:t.ivoryBackground;
        }
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public static void Panel(Rect r)
        {
            var t=EchoArtTheme.Instance;if(t==null){Fill(r,new Color(.04f,.045f,.07f,.97f));return;}
            var s=new GUIStyle {border=new RectOffset(20,20,20,20)};s.normal.background=t.panel.texture;GUI.Box(r,GUIContent.none,s);
        }
        public static void Backdrop(EchoArtRegion region,bool title,bool alternate)
        {
            var t=EchoArtTheme.Instance;if(t==null)return;
            var bg=Background(region);if(bg!=null)GUI.DrawTexture(new Rect(0,0,1280,800),bg.texture,ScaleMode.ScaleAndCrop);
            Fill(new Rect(0,0,1280,800),new Color(.025f,.025f,.06f,.35f));
            if(title)
            {
                var hero=alternate?t.salvager:t.warden;
                if(hero!=null)GUI.DrawTexture(new Rect(820,75,430,690),hero.texture,ScaleMode.ScaleToFit,true);
                Panel(new Rect(40,24,770,750));
            }
            else Panel(new Rect(40,24,1200,750));
            Fill(new Rect(34,38,5,720),Accent(region));
        }
        public static void NoiseMeter(int noise,GUIStyle text)
        {
            Panel(new Rect(18,8,330,102));
            var tint=noise<36?Accent(EchoArtRegion.Ivory):noise<70?new Color(1,.68f,.28f):new Color(1,.38f,.43f);
            var prior=GUI.color;GUI.color=tint;
            GUI.Label(new Rect(32,16,300,26),"◈  RESONANCE / "+noise.ToString("000"),text);GUI.color=prior;
            Fill(new Rect(32,51,300,43),new Color(.03f,.035f,.055f));
            for(int i=0;i<50;i++)
            {
                float h=8+(i*17%31);
                Fill(new Rect(36+i*5.8f,72-h/2,3,h),i<noise*.5f?tint:new Color(.16f,.18f,.22f));
            }
            Fill(new Rect(36+35*.5f*5.8f,50,2,44),new Color(.85f,.83f,.73f,.65f));
            Fill(new Rect(36+69*.5f*5.8f,50,2,44),new Color(.85f,.83f,.73f,.65f));
        }
    }
}
