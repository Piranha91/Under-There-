﻿using System;
using System.Collections.Generic;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
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
                string npcID = npc.Name + " (" + npc + ")";

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
                        if (!bFound)
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Set could not be matched to one of the defined sets.");
                        }
                        break;

                    case NpcAssignmentType.Group:
                        if (!settings.Assignments.ContainsKey(npc.Assignment_Group))
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Group could not be matched to one of the defined Assignments.");
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                npc.isNull = false;
            }

            // Class Definitions - make sure each is matched to an assignment
            foreach (var def in settings.ClassDefinitions.Keys)
            {
                if (!settings.Assignments.ContainsKey(def))
                {
                    throw new Exception("ClassDefinitions: Definition " + def + " was not found in Assignments.");
                }
            }
            if (settings.AssignmentMode == AssignmentMode.Class && settings.Assignments.Keys.Count - 1 != settings.ClassDefinitions.Keys.Count) // - 1 because Assignments also includes "Default"
            {
                throw new Exception("ClassDefinitions: All definitions must have a corresponding Assignment and vice-versa");
            }

            // Faction definitions - make sure each is matched to an assignment
            foreach (var def in settings.FactionDefinitions.Keys)
            {
                if (!settings.Assignments.ContainsKey(def))
                {
                    throw new Exception("FactionDefinitions: Definition " + def + " was not found in Assignments.");
                }
            }
            if (settings.AssignmentMode == AssignmentMode.Faction && settings.Assignments.Keys.Count - 1 != settings.FactionDefinitions.Keys.Count)
            {
                throw new Exception("FactionDefinitions: All definitions must have a corresponding Assignment and vice-versa");
            }

            // FallBackFaction definitions - make sure each is matched to an assignment
            foreach (var def in settings.FallBackFactionDefinitions.Keys)
            {
                if (!settings.Assignments.ContainsKey(def))
                {
                    throw new Exception("FallBackFactionDefinitions: Definition " + def + " was not found in Assignments.");
                }
                if (!settings.FactionDefinitions.ContainsKey(def))
                {
                    throw new Exception("FallBackFactionDefinitions: Definition " + def + " was not found in FactionDefinitions.");
                }
            }

            // Assignments - make sure each inventory item is found
            foreach (var assignedGroup in settings.Assignments.Values)
            {
                foreach(string variant in assignedGroup)
                {
                    bool bFound = false;
                    foreach (var set in settings.Sets)
                    {
                        if (set.Name == variant)
                        {
                            bFound = true; break;
                        }
                    }
                    if (!bFound)
                    {
                        throw new Exception("Assignments: Could not find set \"" + variant + "\" in Sets");
                    }
                }
            }

            // Sets - make sure each set has at least one item for each patchable gender
            foreach (var set in settings.Sets)
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
