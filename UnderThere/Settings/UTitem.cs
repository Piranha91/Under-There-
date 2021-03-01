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
    public class UTitem
    {
        [SynthesisOrder]
        public FormLink<IArmorGetter> Record { get; set; } = FormLink<IArmorGetter>.Null;

        [SynthesisOrder]
        public GenderTarget Gender = GenderTarget.Mutual;

        [SynthesisOrder]
        public string DispName { get; set; } = string.Empty;

        [SynthesisOrder]
        public bool IsBottom { get; set; }

        [SynthesisOrder]
        public float Weight { get; set; } = 0.5f;

        [SynthesisOrder]
        public UInt32 Value { get; set; } = 25;

        [SynthesisOrder]
        public List<int> Slots { get; set; } = new List<int>();
    }
}
