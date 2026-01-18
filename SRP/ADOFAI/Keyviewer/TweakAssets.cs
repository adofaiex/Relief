using UnityEngine;

namespace SRP.ADOFAI.Keyviewer
{
    /// <summary>
    /// Provides sprite assets for the Key Viewer tweak.
    /// </summary>
    internal static class TweakAssets
    {
        private static Sprite _keyBackgroundSprite;
        private static Sprite _keyOutlineSprite;

        /// <summary>
        /// Gets the sprite used for key backgrounds.
        /// </summary>
        public static Sprite KeyBackgroundSprite
        {
            get
            {
                if (_keyBackgroundSprite == null)
                {
                    _keyBackgroundSprite = CreateRoundedRectSprite(100, 100, 10);
                }
                return _keyBackgroundSprite;
            }
        }

        /// <summary>
        /// Gets the sprite used for key outlines.
        /// </summary>
        public static Sprite KeyOutlineSprite
        {
            get
            {
                if (_keyOutlineSprite == null)
                {
                    _keyOutlineSprite = CreateRoundedRectOutlineSprite(100, 100, 10, 4);
                }
                return _keyOutlineSprite;
            }
        }

        /// <summary>
        /// Creates a rounded rectangle sprite for key backgrounds.
        /// </summary>
        private static Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - (width - 1) / 2f) - (width / 2f - cornerRadius), 0);
                    float dy = Mathf.Max(Mathf.Abs(y - (height - 1) / 2f) - (height / 2f - cornerRadius), 0);
                    bool inside = Mathf.Sqrt(dx * dx + dy * dy) <= cornerRadius;
                    pixels[y * width + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius)
            );
        }

        /// <summary>
        /// Creates a rounded rectangle outline sprite for key borders.
        /// </summary>
        private static Sprite CreateRoundedRectOutlineSprite(int width, int height, int cornerRadius, int borderWidth)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - (width - 1) / 2f) - (width / 2f - cornerRadius), 0);
                    float dy = Mathf.Max(Mathf.Abs(y - (height - 1) / 2f) - (height / 2f - cornerRadius), 0);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist <= cornerRadius && dist >= (cornerRadius - borderWidth))
                    {
                        pixels[y * width + x] = Color.white;
                    }
                    else if (dist < (cornerRadius - borderWidth))
                    {
                        // Check if we are in the straight parts of the border
                        bool isEdge = x < borderWidth || x >= width - borderWidth ||
                                      y < borderWidth || y >= height - borderWidth;
                        pixels[y * width + x] = isEdge ? Color.white : Color.clear;
                    }
                    else
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius)
            );
        }
    }
}
