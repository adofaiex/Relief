using System.IO;
using System.Xml.Serialization;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Base class for tweak settings profiles.
    /// </summary>
    public abstract class TweakSettingsProfile
    {
        /// <summary>
        /// Creates a copy of this profile.
        /// </summary>
        /// <typeparam name="T">The type of the profile.</typeparam>
        /// <returns>A copy of this profile.</returns>
        public T Copy<T>() where T : TweakSettingsProfile
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, this);
                using (StringReader reader = new StringReader(writer.ToString()))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
        }
    }
}
