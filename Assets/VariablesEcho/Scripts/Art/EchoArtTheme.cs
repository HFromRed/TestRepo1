using UnityEngine;
namespace VariablesEcho
{
    [CreateAssetMenu(menuName="Variables Echo/Art Theme")]
    public class EchoArtTheme : ScriptableObject
    {
        public Sprite ivoryBackground, wastelandBackground, neonBackground;
        public Sprite warden, salvager, cardFrame, panel, button, buttonHover, buttonPressed, buttonDisabled;
        public Sprite cardOutline;
        public Sprite attackIcon, blockIcon, noiseIcon, memoryIcon;
        public Color ivory = new Color(.86f,.83f,.73f), teal = new Color(.32f,.84f,.79f), coral = new Color(.99f,.39f,.42f);
        public static EchoArtTheme Instance { get { return Resources.Load<EchoArtTheme>("EchoArtTheme"); } }
    }
}

