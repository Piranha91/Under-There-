using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UnderThere.Settings
{
    class Validator
    {
        public static void validateSettings(UTconfig settings)
        {
            // specific NPCs
            foreach (var npc in settings.SpecificNPCs)
            {
                string npcID = npc.Name + " (" + npc.Record.FormKey + ")";

                npc.Type = npc.Type;
                switch (npc.Type)
                {
                    case NpcAssignmentType.Set:
                        bool bFound = false;
                        foreach (UTSet set in settings.Sets)
                        {
                            if (set.Name == npc.Assignment_Set)
                            {
                                bFound = true;
                                npc.AssignmentSet_Obj = set;
                                break;
                            }
                        }
                        if (bFound == false)
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Set could not be matched to one of the defined sets.");
                        }
                        break;

                    case NpcAssignmentType.Group:
                        break;
                    default:
                        throw new Exception("Specific NPC Assignment " + npcID + "'s Type must be either \"set\" or \"group\"");
                }

                npc.isNull = false;
            }

            // FallBackFaction definitions - make sure each is matched to an assignment
            foreach (var def in settings.FallBackFactionDefinitions.Keys)
            {
                if (!settings.FactionDefinitions.ContainsKey(def))
                {
                    throw new Exception("FallBackFactionDefinitions: Definition " + def + " was not found in FactionDefinitions.");
                }
            }

            // Sets - make sure each set has at least one item for each patchable gender
            foreach (var set in settings.AllSets)
            {
                if (settings.PatchMales)
                {
                    if (!set.Items.Any(i => i.Gender == GenderTarget.Male || i.Gender == GenderTarget.Mutual))
                    {
                        throw new Exception("Sets: set \"" + set.Name + "\" must have at least one item in Items_Mutual or Items_Male");
                    }
                }
                if (settings.PatchFemales)
                {
                    if (!set.Items.Any(i => i.Gender == GenderTarget.Female || i.Gender == GenderTarget.Mutual))
                    {
                        throw new Exception("Sets: set \"" + set.Name + "\" must have at least one item in Items_Mutual or Items_Female");
                    }
                }
            }
        }
    }
}
