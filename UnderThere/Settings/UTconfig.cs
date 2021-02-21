using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public class UTconfig
    {
        public bool VerboseMode { get; set; }
        public string AssignmentMode { get; set; } = string.Empty;
        public bool PatchMales { get; set; } = true;
        public bool PatchFemales { get; set; } = true;
        public bool PatchNakedNPCs { get; set; } = true;
        public bool PatchSummonedNPCs { get; set; }
        public bool PatchGhosts { get; set; } = true;
        public bool MakeItemsEquippable { get; set; }
        public HashSet<FormLink<IRaceGetter>> PatchableRaces { get; set; } = new HashSet<FormLink<IRaceGetter>>();
        public HashSet<FormLink<IRaceGetter>> NonPatchableRaces { get; set; } = new HashSet<FormLink<IRaceGetter>>();
        public Dictionary<string, List<string>> ClassDefinitions { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> FactionDefinitions { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> FallBackFactionDefinitions { get; set; } = new Dictionary<string, List<string>>();
        public HashSet<FormLink<IFactionGetter>> IgnoreFactionsWhenScoring { get; set; } = new HashSet<FormLink<IFactionGetter>>();
        public List<NPCassignment> SpecificNPCs { get; set; } = new List<NPCassignment>();
        public List<NPCassignment> BlockedNPCs { get; set; } = new List<NPCassignment>();
        public Dictionary<string, List<string>> Assignments { get; set; } = new Dictionary<string, List<string>>();
        public List<UTSet> Sets { get; set; } = new List<UTSet>();
    }
}
