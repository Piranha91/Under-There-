using System;
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
                // Is this needed?
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
