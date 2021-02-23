using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public class UTSet
    {
        [SynthesisOrder(0)]
        public string Name { get; set; } = string.Empty;

        [SynthesisOrder(1)]
        public List<UTitem> Items { get; set; } = new List<UTitem>();

        [SynthesisIgnoreSetting]
        public FormLink<ILeveledItemGetter> LeveledList { get; set; }
    }

    public class UTQualitySet : UTSet
    {
        [SynthesisOrder(2)]
        public Quality Quality;
    }
}
