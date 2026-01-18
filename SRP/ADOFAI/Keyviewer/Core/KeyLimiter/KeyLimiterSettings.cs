using System.Collections.Generic;
using UnityEngine;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Settings for the Key Limiter tweak (for migration purposes).
    /// </summary>
    public class KeyLimiterSettings : TweakSettings
    {
        public List<KeyCode> ActiveKeys { get; set; } = new List<KeyCode>();
        public bool ViewerOnlyGameplay { get; set; }
        public bool AnimateKeys { get; set; } = true;
        public float KeyViewerSize { get; set; } = 100f;
        public float KeyViewerXPos { get; set; } = 0.89f;
        public float KeyViewerYPos { get; set; } = 0.03f;
        public Color PressedOutlineColor { get; set; } = Color.black;
        public Color ReleasedOutlineColor { get; set; } = Color.black;
        public Color PressedBackgroundColor { get; set; } = Color.black;
        public Color ReleasedBackgroundColor { get; set; } = Color.black;
        public Color PressedTextColor { get; set; } = Color.black;
        public Color ReleasedTextColor { get; set; } = Color.black;
    }
}
