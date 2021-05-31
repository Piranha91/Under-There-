using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using UnderThere.Settings;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Internals;
using Mutagen.Bethesda.Plugins.Records;

namespace UnderThere
{
    class Auxil
    {
        public static bool isNonHumanoid(INpcGetter npc, IRaceGetter npcRace, ILinkCache lk)
        {
            List<string> nonHumanoidFactions = new List<string> { "CreatureFaction", "PreyFaction", "DragonFaction", "DwarvenAutomatonFaction", "DLC2ExpSpiderFriendFaction" };
            List<string> nonHumanoidAttackRaces = new List<string> { "DwarvenSphereRace", "DwarvenSpiderRace" };
            foreach (var fact in npc.Factions)
            {
                if (!lk.TryResolve<IFactionGetter>(fact.Faction.FormKey, out var currentFaction) || currentFaction.EditorID == null) { continue; }
                if (nonHumanoidFactions.Contains(currentFaction.EditorID))
                {
                    return true;
                }
            }

            if (npcRace.EditorID != null && npcRace.EditorID.Contains("atronach", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lk.TryResolve<IRaceGetter>(npc.AttackRace.FormKey, out var currentAttackRace) && currentAttackRace.EditorID != null)
            {
                if (nonHumanoidAttackRaces.Contains(currentAttackRace.EditorID))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool hasGhostAbility(INpcGetter npc)
        {
            if (npc.ActorEffect == null) return false;
            foreach (var ability in npc.ActorEffect)
            {
                if (ability.Equals(Skyrim.Spell.GhostAbility))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool hasGhostScript(INpcGetter npc)
        {
            if (npc.VirtualMachineAdapter == null) return false;
            foreach (var script in npc.VirtualMachineAdapter.Scripts)
            {
                if (script.Name == "defaultGhostScript")
                {
                    return true;
                }
            }
            return false;
        }

        public static List<BipedObjectFlag> getItemSetARMAslots(IEnumerable<UTSet> sets, ILinkCache lk)
        {
            List<BipedObjectFlag> usedSlots = new List<BipedObjectFlag>();

            foreach (UTSet set in sets)
            {
                getContainedSlots(set.Items, usedSlots, lk);
            }

            return usedSlots;
        }

        public static void getContainedSlots(List<UTitem> items, List<BipedObjectFlag> usedSlots, ILinkCache lk)
        {
            foreach (UTitem item in items)
            {
                if (!item.Record.TryResolve(lk, out var itemObj))
                {
                    continue;
                }

                foreach (IFormLink<IArmorAddonGetter> AAgetter in itemObj.Armature)
                {
                    if (!lk.TryResolve<IArmorAddon>(AAgetter.FormKey, out var ARMAobj) || ARMAobj.BodyTemplate == null)
                    {
                        continue;
                    }

                    List<BipedObjectFlag> currentUsedSlots = getARMAslots(ARMAobj.BodyTemplate);
                    foreach (var usedFlag in currentUsedSlots)
                    {
                        if (!usedSlots.Contains(usedFlag))
                        {
                            usedSlots.Add(usedFlag);
                        }
                    }
                }
            }
        }

        public static List<BipedObjectFlag> getARMAslots(BodyTemplate bodyTemplate)
        {
            List<BipedObjectFlag> usedSlots = new List<BipedObjectFlag>();
            List<BipedObjectFlag> possibleSlots = new List<BipedObjectFlag>
            {
                (BipedObjectFlag)0x00000001,
                (BipedObjectFlag)0x00000002,
                (BipedObjectFlag)0x00000004,
                (BipedObjectFlag)0x00000008,
                (BipedObjectFlag)0x00000010,
                (BipedObjectFlag)0x00000020,
                (BipedObjectFlag)0x00000040,
                (BipedObjectFlag)0x00000080,
                (BipedObjectFlag)0x00000100,
                (BipedObjectFlag)0x00000200,
                (BipedObjectFlag)0x00000400,
                (BipedObjectFlag)0x00000800,
                (BipedObjectFlag)0x00001000,
                (BipedObjectFlag)0x00002000,
                (BipedObjectFlag)0x00004000,
                (BipedObjectFlag)0x00008000,
                (BipedObjectFlag)0x00010000,
                (BipedObjectFlag)0x00020000,
                (BipedObjectFlag)0x00040000,
                (BipedObjectFlag)0x00080000,
                (BipedObjectFlag)0x00100000,
                (BipedObjectFlag)0x00200000,
                (BipedObjectFlag)0x00400000,
                (BipedObjectFlag)0x00800000,
                (BipedObjectFlag)0x01000000,
                (BipedObjectFlag)0x02000000,
                (BipedObjectFlag)0x04000000,
                (BipedObjectFlag)0x08000000,
                (BipedObjectFlag)0x10000000,
                (BipedObjectFlag)0x20000000
            };

            foreach (BipedObjectFlag flag in possibleSlots)
            {
                if (bodyTemplate.FirstPersonFlags.HasFlag(flag))
                {
                    usedSlots.Add(flag);
                }
            }

            return usedSlots;
        }

        public static BipedObjectFlag mapIntToSlot(int iFlag)
        {
            switch (iFlag)
            {
                case 30: return (BipedObjectFlag)0x00000001;
                case 31: return (BipedObjectFlag)0x00000002;
                case 32: return (BipedObjectFlag)0x00000004;
                case 33: return (BipedObjectFlag)0x00000008;
                case 34: return (BipedObjectFlag)0x00000010;
                case 35: return (BipedObjectFlag)0x00000020;
                case 36: return (BipedObjectFlag)0x00000040;
                case 37: return (BipedObjectFlag)0x00000080;
                case 38: return (BipedObjectFlag)0x00000100;
                case 39: return (BipedObjectFlag)0x00000200;
                case 40: return (BipedObjectFlag)0x00000400;
                case 41: return (BipedObjectFlag)0x00000800;
                case 42: return (BipedObjectFlag)0x00001000;
                case 43: return (BipedObjectFlag)0x00002000;
                case 44: return (BipedObjectFlag)0x00004000;
                case 45: return (BipedObjectFlag)0x00008000;
                case 46: return (BipedObjectFlag)0x00010000;
                case 47: return (BipedObjectFlag)0x00020000;
                case 48: return (BipedObjectFlag)0x00040000;
                case 49: return (BipedObjectFlag)0x00080000;
                case 50: return (BipedObjectFlag)0x00100000;
                case 51: return (BipedObjectFlag)0x00200000;
                case 52: return (BipedObjectFlag)0x00400000;
                case 53: return (BipedObjectFlag)0x00800000;
                case 54: return (BipedObjectFlag)0x01000000;
                case 55: return (BipedObjectFlag)0x02000000;
                case 56: return (BipedObjectFlag)0x04000000;
                case 57: return (BipedObjectFlag)0x08000000;
                case 58: return (BipedObjectFlag)0x10000000;
                case 59: return (BipedObjectFlag)0x20000000;
                default: throw new Exception(iFlag + " is not a valid armor slot.");
            }
        }

        public static int mapSlotToInt(BipedObjectFlag oFlag)
        {
            switch (oFlag)
            {
                case (BipedObjectFlag)0x00000001: return 30;
                case (BipedObjectFlag)0x00000002: return 31;
                case (BipedObjectFlag)0x00000004: return 32;
                case (BipedObjectFlag)0x00000008: return 33;
                case (BipedObjectFlag)0x00000010: return 34;
                case (BipedObjectFlag)0x00000020: return 35;
                case (BipedObjectFlag)0x00000040: return 36;
                case (BipedObjectFlag)0x00000080: return 37;
                case (BipedObjectFlag)0x00000100: return 38;
                case (BipedObjectFlag)0x00000200: return 39;
                case (BipedObjectFlag)0x00000400: return 40;
                case (BipedObjectFlag)0x00000800: return 41;
                case (BipedObjectFlag)0x00001000: return 42;
                case (BipedObjectFlag)0x00002000: return 43;
                case (BipedObjectFlag)0x00004000: return 44;
                case (BipedObjectFlag)0x00008000: return 45;
                case (BipedObjectFlag)0x00010000: return 46;
                case (BipedObjectFlag)0x00020000: return 47;
                case (BipedObjectFlag)0x00040000: return 48;
                case (BipedObjectFlag)0x00080000: return 49;
                case (BipedObjectFlag)0x00100000: return 50;
                case (BipedObjectFlag)0x00200000: return 51;
                case (BipedObjectFlag)0x00400000: return 52;
                case (BipedObjectFlag)0x00800000: return 53;
                case (BipedObjectFlag)0x01000000: return 54;
                case (BipedObjectFlag)0x02000000: return 55;
                case (BipedObjectFlag)0x04000000: return 56;
                case (BipedObjectFlag)0x08000000: return 57;
                case (BipedObjectFlag)0x10000000: return 58;
                case (BipedObjectFlag)0x20000000: return 59;
                default: return 0;
            }
        }

        public static void LogDefaultNPCs(List<string> failedNPClookups, HashSet<IFormLinkGetter> failedGroupLookups, string extraSettingsPath, string fallBackQuality)
        {
            string logPath = Path.Combine(extraSettingsPath, "failedAssignmentLog.txt");
            List<string> logLines = new List<string>();

            Console.WriteLine("");
            if (failedNPClookups.Count > 0)
            {
                Console.WriteLine(failedNPClookups.Count + " NPCs were assigned to the " + fallBackQuality + " group because their definitions could not be matched to any custom groups.");
                logLines.Add("The following NPCs were assigned to the Default group:");
                logLines.AddRange(failedNPClookups);
                logLines.Add("=================================================================================================");
            }
            if (failedGroupLookups.Count > 0)
            {
                Console.WriteLine(failedGroupLookups.Count + " classifiers could not be matched to any group definitions defined in the settings file.");
                logLines.Add("The following classifiers could not be matched to any group definitions within the settings file.");
                logLines.AddRange(failedGroupLookups.Select(x => x.ToString() ?? string.Empty));
            }
            if (failedGroupLookups.Count > 0 || failedNPClookups.Count > 0)
            {
                Console.WriteLine("See the log for details: " + logPath);
            }
            
            try
            {
                File.WriteAllLines(logPath, logLines);
            }

            catch
            {
                throw new Exception("Could not write " + logPath);
            }
        }

        public static bool hasBlockedFaction(INpcGetter npc, HashSet<IFormLinkGetter<IFactionGetter>> BlockedFactions)
        {
            foreach (var fact in npc.Factions)
            {
                if (BlockedFactions.Contains(fact.Faction) && fact.Rank != 255) // 255 shows up as -1 in SSEedit
                {
                    return true;
                }
            }
            return false;
        }
    }
    public static class DuplicateFromMixInB
    {
        /// <summary>
        /// Duplicates records into a given mod 'modToDuplicateInto', which originated from target ModKey 'modKeyToDuplicateFrom'.<br />
        /// Only considers records that are currently within the target modToDuplicateInto, which are then duplicated. <br/>
        /// Records from the modKeyToDuplicateFrom that are not within or referenced by records in the target mod are skipped.<br />
        /// <br />
        /// End result will be that all records that the given modToDuplicateInto contains or references that originate from the target modKeyToDuplicateFrom will be duplicated in
        /// and replace the records they duplicated.  No references to the modKeyToDuplicateFrom will remain.
        /// </summary>
        /// <typeparam name="TMod"></typeparam>
        /// <typeparam name="TModGetter"></typeparam>
        /// <param name="modToDuplicateInto">Mod to duplicate into and originate new records from</param>
        /// <param name="linkCache">LinkCache for lookup</param>
        /// <param name="modKeyToDuplicateFrom">ModKey to search modToDuplicateInto for, and duplicate records that originate from modKeyToDuplicateFrom</param>
        /// <param name="typesToInspect">
        /// Types to iterate and look at within modToDuplicateInto for references to modKeyToDuplicateFrom<br />
        /// Only use if you know specifically the types that can reference modKeyToDuplicateFrom, and want a little bit of speed
        /// by not checking uninteresting records.
        /// </param>
        public static void DuplicateFromOnlyReferencedB<TMod, TModGetter>(
            this TMod modToDuplicateInto,
            ILinkCache<TMod, TModGetter> linkCache,
            ModKey modKeyToDuplicateFrom,
            params Type[] typesToInspect)
            where TModGetter : class, IModGetter
            where TMod : class, TModGetter, IMod
        {
            DuplicateFromOnlyReferencedB(
                modToDuplicateInto,
                linkCache,
                modKeyToDuplicateFrom,
                out _,
                typesToInspect);
        }

        /// <summary>
        /// Duplicates records into a given mod 'modToDuplicateInto', which originated from target ModKey 'modKeyToDuplicateFrom'.<br />
        /// Only considers records that are currently within the target modToDuplicateInto, which are then duplicated. <br/>
        /// Records from the modKeyToDuplicateFrom that are not within or referenced by records in the target mod are skipped.<br />
        /// <br />
        /// End result will be that all records that the given modToDuplicateInto contains or references that originate from the target modKeyToDuplicateFrom will be duplicated in
        /// and replace the records they duplicated.  No references to the modKeyToDuplicateFrom will remain.
        /// </summary>
        /// <typeparam name="TMod"></typeparam>
        /// <typeparam name="TModGetter"></typeparam>
        /// <param name="modToDuplicateInto">Mod to duplicate into and originate new records from</param>
        /// <param name="linkCache">LinkCache for lookup</param>
        /// <param name="modKeyToDuplicateFrom">ModKey to search modToDuplicateInto for, and duplicate records that originate from modKeyToDuplicateFrom</param>
        /// <param name="mapping">Out parameter to store the resulting duplication mappings that were made</param>
        /// <param name="typesToInspect">
        /// Types to iterate and look at within modToDuplicateInto for references to modKeyToDuplicateFrom<br />
        /// Only use if you know specifically the types that can reference modKeyToDuplicateFrom, and want a little bit of speed
        /// by not checking uninteresting records.
        /// </param>
        public static void DuplicateFromOnlyReferencedB<TMod, TModGetter>(
            this TMod modToDuplicateInto,
            ILinkCache<TMod, TModGetter> linkCache,
            ModKey modKeyToDuplicateFrom,
            out Dictionary<FormKey, FormKey> mapping,
            params Type[] typesToInspect)
            where TModGetter : class, IModGetter
            where TMod : class, TModGetter, IMod
        {
            if (modKeyToDuplicateFrom == modToDuplicateInto.ModKey)
            {
                throw new ArgumentException("Cannot pass the target mod's Key as the one to extract and self contain");
            }

            // Compile list of things to duplicate
            HashSet<IFormLinkGetter> identifiedLinks = new();
            HashSet<FormKey> passedLinks = new();

            void AddAllLinks(IFormLinkGetter link)
            {
                if (link.FormKey.IsNull || !passedLinks.Add(link.FormKey)) return;

                if (link.FormKey.ModKey == modKeyToDuplicateFrom)
                {
                    identifiedLinks.Add(link);
                }

                if (!linkCache.TryResolve(link.FormKey, link.Type, out var linkRec))
                {
                    throw new KeyNotFoundException($"Could not locate record to make self contained: {link.FormKey} of type: {link.Type}");
                }

                foreach (var containedLink in linkRec.ContainedFormLinks)
                {
                    AddAllLinks(containedLink);
                }
            }

            var enumer = typesToInspect == null || typesToInspect.Length == 0
                ? modToDuplicateInto.EnumerateMajorRecords()
                : typesToInspect.SelectMany(x => modToDuplicateInto.EnumerateMajorRecords(x));
            foreach (var rec in enumer)
            {
                AddAllLinks(new FormLinkInformation(rec.FormKey, rec.Registration.GetterType));
            }

            // Duplicate in the records
            mapping = new();
            foreach (var identifiedRec in identifiedLinks)
            {
                if (!linkCache.TryResolveContext(identifiedRec.FormKey, identifiedRec.Type, out var rec))
                {
                    throw new KeyNotFoundException($"Could not locate record to make self contained: {identifiedRec.FormKey} of type: {identifiedRec.Type}");
                }

                var dup = rec.DuplicateIntoAsNewRecord(modToDuplicateInto, rec.Record.EditorID);
                mapping[rec.Record.FormKey] = dup.FormKey;

                // ToDo
                // Move this out of loop, and remove off a new IEnumerable<FormLinkInformation> call
                modToDuplicateInto.Remove(identifiedRec.FormKey, identifiedRec.Type);
            }

            // Remap links
            modToDuplicateInto.RemapLinks(mapping);
        }
    }
}
