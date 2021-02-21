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
            var validModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "default", "random", "class", "faction" 
            };
            if (!validModes.Contains(settings.AssignmentMode))
            {
                throw new Exception("AssignmentMode can only contain one of the following: default, random, class, faction.");
            }

            // specific NPCs
            foreach (var npc in settings.SpecificNPCs)
            {
                string npcID = npc.Name + " (" + npc + ")";

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
                        if (!bFound)
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Set could not be matched to one of the defined sets.");
                        }
                        break;

                    case "group":
                        if (!settings.Assignments.ContainsKey(npc.Assignment_Group))
                        {
                            throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Group could not be matched to one of the defined Assignments.");
                        }
                        break;
                    default:
                        throw new Exception("Specific NPC Assignment " + npcID + "'s Type must be either \"set\" or \"group\"");
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
            if (settings.AssignmentMode.Equals("class", StringComparison.OrdinalIgnoreCase) && settings.Assignments.Keys.Count - 1 != settings.ClassDefinitions.Keys.Count) // - 1 because Assignments also includes "Default"
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
            if (settings.AssignmentMode.Equals("faction", StringComparison.OrdinalIgnoreCase) && settings.Assignments.Keys.Count - 1 != settings.FactionDefinitions.Keys.Count)
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
            // Assignments - make sure there is a default variant
            if (settings.Assignments["Default"].Count == 0)
            {
                throw new Exception("Assignments: could not find \"Default\" assignment category.");
            }

            // Sets - make sure each set has at least one item for each patchable gender
            foreach (var set in settings.Sets)
            {
                if (settings.PatchMales)
                {
                    if (set.Items_Mutual.Count + set.Items_Male.Count < 1)
                    {
                        throw new Exception("Sets: set \"" + set.Name + "\" must have at least one item in Items_Mutual or Items_Male");
                    }
                }
                if (settings.PatchFemales)
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
