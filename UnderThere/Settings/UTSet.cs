using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public class UTSet
    {
        public string Name { get; set; } = string.Empty;
        public List<UTitem> Items { get; set; } = new List<UTitem>();
        public FormLink<ILeveledItemGetter> LeveledList { get; set; }
    }

    public class UTQualitySet : UTSet
    {
        public Quality Quality;
    }
}
