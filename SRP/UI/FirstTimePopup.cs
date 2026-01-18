using UnityEngine;
using SRP;

namespace SRP.UI
{
    public class FirstTimePopup : MonoBehaviour
    {
        private static FirstTimePopup instance;
        private Rect windowRect;
        private string message;
        private string buttonText;
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        public static void Create()
        {
            if (instance != null) return;
            GameObject go = new GameObject("SRPFirstTimePopup");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FirstTimePopup>();
        }

        private void Awake()
        {
            message = Localization.GetFirstTimeMessage();
            buttonText = Localization.GetButtonText();
            // Center of screen
            windowRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 100, 500, 200);
        }

        private void OnGUI()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.fontSize = 20;
                windowStyle.normal.textColor = Color.white;
                
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 18;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.wordWrap = true;
                labelStyle.normal.textColor = Color.white;

                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 18;
                buttonStyle.normal.textColor = Color.white;
            }

            GUI.depth = -2000; // Top layer
            windowRect = GUI.Window(999, windowRect, DrawWindow, "Samui Resource Pack");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message, labelStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(40)))
            {
                MainClass.Settings.FirstTimeLaunch = false;
                MainClass.Settings.Save(MainClass.ModEntry);
                Destroy(gameObject);
            }
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
