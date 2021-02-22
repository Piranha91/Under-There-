using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public class NPCassignment
    {
        public string Name { get; set; } = string.Empty;
        public FormLink<INpcGetter> Record { get; set; }
        public NpcAssignmentType Type { get; set; }
        public AssignmentQuality Assignment_Set { get; set; }
        public AssignmentQuality Assignment_Group { get; set; }
        public UTSet AssignmentSet_Obj { get; set; } = new UTSet();
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
