using System;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Base class for all tweaks.
    /// </summary>
    public abstract class Tweak
    {
        /// <summary>
        /// The name of the tweak.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The description of the tweak.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Called when the tweak is enabled.
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the tweak is disabled.
        /// </summary>
        public virtual void OnDisable() { }

        /// <summary>
        /// Called every frame to update the tweak.
        /// </summary>
        /// <param name="deltaTime">Time since last frame.</param>
        public virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// Called when the settings GUI should be drawn.
        /// </summary>
        public virtual void OnSettingsGUI() { }

        /// <summary>
        /// Called when the GUI is hidden.
        /// </summary>
        public virtual void OnHideGUI() { }
    }
}
