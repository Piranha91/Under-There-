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
    [SynthesisObjectNameMember(nameof(Name))]
    public class UTSet
    {
        [SynthesisOrder]
        public string Category { get; set; } = string.Empty;

        [SynthesisOrder]
        public string Name { get; set; } = string.Empty;

        [SynthesisOrder]
        public List<UTitem> Items { get; set; } = new List<UTitem>();

        [SynthesisIgnoreSetting]
        public FormLink<ILeveledItemGetter> LeveledList { get; set; }
    }
}
