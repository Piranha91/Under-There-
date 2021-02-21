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
        public List<UTitem> Items_Mutual { get; set; } = new List<UTitem>();
        public List<UTitem> Items_Male { get; set; } = new List<UTitem>();
        public List<UTitem> Items_Female { get; set; } = new List<UTitem>();
        public FormLink<ILeveledItemGetter> LeveledList { get; set; }
    }
}
