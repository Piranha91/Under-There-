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
        public IFormLinkGetter<IArmorGetter> Record { get; set; } = FormLink<IArmorGetter>.Null;

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
        [SynthesisTooltip("Weight of this underwear in-game (if blank, will use the weight of the imported record).")]
        public string Weight { get; set; } = "";

        [SynthesisOrder]
        [SynthesisTooltip("Value of this underwear in-game (if blank, will use the value of the imported record).")]
        public string Value { get; set; } = "";

        [SynthesisOrder]
        [SynthesisTooltip("Armor & Armor Addon Slots to be used by this underwear (if blank, will use the slots of the imported record).")]
        public List<int> Slots { get; set; } = new List<int>();
    }
}
