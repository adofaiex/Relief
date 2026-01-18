using SRP.ADOFAI.Keyviewer.Core;
using UnityEngine;

namespace SRP.ADOFAI.Planet
{
    /// <summary>
    /// Individual profile for a planet's color and opacity.
    /// </summary>
    public class PlanetProfile
    {
        public bool Enabled = false;
        public Color BodyColor = Color.white;
        public Color TailColor = Color.white;
        public float BodyOpacity = 100f;
        public float TailOpacity = 100f;
        public float RingOpacity = 100f;
    }

    /// <summary>
    /// Settings for the Planet tweak.
    /// </summary>
    public class PlanetSettings : TweakSettings
    {
        public PlanetProfile RedPlanet = new PlanetProfile();
        public PlanetProfile BluePlanet = new PlanetProfile();
        public PlanetProfile GreenPlanet = new PlanetProfile();
    }
}
