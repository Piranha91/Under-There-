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
        [SynthesisTooltip("Name of this underwear set in patcher menu (not shown in-game).")]
        public string Name { get; set; } = string.Empty;

        [SynthesisOrder]
        [SynthesisTooltip("Underwear items to include with this underwear set. Must have at least one item for both male and female NPCs.")]
        public List<UTitem> Items { get; set; } = new List<UTitem>();

        [SynthesisIgnoreSetting]
        public FormLink<ILeveledItemGetter> LeveledList { get; set; } = new FormLink<ILeveledItemGetter>();
    }

    [SynthesisObjectNameMember(nameof(Category))]
    [SynthesisObjectNameMember(nameof(Name))]
    public class UTCategorySet : UTSet
    {
        [SynthesisOrder]
        [SynthesisTooltip("Class or Faction category to which this set should be assigned.")]
        public string Category { get; set; } = string.Empty;
    }
}
