using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 运行时生成的像素风 UI 贴图。全部使用 Point 过滤与硬边，
    /// 不依赖任何外部美术素材，替换皮肤时只需改这里。
    /// </summary>
    public static class EchoPixelUI
    {
        private static Sprite solid;
        private static Sprite frame;
        private static Sprite notch;

        /// <summary>1 像素白块，配合 Image.color 使用。</summary>
        public static Sprite Solid
        {
            get
            {
                if (solid == null)
                    solid = Build(8, 8, (x, y) => Color.white);
                return solid;
            }
        }

        /// <summary>厚边框像素面板（九宫格），中间镂空。</summary>
        public static Sprite Frame
        {
            get
            {
                var theme=EchoArtTheme.Instance;
                if(theme!=null && theme.panel!=null)return theme.panel;
                if (frame == null)
                    frame = Build(48, 48, (x, y) =>
                    {
                        const int border = 7;
                        const int cut = 5;
                        // 四角斜切成阶梯状，形成硬朗的像素轮廓
                        var cornerX = x < cut ? cut - x : (47 - x < cut ? cut - (47 - x) : 0);
                        var cornerY = y < cut ? cut - y : (47 - y < cut ? cut - (47 - y) : 0);
                        if (cornerX > 0 && cornerY > 0 && cornerX + cornerY > cut)
                            return Color.clear;

                        var onBorder = x < border || y < border || x >= 48 - border || y >= 48 - border;
                        return onBorder ? Color.white : Color.clear;
                    }, new Vector4(14f, 14f, 14f, 14f));
                return frame;
            }
        }

        /// <summary>单个像素格：亮底 + 右下暗边，放大后像 CRT 的一个点。</summary>
        public static Sprite Cell
        {
            get
            {
                if (notch == null)
                    notch = Build(10, 10, (x, y) =>
                    {
                        if (x == 9 || y == 0)
                            return new Color(0f, 0f, 0f, 0.35f);
                        return Color.white;
                    });
                return notch;
            }
        }

        private static Sprite Build(int width, int height, System.Func<int, int, Color> pixel, Vector4 border = default)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    texture.SetPixel(x, y, pixel(x, y));
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
        }
    }
}

