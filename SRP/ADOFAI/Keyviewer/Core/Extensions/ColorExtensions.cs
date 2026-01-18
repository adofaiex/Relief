using UnityEngine;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Extension methods for Color.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a new color with the specified alpha value.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
