using SRP.ADOFAI.Keyviewer.Core;
using SRP.ADOFAI.Keyviewer.Core.Attributes;
using SRP.UI;
using UnityEngine;

namespace SRP.ADOFAI.Planet
{
    [RegisterTweak(
        id: "planet",
        settingsType: typeof(PlanetSettings),
        patchesType: typeof(PlanetPatches))]
    public class PlanetTweak : Tweak
    {
        public override string Name => TweakStrings.Get(TranslationKeys.Planet.NAME);
        public override string Description => TweakStrings.Get(TranslationKeys.Planet.DESCRIPTION);

        [SyncTweakSettings]
        public PlanetSettings Settings { get; set; }

        public override void OnEnable() {
            UpdatePlanetColors();
        }

        public override void OnDisable() {
            UpdatePlanetColors();
        }

        public override void OnSettingsGUI() {
            DrawPlanetSettings(Settings.RedPlanet, TweakStrings.Get(TranslationKeys.Planet.RED_PLANET));
            DrawPlanetSettings(Settings.BluePlanet, TweakStrings.Get(TranslationKeys.Planet.BLUE_PLANET));
            DrawPlanetSettings(Settings.GreenPlanet, TweakStrings.Get(TranslationKeys.Planet.GREEN_PLANET));
        }

        private void DrawPlanetSettings(PlanetProfile profile, string label) {
            profile.Enabled = GUILayout.Toggle(profile.Enabled, "<b>" + label + "</b>", PinkUI.Toggle);
            if (profile.Enabled) {
                MoreGUILayout.BeginIndent();
                
                GUILayout.Label(TweakStrings.Get(TranslationKeys.Planet.BODY_COLOR), PinkUI.Label);
                profile.BodyColor = ColorSliders(profile.BodyColor);
                
                GUILayout.Label(TweakStrings.Get(TranslationKeys.Planet.TAIL_COLOR), PinkUI.Label);
                profile.TailColor = ColorSliders(profile.TailColor);
                
                profile.BodyOpacity = MoreGUILayout.NamedSlider(TweakStrings.Get(TranslationKeys.Planet.BODY_OPACITY), profile.BodyOpacity, 0f, 100f, 200f);
                profile.TailOpacity = MoreGUILayout.NamedSlider(TweakStrings.Get(TranslationKeys.Planet.TAIL_OPACITY), profile.TailOpacity, 0f, 100f, 200f);
                profile.RingOpacity = MoreGUILayout.NamedSlider(TweakStrings.Get(TranslationKeys.Planet.RING_OPACITY), profile.RingOpacity, 0f, 100f, 200f);
                
                if (GUI.changed) {
                    UpdatePlanetColors();
                }
                
                MoreGUILayout.EndIndent();
            }
        }

        private Color ColorSliders(Color color) {
            Color newColor = color;
            GUILayout.BeginHorizontal();
            newColor.r = ColorSlider("R:", newColor.r);
            newColor.g = ColorSlider("G:", newColor.g);
            newColor.b = ColorSlider("B:", newColor.b);
            GUILayout.EndHorizontal();
            return newColor;
        }

        private float ColorSlider(string label, float value) {
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(20f));
            float result = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Width(100f));
            GUILayout.Label(((int)(result * 255)).ToString(), PinkUI.Label, GUILayout.Width(30f));
            return result;
        }

        private void UpdatePlanetColors() {
            if (scrController.instance == null) return;
            scrController.instance.planetRed?.planetarySystem?.ColorPlanets();
            scrController.instance.planetBlue?.planetarySystem?.ColorPlanets();
            scrController.instance.planetGreen?.planetarySystem?.ColorPlanets();
        }
    }
}
