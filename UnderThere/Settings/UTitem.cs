using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public class UTitem
    {
        public FormLink<IArmorGetter> Record { get; set; } = FormLink<IArmorGetter>.Null;
        public GenderTarget Gender = GenderTarget.Mutual;
        public string DispName { get; set; } = string.Empty;
        public bool IsBottom { get; set; }
        public float Weight { get; set; } = -1;
        public UInt32 Value { get; set; } = int.MaxValue;
        public List<int> Slots { get; set; } = new List<int>();
    }
}
