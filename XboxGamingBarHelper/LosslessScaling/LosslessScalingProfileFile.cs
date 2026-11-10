using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XboxGamingBarHelper.LosslessScaling
{
    internal sealed class LosslessScalingProfileFile
    {
        private static readonly object SaveLock = new object();
        private readonly string path;
        private readonly byte[] original;

        public LosslessScalingProfileFile(string path)
        {
            this.path = path;
            original = File.ReadAllBytes(path);
        }

        public XDocument Read()
        {
            using (var stream = new MemoryStream(original, writable: false))
                return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }

        public void Save(int profileIndex, IReadOnlyDictionary<string, string> changes)
        {
            var document = Read();
            var profile = document.Element("Settings")?.Element("GameProfiles")?
                .Elements("Profile").ElementAtOrDefault(profileIndex);
            if (profile == null)
                throw new InvalidDataException("The Lossless Scaling profile no longer exists.");

            foreach (var change in changes)
            {
                var field = profile.Element(change.Key);
                if (field == null || field.HasElements || change.Key == "Title" || change.Key == "Path")
                    throw new InvalidDataException($"Unsupported Lossless Scaling profile field: {change.Key}.");
                field.Value = change.Value;
            }

            string staged = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    document.Save(stream, SaveOptions.DisableFormatting);

                lock (SaveLock)
                {
                    // The caller must close Lossless Scaling before saving. This also catches edits made since loading.
                    if (!File.ReadAllBytes(path).SequenceEqual(original))
                        throw new IOException("Lossless Scaling settings changed. Reload the profiles before saving.");

                    File.Replace(staged, path, path + ".gotweaks.bak");
                }
            }
            finally
            {
                File.Delete(staged);
            }
        }
    }
}
