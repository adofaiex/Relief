using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Relief.Modules.BuiltIn
{
    public class UIText : MonoBehaviour
    {
        public GameObject TextObject;
        public Text text;
        public Shadow shadowText;
        public RectTransform rectTransform;

        public void setSize(int size)
        {
            if (text == null) EnsureInitialized();
            text.fontSize = size;
            text.rectTransform.sizeDelta = new Vector2(text.preferredWidth, text.preferredHeight);
        }

        public void setText(string textContent)
        {
            if (text == null) EnsureInitialized();
            text.text = textContent;
            //this.text.rectTransform.sizeDelta = new Vector2(this.text.preferredWidth, this.text.preferredHeight);
        }

        public void setPosition(float x, float y)
        {
            if (rectTransform == null) EnsureInitialized();
            Vector2 pos = new Vector2(x, y);
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;
        }

        public void setFont(Font font)
        {
            if (text == null) EnsureInitialized();
            text.font = font;
        }

        public void setAlignment(TextAnchor alignment)
        {
            if (text == null) EnsureInitialized();
            text.alignment = alignment;
        }

        public void setFontStyle(FontStyle fontStyle)
        {
            if (text == null) EnsureInitialized();
            text.fontStyle = fontStyle;
        }

        public void setShadowEnabled(bool enabled)
        {
            if (shadowText == null) EnsureInitialized();
            if (shadowText != null)
            {
                shadowText.enabled = enabled;
            }
        }

        private bool _initialized = false;

        void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (gameObject.GetComponent<Canvas>() == null)
            {
                Canvas mainCanvas = gameObject.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 10001;
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (TextObject == null)
            {
                TextObject = new GameObject("TextContainer");
                TextObject.transform.SetParent(transform);
                rectTransform = TextObject.AddComponent<RectTransform>();
            }

            if (text == null)
            {
                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(TextObject.transform);

                text = textObject.AddComponent<Text>();
                // Default values, can be set by caller
                text.alignment = TextAnchor.UpperLeft;
                text.fontSize = 24;
                text.color = Color.white;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                
                // Set default font (Arial usually exists)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                shadowText = textObject.AddComponent<Shadow>();
                shadowText.effectColor = new Color(0f, 0f, 0f, 0.45f);
                shadowText.effectDistance = new Vector2(2f, -2f);

                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.pivot = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        public TextAnchor toAlign(int _align)
        {
            if (_align == 0) return TextAnchor.UpperLeft;
            if (_align == 1) return TextAnchor.UpperCenter;
            return TextAnchor.UpperRight;
        }
    }
}