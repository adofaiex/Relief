using System;

namespace SRP.ADOFAI.Keyviewer.Core.Attributes
{
    /// <summary>
    /// Attribute to automatically sync tweak settings to a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SyncTweakSettingsAttribute : Attribute
    {
    }
}
