using JetBrains.Annotations;
using Modding;

namespace ModConsole 
{
    internal class GlobalSettings : ModSettings
    {
        [CanBeNull]
        // ReSharper disable once FieldCanBeMadeReadOnly.Global -- It's serialized.
        public string Font = null;
    }
}