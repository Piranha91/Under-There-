using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Mutagen.Bethesda.Skyrim;

namespace UnderThere.Settings
{
    class Validator
    {
        public static void validateSettings(UTconfig settings)
        {
            // specific NPCs
            foreach (var npc in settings.SpecificNpcs)
            {
                string npcID = npc.Name + " (" + npc.Record.FormKey + ")";

                npc.Type = npc.Type;
                switch (npc.Type)
                {
                    case NpcAssignmentType.Set:
                        bool bFound = false;
                        foreach (UTSet set in settings.Sets)
                        {
                            if (set.Name == npc.AssignmentSet)
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

            // Sets - make sure the default set has at least one item for each patchable gender
            if (settings.PatchMales && !settings.DefaultSet.Items.Any(i => i.Gender == GenderTarget.Male || i.Gender == GenderTarget.Mutual))
            {
                throw new Exception("The Default Set must have at least one item in Items_Mutual or Items_Male if Patch Males is enabled.");
            }
            if (settings.PatchFemales && !settings.DefaultSet.Items.Any(i => i.Gender == GenderTarget.Female || i.Gender == GenderTarget.Mutual))
            {
                throw new Exception("The Default Set must have at least one item in Items_Mutual or Items_Female if Patch Females is enabled.");
            }
        }

        public static void validateGenderedSets(UTconfig settings, Dictionary<GenderTarget, Dictionary<string, LeveledItem>> lItemsByGenderAndWealth)
        {
            if (settings.PatchMales)
            {
                if (lItemsByGenderAndWealth[GenderTarget.Male] != null)
                {
                    foreach (var category in lItemsByGenderAndWealth[GenderTarget.Male].Keys)
                    {
                        if (lItemsByGenderAndWealth[GenderTarget.Male][category] == null)
                        {
                            throw new Exception("Patch Males is enabled but a male-specific leveled list could not be generated for category: " + category);
                        }
                        else
                        {
                            var currentEntries = lItemsByGenderAndWealth[GenderTarget.Male][category].Entries;
                            if (currentEntries == null || !currentEntries.Any())
                            {
                                throw new Exception("Patch Males is enabled but no male undergarments were found for category: " + category);
                            }
                        }
                    }
                }
            }

            if (settings.PatchFemales)
            {
                if (lItemsByGenderAndWealth[GenderTarget.Female] != null)
                {
                    foreach (var category in lItemsByGenderAndWealth[GenderTarget.Female].Keys)
                    {
                        if (lItemsByGenderAndWealth[GenderTarget.Female][category] == null)
                        {
                            throw new Exception("Patch Females is enabled but a female-specific leveled list could not be generated for category: " + category);
                        }
                        else
                        {
                            var currentEntries = lItemsByGenderAndWealth[GenderTarget.Female][category].Entries;
                            if (currentEntries == null || !currentEntries.Any())
                            {
                                throw new Exception("Patch Females is enabled but no female undergarments were found for category: " + category);
                            }
                        }
                    }
                }
            }
        }
    }
}
