using System;
using System.Collections.Generic;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;

namespace UnderThere
{
    class Validator
    {
        public static void validateSettings(UTconfig settings)
        {
            // assignment mode
            settings.AssignmentMode = settings.AssignmentMode.ToLower();
            List<string> validModes = new List<string> { "default", "random", "class", "faction" };
            if (validModes.Contains(settings.AssignmentMode) == false)
            {
                throw new Exception("AssignmentMode can only contain one of the following: default, random, class, faction.");
            }

            // specific NPCs
            foreach (var npc in settings.SpecificNPCs)
            {
                if (!FormKey.TryFactory(npc.FormKey, out var currentFK) || currentFK == null)
                {
                    throw new Exception("Specific NPC List: Could not resolve formkey " + npc.FormKey + " for NPC " + npc.Name);
                }
                else
                {
                    npc.FormKeyObj = currentFK;
                }

                string npcID = npc.Name + " (" + npc.FormKey + ")";

                npc.Type = npc.Type.ToLower();
                switch (npc.Type)
                {
                    case "set":
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

                    case "group":
                        if (settings.Assignments.ContainsKey(npc.Assignment_Group) == false)
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Group could not be matched to one of the defined Assignments.");
                        }
                        break;
                    default:
                        throw new Exception("Specific NPC Assignment " + npcID + "'s Type must be either \"set\" or \"group\"");
                }

                npc.isNull = false;
            }

            // blocked NPCs
            foreach (var npc in settings.BlockedNPCs)
            {
                if (!FormKey.TryFactory(npc.FormKey, out var currentFK) || currentFK == null)
                {
                    throw new Exception("Blocked NPC List: Could not resolve formkey " + npc.FormKey + " for NPC " + npc.Name);
                }
            }

            // Class Definitions - make sure each is matched to an assignment
            foreach (var def in settings.ClassDefinitions.Keys)
            {
                if (settings.Assignments.ContainsKey(def) == false)
                {
                    throw new Exception("ClassDefinitions: Definition " + def + " was not found in Assignments.");
                }
            }
            if (settings.AssignmentMode == "class" && settings.Assignments.Keys.Count - 1 != settings.ClassDefinitions.Keys.Count) // - 1 because Assignments also includes "Default"
            {
                throw new Exception("ClassDefinitions: All definitions must have a corresponding Assignment and vice-versa");
            }

            // Faction definitions - make sure each is matched to an assignment
            foreach (var def in settings.FactionDefinitions.Keys)
            {
                if (settings.Assignments.ContainsKey(def) == false)
                {
                    throw new Exception("FactionDefinitions: Definition " + def + " was not found in Assignments.");
                }
            }
            if (settings.AssignmentMode == "faction" && settings.Assignments.Keys.Count - 1 != settings.FactionDefinitions.Keys.Count)
            {
                throw new Exception("FactionDefinitions: All definitions must have a corresponding Assignment and vice-versa");
            }

            // FallBackFaction definitions - make sure each is matched to an assignment
            foreach (var def in settings.FallBackFactionDefinitions.Keys)
            {
                if (settings.Assignments.ContainsKey(def) == false)
                {
                    throw new Exception("FallBackFactionDefinitions: Definition " + def + " was not found in Assignments.");
                }
                if (settings.FactionDefinitions.ContainsKey(def) == false)
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
                    if (bFound == false)
                    {
                        throw new Exception("Assignments: Could not find set \"" + variant + "\" in Sets");
                    }
                }
            }
            // Assignments - make sure there is a default variant
            if (settings.Assignments["Default"].Count == 0)
            {
                throw new Exception("Assignments: could not find \"Default\" assignment category.");
            }

            // Sets - make sure each set has at least one item for each patchable gender
            foreach (var set in settings.Sets)
            {
                if(settings.bPatchMales == true)
                {
                    if (set.Items_Mutual.Count + set.Items_Male.Count < 1)
                    {
                        throw new Exception("Sets: set \"" + set.Name + "\" must have at least one item in Items_Mutual or Items_Male");
                    }
                }
                if (settings.bPatchFemales == true)
                {
                    if (set.Items_Mutual.Count + set.Items_Female.Count < 1)
                    {
                        throw new Exception("Sets: set \"" + set.Name + "\" must have at least one item in Items_Mutual or Items_Female");
                    }
                }
            }
        }
    }
}
