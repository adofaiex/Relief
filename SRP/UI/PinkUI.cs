using UnityEngine;

namespace SRP.UI
{
    public static class PinkUI
    {
        private static GUIStyle _buttonStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _textFieldStyle;
        private static GUIStyle _toggleStyle;

        public static Color MainPink = new Color(1f, 0.6f, 0.8f); // Soft Pink
        public static Color DarkPink = new Color(0.8f, 0.4f, 0.6f);
        public static Color LightPink = new Color(1f, 0.85f, 0.9f);

        public static GUIStyle Button {
            get {
                if (_buttonStyle == null) {
                    _buttonStyle = new GUIStyle(GUI.skin.button);
                    _buttonStyle.normal.textColor = Color.white;
                    _buttonStyle.hover.textColor = Color.white;
                    _buttonStyle.active.textColor = Color.white;
                    _buttonStyle.normal.background = CreateRoundedGradientTexture(128, 48, 8, DarkPink * 0.9f, DarkPink, true);
                    _buttonStyle.hover.background = CreateRoundedGradientTexture(128, 48, 8, MainPink * 0.9f, MainPink, true);
                    _buttonStyle.active.background = CreateRoundedGradientTexture(128, 48, 8, DarkPink * 0.7f, DarkPink * 0.8f, false);
                    _buttonStyle.margin = new RectOffset(4, 4, 4, 4);
                    _buttonStyle.padding = new RectOffset(12, 12, 6, 6);
                    _buttonStyle.fontStyle = FontStyle.Bold;
                    _buttonStyle.alignment = TextAnchor.MiddleCenter;
                    _buttonStyle.border = new RectOffset(12, 12, 12, 12);
                }
                return _buttonStyle;
            }
        }

        public static GUIStyle Label {
            get {
                if (_labelStyle == null) {
                    _labelStyle = new GUIStyle(GUI.skin.label);
                    _labelStyle.normal.textColor = DarkPink;
                    _labelStyle.fontStyle = FontStyle.Bold;
                }
                return _labelStyle;
            }
        }

        public static GUIStyle Box {
            get {
                if (_boxStyle == null) {
                    _boxStyle = new GUIStyle(GUI.skin.box);
                    _boxStyle.normal.background = CreateRoundedGradientTexture(256, 256, 12, LightPink * 0.95f, LightPink, true);
                    _boxStyle.padding = new RectOffset(12, 12, 12, 12);
                    _boxStyle.border = new RectOffset(16, 16, 16, 16);
                }
                return _boxStyle;
            }
        }

        public static GUIStyle TextField {
            get {
                if (_textFieldStyle == null) {
                    _textFieldStyle = new GUIStyle(GUI.skin.textField);
                    _textFieldStyle.normal.textColor = DarkPink;
                    _textFieldStyle.focused.textColor = DarkPink;
                    _textFieldStyle.normal.background = CreateRoundedGradientTexture(128, 32, 6, Color.white * 0.95f, Color.white, false);
                    _textFieldStyle.padding = new RectOffset(8, 8, 4, 4);
                    _textFieldStyle.border = new RectOffset(8, 8, 8, 8);
                }
                return _textFieldStyle;
            }
        }

        public static GUIStyle Toggle {
            get {
                if (_toggleStyle == null) {
                    _toggleStyle = new GUIStyle(GUI.skin.toggle);
                    _toggleStyle.normal.textColor = DarkPink;
                    _toggleStyle.onNormal.textColor = DarkPink;
                    _toggleStyle.fontStyle = FontStyle.Bold;
                }
                return _toggleStyle;
            }
        }

        private static Texture2D CreateTexture(int width, int height, Color col) {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static Texture2D CreateRoundedGradientTexture(int width, int height, int radius, Color colStart, Color colEnd, bool shadow) {
            Texture2D tex = new Texture2D(width, height);
            Color[] pix = new Color[width * height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float dist = 0;
                    bool inCorner = false;

                    // Corner detection
                    if (x < radius && y < radius) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)); inCorner = true; }
                    else if (x > width - radius - 1 && y < radius) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)); inCorner = true; }
                    else if (x < radius && y > height - radius - 1) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)); inCorner = true; }
                    else if (x > width - radius - 1 && y > height - radius - 1) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)); inCorner = true; }

                    if (inCorner && dist > radius) {
                        pix[y * width + x] = Color.clear;
                    } else {
                        // Gradient
                        float t = (float)y / height;
                        Color baseCol = Color.Lerp(colStart, colEnd, t);
                        
                        // Edge anti-aliasing
                        if (inCorner && dist > radius - 1f) {
                            baseCol.a *= (radius - dist);
                        }

                        // Simple Shadow effect (darken bottom edge)
                        if (shadow && y < 4) {
                            baseCol *= 0.8f;
                        }

                        pix[y * width + x] = baseCol;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
