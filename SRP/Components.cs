using UnityEngine;

namespace SRP
{
    public class SRPBuildOverlay : MonoBehaviour
    {
        private static SRPBuildOverlay _instance;

        public static void Create()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SRPBuildOverlay");
            _instance = go.AddComponent<SRPBuildOverlay>();
            Object.DontDestroyOnLoad(go);
        }

        public static void Remove()
        {
            if (_instance != null)
            {
                Object.Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle();
                _style.fontSize = 20;
                _style.normal.textColor = Color.white;
                _style.alignment = TextAnchor.UpperRight;
                _style.fontStyle = FontStyle.Bold;
            }

            // Margin from the top right corner
            float margin = 20f;
            string text = "SRP Build";
            Vector2 size = _style.CalcSize(new GUIContent(text));
            
            // Screen.width and Screen.height give current resolution
            Rect rect = new Rect(Screen.width - size.x - margin, margin, size.x, size.y);
            
            // Ensure it's on top
            GUI.depth = -1000;
            
            // Shadow effect for better visibility
            GUIStyle shadowStyle = new GUIStyle(_style);
            shadowStyle.normal.textColor = new Color(0, 0, 0, 0.5f);
            Rect shadowRect = new Rect(rect.x + 2, rect.y + 2, size.x, size.y);
            GUI.Label(shadowRect, text, shadowStyle);
            
            GUI.Label(rect, text, _style);
        }
    }
}
