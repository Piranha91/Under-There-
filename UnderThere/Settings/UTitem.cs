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
    [SynthesisObjectNameMember(nameof(DispName))]
    [SynthesisObjectNameMember(nameof(Record))]
    public class UTitem
    {
        [SynthesisOrder]
        [SynthesisTooltip("Armor record to be imported as underwear.")]
        public FormLink<IArmorGetter> Record { get; set; } = FormLink<IArmorGetter>.Null;

        [SynthesisOrder]
        [SynthesisTooltip("Gender to which this underwear is to be assigned.")]
        public GenderTarget Gender = GenderTarget.Mutual;

        [SynthesisOrder]
        [SynthesisTooltip("If checked, this item will be recognized as an underpant (for SOS compatibility).")]
        public bool IsBottom { get; set; }

        [SynthesisOrder]
        [SynthesisTooltip("Name of this underwear in-game.")]
        public string DispName { get; set; } = string.Empty;

        [SynthesisOrder]
        [SynthesisTooltip("Weight of this underwear in-game.")]
        public float Weight { get; set; } = 0.5f;

        [SynthesisOrder]
        [SynthesisTooltip("Value of this underwear in-game.")]
        public UInt32 Value { get; set; } = 25;

        [SynthesisOrder]
        [SynthesisTooltip("Armor & Armor Addon Slots to be used by this underwear (if empty, will keep the ones from the source mod).")]
        public List<int> Slots { get; set; } = new List<int>();
    }
}
