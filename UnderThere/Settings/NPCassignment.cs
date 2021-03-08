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
        [SynthesisTooltip("Name of this NPC (FYI only).")]
        public string Name { get; set; } = string.Empty;

        [SynthesisOrder]
        [SynthesisTooltip("The NPC to whom specific underwear should be assigned.")]
        public FormLink<INpcGetter> Record { get; set; }

        [SynthesisOrder]
        [SynthesisTooltip("Set: apply a specific underwear set to this NPC\nGroup: apply a specific underwear group to this NPC and choose a random underwear set from this group")]
        public NpcAssignmentType Type { get; set; }

        [SynthesisOrder]
        [SynthesisTooltip("(Use if Type = \"Set\"): The name of the underwear set to assign to this NPC.")]
        public string AssignmentSet { get; set; } = string.Empty;

        [SynthesisOrder]
        [SynthesisTooltip("(Use if Type = \"Group\"): The name of the Class or Faction Category to assign to this NPC.")]
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
