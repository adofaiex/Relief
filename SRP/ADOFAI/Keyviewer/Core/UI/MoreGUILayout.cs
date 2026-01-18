using System;
using System.Collections.Generic;
using UnityEngine;
using SRP.UI;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Additional GUI layout utilities.
    /// </summary>
    public static class MoreGUILayout
    {
        private static int indentLevel = 0;

        public static void BeginIndent()
        {
            indentLevel++;
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f * indentLevel);
            GUILayout.BeginVertical();
        }

        public static void EndIndent()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            indentLevel--;
        }

        public static void HorizontalLine(float height, float width)
        {
            GUILayout.Box("", GUILayout.Height(height), GUILayout.Width(width));
        }

        public static string NamedTextField(string label, string value, float width)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(150f));
            string result = GUILayout.TextField(value, PinkUI.TextField, GUILayout.Width(width));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return result;
        }

        public static (string, string) NamedTextFieldPair(string label1, string label2, string value1, string value2, float labelWidth, float fieldWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label1, PinkUI.Label, GUILayout.Width(labelWidth));
            string result1 = GUILayout.TextField(value1, PinkUI.TextField, GUILayout.Width(fieldWidth));
            GUILayout.Space(20f);
            GUILayout.Label(label2, PinkUI.Label, GUILayout.Width(labelWidth));
            string result2 = GUILayout.TextField(value2, PinkUI.TextField, GUILayout.Width(fieldWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return (result1, result2);
        }

        public static float NamedSlider(string label, float value, float min, float max, float width, float roundNearest = 1f, string valueFormat = "{0:0}")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(200f));
            float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(width));
            result = Mathf.Round(result / roundNearest) * roundNearest;
            GUILayout.Label(string.Format(valueFormat, result), PinkUI.Label, GUILayout.Width(50f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return result;
        }

        public static bool ToggleList<T>(List<T> items, ref int selectedIndex, Func<T, string> labelFunc)
        {
            bool changed = false;
            for (int i = 0; i < items.Count; i++)
            {
                bool isSelected = i == selectedIndex;
                bool newSelected = GUILayout.Toggle(isSelected, labelFunc(items[i]), PinkUI.Toggle);
                if (newSelected && !isSelected)
                {
                    selectedIndex = i;
                    changed = true;
                }
            }
            return changed;
        }

        public static (Color, Color) ColorRgbaSlidersPair(Color color1, Color color2)
        {
            Color newColor1 = color1;
            Color newColor2 = color2;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            newColor1.r = ColorSlider("R:", color1.r);
            newColor1.g = ColorSlider("G:", color1.g);
            newColor1.b = ColorSlider("B:", color1.b);
            newColor1.a = ColorSlider("A:", color1.a);
            GUILayout.EndVertical();

            GUILayout.Space(20f);

            GUILayout.BeginVertical();
            newColor2.r = ColorSlider("R:", color2.r);
            newColor2.g = ColorSlider("G:", color2.g);
            newColor2.b = ColorSlider("B:", color2.b);
            newColor2.a = ColorSlider("A:", color2.a);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return (newColor1, newColor2);
        }

        private static float ColorSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(30f));
            float result = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Width(150f));
            GUILayout.Label(((int)(result * 255)).ToString(), PinkUI.Label, GUILayout.Width(30f));
            GUILayout.EndHorizontal();
            return result;
        }
    }
}
