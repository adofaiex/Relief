using System;

namespace SRP.ADOFAI.Keyviewer.Core.Attributes
{
    /// <summary>
    /// Attribute to register a tweak with the tweak system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterTweakAttribute : Attribute
    {
        public string Id { get; }
        public Type SettingsType { get; }
        public Type PatchesType { get; }

        public RegisterTweakAttribute(string id, Type settingsType, Type patchesType)
        {
            Id = id;
            SettingsType = settingsType;
            PatchesType = patchesType;
        }
    }
}
