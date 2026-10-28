using CCGKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace VariablesEcho
{
    public static class EchoArtSkin
    {
        public static void ApplyCard(CardObject card, SpriteRenderer picture)
        {
            var theme=EchoArtTheme.Instance;if(theme==null)return;
            var frame=card.GetComponent<SpriteRenderer>();
            if(frame!=null && frame.sprite!=theme.cardFrame)
            {
                var size=frame.sprite.bounds.size;
                frame.sprite=theme.cardFrame;
                // Use sliced draw mode to preserve the Kit card geometry and all hit boxes.
                frame.drawMode=SpriteDrawMode.Sliced;frame.size=new Vector2(3.5f,5f);frame.color=Color.white;
            }
            foreach(var renderer in card.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name!="Glow")continue;
                renderer.sprite=theme.cardOutline;renderer.drawMode=SpriteDrawMode.Sliced;
                renderer.size=frame.size+Vector2.one*.08f;
                renderer.transform.localPosition=Vector3.zero;
            }
            var mask=card.GetComponentInChildren<SpriteMask>();
            if(mask!=null && picture.sprite!=null)
            {
                var target=new Vector3(3.15f,2.55f,1);var size=picture.sprite.bounds.size;
                float scale=Mathf.Min(target.x/size.x,target.y/size.y);
                picture.transform.localScale=new Vector3(scale,scale,1);
                var artBase=card.transform.Find("IvoryArtWell");
                if(artBase==null)
                {
                    var go=new GameObject("IvoryArtWell");go.transform.SetParent(card.transform,false);artBase=go.transform;
                    var bg=go.AddComponent<SpriteRenderer>();bg.sprite=EchoPixelUI.Solid;bg.drawMode=SpriteDrawMode.Sliced;
                    bg.color=new Color(.045f,.045f,.075f);bg.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
                    bg.sortingLayerID=picture.sortingLayerID;bg.sortingOrder=picture.sortingOrder-1;
                }
                artBase.localPosition=picture.transform.localPosition;
                artBase.GetComponent<SpriteRenderer>().size=new Vector2(target.x,target.y);
            }
            foreach(var text in card.GetComponentsInChildren<TextMeshPro>(true))
            {
                text.color=theme.ivory;
                text.enableAutoSizing=true;
                text.fontSizeMin=2.3f;text.fontSizeMax=4;
                if(text.name.Contains("Name")){text.rectTransform.anchoredPosition=new Vector2(.12f,2.08f);text.rectTransform.sizeDelta=new Vector2(2.65f,.45f);text.alignment=TextAlignmentOptions.Center;}
                if(text.name.Contains("Cost")){text.color=theme.teal;text.rectTransform.anchoredPosition=new Vector2(-1.45f,2.12f);}
                if(text.name.Contains("Type")){text.rectTransform.anchoredPosition=new Vector2(0,-1.15f);text.rectTransform.sizeDelta=new Vector2(2.8f,.3f);text.fontSizeMin=1.7f;text.fontSizeMax=2;text.textWrappingMode=TextWrappingModes.NoWrap;}
                if(text.name.Contains("Description")){text.rectTransform.anchoredPosition=new Vector2(0,-1.85f);text.rectTransform.sizeDelta=new Vector2(3.02f,.9f);text.fontSizeMax=3;}
            }
        }
        public static void ApplyHealth(Image bar,Image trail)
        {
            var theme=EchoArtTheme.Instance;if(theme==null)return;
            bar.color=theme.coral;trail.color=theme.ivory;
        }
    }
    // Only presentation; it never changes combat stats or event bindings.
    public class EchoBattleVisualSkin : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            yield return null;yield return null;
            var theme=EchoArtTheme.Instance;if(theme==null)yield break;
            foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var image=button.GetComponent<Image>();if(image==null)continue;
                image.sprite=theme.button;image.type=Image.Type.Sliced;image.color=Color.white;
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=theme.teal;colors.pressedColor=theme.coral;colors.disabledColor=Color.gray;button.colors=colors;
                foreach(var text in button.GetComponentsInChildren<TMP_Text>())text.color=theme.ivory;
            }
            foreach(var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                text.color=text.name.Contains("Border")?new Color(.04f,.04f,.065f):theme.ivory;
            foreach(var mana in FindObjectsByType<ManaWidget>(FindObjectsSortMode.None))
            {
                foreach(var image in mana.GetComponentsInChildren<Image>(true)){image.sprite=theme.panel;image.type=Image.Type.Sliced;image.color=Color.white;}
                foreach(var text in mana.GetComponentsInChildren<TMP_Text>())text.color=text.name.Contains("Border")?Color.black:theme.teal;
            }
        }
    }
}





