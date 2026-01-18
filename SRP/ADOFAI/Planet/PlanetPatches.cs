using HarmonyLib;
using UnityEngine;
using SRP.ADOFAI.Keyviewer.Core.Attributes;

namespace SRP.ADOFAI.Planet
{
    internal static class PlanetPatches
    {
        [SyncTweakSettings]
        public static PlanetSettings Settings { get; set; }

        private static PlanetProfile Red => Settings?.RedPlanet;
        private static PlanetProfile Blue => Settings?.BluePlanet;
        private static PlanetProfile Green => Settings?.GreenPlanet;

        private static bool IsRedPlanet(PlanetRenderer renderer) {
            return renderer != null && scrController.instance != null && renderer == scrController.instance.planetRed?.planetRenderer;
        }

        private static bool IsBluePlanet(PlanetRenderer renderer) {
            return renderer != null && scrController.instance != null && renderer == scrController.instance.planetBlue?.planetRenderer;
        }

        private static bool IsGreenPlanet(PlanetRenderer renderer) {
            return renderer != null && scrController.instance != null && renderer == scrController.instance.planetGreen?.planetRenderer;
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetPlanetColor")]
        private static class SetPlanetColorPatch
        {
            public static void Prefix(PlanetRenderer __instance, ref Color color) {
                if (IsRedPlanet(__instance) && Red.Enabled) {
                    color = Red.BodyColor;
                    color.a *= Red.BodyOpacity / 100f;
                } else if (IsBluePlanet(__instance) && Blue.Enabled) {
                    color = Blue.BodyColor;
                    color.a *= Blue.BodyOpacity / 100f;
                } else if (IsGreenPlanet(__instance) && Green.Enabled) {
                    color = Green.BodyColor;
                    color.a *= Green.BodyOpacity / 100f;
                }
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetCoreColor")]
        private static class SetCoreColorPatch
        {
            public static void Prefix(PlanetRenderer __instance, ref Color color) {
                if (IsRedPlanet(__instance) && Red.Enabled) {
                    color = Red.BodyColor;
                    color.a *= Red.BodyOpacity / 100f;
                } else if (IsBluePlanet(__instance) && Blue.Enabled) {
                    color = Blue.BodyColor;
                    color.a *= Blue.BodyOpacity / 100f;
                } else if (IsGreenPlanet(__instance) && Green.Enabled) {
                    color = Green.BodyColor;
                    color.a *= Green.BodyOpacity / 100f;
                }
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetTailColor")]
        private static class SetTailColorPatch
        {
            public static void Prefix(PlanetRenderer __instance, ref Color color) {
                if (IsRedPlanet(__instance) && Red.Enabled) {
                    color = Red.TailColor;
                    color.a *= Red.TailOpacity / 100f;
                } else if (IsBluePlanet(__instance) && Blue.Enabled) {
                    color = Blue.TailColor;
                    color.a *= Blue.TailOpacity / 100f;
                } else if (IsGreenPlanet(__instance) && Green.Enabled) {
                    color = Green.TailColor;
                    color.a *= Green.TailOpacity / 100f;
                }
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetRingColor")]
        private static class SetRingColorPatch
        {
            public static void Prefix(PlanetRenderer __instance, ref Color color) {
                if (IsRedPlanet(__instance) && Red.Enabled) {
                    color = Red.BodyColor; // Ring usually uses body color
                    color.a *= Red.RingOpacity / 100f;
                } else if (IsBluePlanet(__instance) && Blue.Enabled) {
                    color = Blue.BodyColor;
                    color.a *= Blue.RingOpacity / 100f;
                } else if (IsGreenPlanet(__instance) && Green.Enabled) {
                    color = Green.BodyColor;
                    color.a *= Green.RingOpacity / 100f;
                }
            }
        }
        
        [HarmonyPatch(typeof(PlanetRenderer), "SetFaceColor")]
        private static class SetFaceColorPatch
        {
            public static void Prefix(PlanetRenderer __instance, ref Color color) {
                if (IsRedPlanet(__instance) && Red.Enabled) {
                    color.a *= Red.BodyOpacity / 100f;
                } else if (IsBluePlanet(__instance) && Blue.Enabled) {
                    color.a *= Blue.BodyOpacity / 100f;
                } else if (IsGreenPlanet(__instance) && Green.Enabled) {
                    color.a *= Green.BodyOpacity / 100f;
                }
            }
        }
    }
}
