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
    public class NPCassignment
    {
        [SynthesisOrder]
        public string Name { get; set; } = string.Empty;

        [SynthesisOrder]
        public FormLink<INpcGetter> Record { get; set; }

        [SynthesisOrder]
        public NpcAssignmentType Type { get; set; }

        [SynthesisOrder]
        public string AssignmentSet { get; set; } = string.Empty;

        [SynthesisOrder]
        public string AssignmentGroup { get; set; } = string.Empty;

        [SynthesisIgnoreSetting]
        public UTSet AssignmentSet_Obj { get; set; } = new UTSet();

        [SynthesisIgnoreSetting]
        public bool isNull { get; set; } = true;

        public static NPCassignment getSpecificNPC(FormKey fk, List<NPCassignment> assigments)
        {
            foreach (var assignment in assigments)
            {
                if (assignment.Record.FormKey == fk)
                {
                    return assignment;
                }
            }

            return new NPCassignment();
        }

        public static bool isBlocked(FormKey fk, List<NPCassignment> assigments)
        {
            foreach (var assignment in assigments)
            {
                if (assignment.Record.FormKey == fk)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
