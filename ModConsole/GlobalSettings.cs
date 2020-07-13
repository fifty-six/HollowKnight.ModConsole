using System;
using JetBrains.Annotations;
using Modding;

namespace ModConsole 
{
    [Serializable]
    internal class GlobalSettings : ModSettings
    {
        [CanBeNull]
        public string Font = string.Empty;
    }
}