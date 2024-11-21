using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using UnderThere.Settings;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Order;

namespace UnderThere
{
    public class Program
    {
        public const string Default = "Default";
        static Lazy<UTconfig> Settings = null!;
        static Lazy<Random> Random = new Lazy<Random>(() => new Random(Settings.Value.RandomSeed));

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .AddRunnabilityCheck(CanRunPatch)
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "UnderThere.esp")
                .Run(args);
        }

        private static void CanRunPatch(IRunnabilityState state)
        {
            UTconfig settings = Settings.Value;

            settings.AllSets = settings.Sets.And(settings.DefaultSet).ToHashSet();

            foreach (var set in settings.AllSets)
            {
                foreach (var item in set.Items)
                { 
                    if (!state.LoadOrder.ContainsKey(item.Record.FormKey.ModKey))
                    {
                        string exceptionStr = "Plugin " + item.Record.FormKey.ModKey + " expected by Set " + set.Name + "from item " + item.DispName + " (" + item.Record.FormKey + ") is not currently in your load order.";
                        exceptionStr += Environment.NewLine + "Current Load Order:" + Environment.NewLine + string.Join(Environment.NewLine, state.LoadOrder.Select(x => x.Value.FileName));
                        throw new Exception(exceptionStr);
                    }
                }
            }

            var envState = state.GetEnvironmentState<ISkyrimMod, ISkyrimModGetter>();
            if (envState == null)
            {
                throw new Exception("Could not create environment state");
            }
            if (envState!= null && UserHasSOS(state))
            {
                CheckSettingsPermitSOS(envState.LinkCache, settings.AllSets);
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string SPIDpath = Path.Combine(state.DataFolderPath, "skse\\plugins\\po3_SpellPerkItemDistributor.dll");
            if (!File.Exists(SPIDpath)) //SPIDtest (dual-level pun - whoa!)
            {
                throw new Exception("Spell Perk Item Distributor was not detected at " + SPIDpath + "\nAborting patch");
            }

            UTconfig settings = Settings.Value;

            Validator.validateSettings(settings);

            // create underwear items
            var UWsourcePlugins = new HashSet<ModKey>(); // set of source mod names for the underwear (to report to user so that they can be disabled)
            ItemImport.CreateItems(settings, UWsourcePlugins, state);

            // created leveled item lists (to be added to outfits)
            IFormLinkGetter<ILeveledItemGetter> UT_LeveledItemsAll = CreateLeveledList_AllItems(settings.AllSets, state.LinkCache, state.PatchMod);
            Dictionary<string, IFormLinkGetter<ILeveledItemGetter>> UT_LeveledItemsByWealth = CreateLeveledList_ByWealth(settings.AllSets, state.PatchMod);

            // modify NPC outfits
            AssignOutfits(settings, settings.DefaultSet.LeveledList, UT_LeveledItemsByWealth, UT_LeveledItemsAll, state);

            // Add slots used by underwear items to clothes and armors with 32 - Body slot active
            Dictionary<IArmorGetter, List<BipedObjectFlag>> usedSlots = Auxil.GetItemSetARMAslotsSorted(settings.AllSets, state.LinkCache);
            PatchBodyARMAslots(usedSlots, settings.PatchableRaces, settings.BlockedArmature, UWsourcePlugins, state, settings.VerboseMode);

            // set SOS compatibiilty if needed
            bool bSOS = AddSOScompatibility(settings.AllSets, state, settings.SOSSupport);

            // create and distribute gendered item inventory spell 
            CopyUTScript(state);
            CreateInventoryFixSpell(settings.AllSets, state);

            //remap dependencies
            foreach (var mk in UWsourcePlugins)
            {
                state.PatchMod.DuplicateFromOnlyReferenced(state.LinkCache, mk, out var _, typeof(Armor));
            }

            // message user
            ReportARMAslots(usedSlots, bSOS);
            ReportDeactivatablePlugins(UWsourcePlugins);

            Console.WriteLine("\nDon't forget to install Spell Perk Item Distributor to properly manage gender-specific items.");
            Console.WriteLine("\nEnjoy the underwear. Goodbye.");
        }

        public static IFormLinkGetter<ILeveledItemGetter> CreateLeveledList_AllItems(IEnumerable<UTSet> sets, ILinkCache lk, ISkyrimMod PatchMod)
        {
            var editorID = "UnderThereAllItems";
            var allItems = PatchMod.LeveledItems.AddNew(editorID);
            allItems.EditorID = editorID;
            allItems.Entries = new ExtendedList<LeveledItemEntry>();
            foreach (UTSet set in sets)
            {
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference = new FormLink<IItemGetter>(set.LeveledList.FormKey);
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                allItems.Entries.Add(entry);
            }

            return allItems.AsLink();
        }

        public static Dictionary<string, IFormLinkGetter<ILeveledItemGetter>> CreateLeveledList_ByWealth(IEnumerable<UTSet> sets, ISkyrimMod PatchMod)
        {
            var itemsByWealth = new Dictionary<string, IFormLinkGetter<ILeveledItemGetter>>()
            {
                { Default, Settings.Value.DefaultSet.LeveledList }
            };

            foreach (var group in sets.WhereCastable<UTSet, UTCategorySet>().GroupBy(s => s.Category))
            {
                itemsByWealth[group.Key] = CreateLList(group.Key, group, PatchMod).AsLink();
            }

            return itemsByWealth;
        }

        public static LeveledItem CreateLList(string nickname, IEnumerable<UTSet> sets, ISkyrimMod PatchMod)
        {
            var editorID = "UnderThereItems_" + nickname;
            var currentItems = PatchMod.LeveledItems.AddNew(editorID);
            currentItems.EditorID = editorID;
            currentItems.Entries = new ExtendedList<LeveledItemEntry>();

            foreach (var set in sets)
            {
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference = new FormLink<IItemGetter>(set.LeveledList.FormKey);
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                currentItems.Entries.Add(entry);
            }

            return currentItems;
        }

        public static void AssignOutfits(UTconfig settings, IFormLinkGetter<ILeveledItemGetter> UT_DefaultItem, Dictionary<string, IFormLinkGetter<ILeveledItemGetter>> UT_LeveledItemsByWealth, IFormLinkGetter<ILeveledItemGetter> UT_LeveledItemsAll, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string npcGroup = "";
            FormKey currentOutfitKey = FormKey.Null;
            IFormLinkGetter<ILeveledItemGetter> currentUW = FormLink<ILeveledItemGetter>.Null;
            var groupLookupFailures = new HashSet<IFormLinkGetter>();
            List<string> NPClookupFailures = new List<string>();
            Dictionary<FormKey, Dictionary<string, Outfit>> outfitMap = new Dictionary<FormKey, Dictionary<string, Outfit>>();

            var editorID = "No_Clothes";
            Outfit underwearOnly = state.PatchMod.Outfits.AddNew(editorID);
            underwearOnly.EditorID = editorID;
            underwearOnly.Items = new ExtendedList<IFormLinkGetter<IOutfitTargetGetter>>();

            List<string> spidOutfitAssignments = new List<string>();

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                NPCassignment specificAssignment = NPCassignment.getSpecificNPC(npc.FormKey, settings.SpecificNpcs);

                // check if NPC race should be patched
                bool isInventoryTemplate = !npc.DefaultOutfit.IsNull && !npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory);
                bool isGhost = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsGhost)
                    || npc.Voice.Equals(Skyrim.VoiceType.FemaleUniqueGhost)
                    || npc.Voice.Equals(Skyrim.VoiceType.MaleUniqueGhost)
                    || Auxil.HasGhostAbility(npc)
                    || Auxil.HasGhostScript(npc);

                if (!state.LinkCache.TryResolve<IRaceGetter>(npc.Race.FormKey, out var currentRace) ||
                    currentRace.EditorID == null ||
                    settings.NonPatchableRaces.Contains(currentRace) ||
                    Auxil.IsNonHumanoid(npc, currentRace, state.LinkCache) ||
                    (!settings.PatchSummonedNpcs && npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Summonable) && !npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique)) || // some unique NPCs like Embry seem to erroneously have "Summonable" flag set, which caused them to be skipped without the "unique" flag checked
                    (!settings.PatchGhosts && isGhost) ||
                    currentRace.EditorID.Contains("Child", StringComparison.OrdinalIgnoreCase) ||
                    (!settings.PatchableRaces.Contains(currentRace) && !isInventoryTemplate) ||
                    settings.BlockedNpcs.Contains(npc) ||
                    Auxil.HasBlockedFaction(npc, settings.BlockedFactions))
                {
                    continue;
                }

                // check if NPC gender should be patched
                if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) && !settings.PatchFemales)
                {
                    continue;
                }
                else if (!npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) && !settings.PatchMales)
                {
                    continue;
                }

                // check if NPC is a preset
                if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsCharGenFacePreset))
                {
                    continue;
                }

                // check if NPC is player
                if (npc.Equals(Skyrim.Npc.Player) || npc.Equals(Skyrim.Npc.PlayerInventory))
                {
                    continue;
                }


                // check if NPC has clothes and decide if it should be patched based on user settings
                currentOutfitKey = npc.DefaultOutfit.FormKey;
                if (currentOutfitKey.IsNull)
                {
                    if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory) && specificAssignment.isNull) // npc inherits inventory from a template - no need to patch
                    {
                        continue;
                    }
                    else if (!settings.PatchNakedNpcs && specificAssignment.isNull)
                    {
                        continue;
                    }
                    else
                    {
                        currentOutfitKey = underwearOnly.FormKey;
                    }
                }
                else
                {
                    if (state.LinkCache.TryResolve<IOutfitGetter>(npc.DefaultOutfit.FormKey, out var NPCoutfit) && NPCoutfit.Items != null && NPCoutfit.Items.Count == 0 && !settings.PatchNakedNpcs)
                    {
                        continue;
                    }
                }

                var npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                if (!specificAssignment.isNull)
                {
                    switch (specificAssignment.Type)
                    {
                        case NpcAssignmentType.Set:
                            npcGroup = specificAssignment.AssignmentSet;
                            currentUW = specificAssignment.AssignmentSet_Obj.LeveledList;
                            break;
                        case NpcAssignmentType.Group:
                            npcGroup = specificAssignment.AssignmentGroup;
                            currentUW = UT_LeveledItemsByWealth[npcGroup];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    // get the wealth of current NPC
                    switch (Settings.Value.AssignmentMode)
                    {
                        case AssignmentMode.Default:
                            npcGroup = Default;
                            currentUW = UT_DefaultItem; break;
                        case AssignmentMode.Class:
                            if (npc.Equals(Skyrim.Npc.Hroki))
                            {
                                npcGroup = Default; // hardcoded due to a particular idiosyncratic issue caused by Bethesda's weird choice of Class for Hroki.
                                break;
                            }
                            npcGroup = GetWealthGroup(npc.Class, settings.ClassDefinitions, groupLookupFailures);
                            if (npcGroup == "Default" && Settings.Value.QualityForNoAssignment.Trim() != "")
                            {
                                npcGroup = Settings.Value.QualityForNoAssignment.Trim();
                            }
                            currentUW = UT_LeveledItemsByWealth[npcGroup];
                            if (npcGroup == Default) { NPClookupFailures.Add(npc.EditorID + " (" + npc.FormKey.ToString() + ")"); }
                            break;
                        case AssignmentMode.Faction:
                            npcGroup = GetWealthGroupByFactions(npc, settings.FactionDefinitions, settings.FallBackFactionDefinitions, settings.IgnoreFactionsWhenScoring, groupLookupFailures);
                            currentUW = UT_LeveledItemsByWealth[npcGroup];
                            if (npcGroup == Default) { NPClookupFailures.Add(npc.EditorID + " (" + npc.FormKey.ToString() + ")"); }
                            break;
                        case AssignmentMode.Random:
                            npcGroup = "Random";
                            currentUW = UT_LeveledItemsAll;
                            break;
                    }
                }

                CreateOrGetOutfitWithUnderwear(currentOutfitKey, currentUW, npcGroup, outfitMap, state);
                switch(settings.OutfitAssignmentMode)
                {
                    case OutfitAssignmentMode.SPID: AssignOutfitViaSPID(npc, outfitMap[currentOutfitKey][npcGroup].FormKey, spidOutfitAssignments); break;
                    case OutfitAssignmentMode.Record: npcOverride.DefaultOutfit.SetTo(outfitMap[currentOutfitKey][npcGroup]); break;
                }

                AssignScriptedOutfits(npcOverride, currentUW, npcGroup, outfitMap, state);
            }

            //report failed lookups
            if (groupLookupFailures.Count > 0 || NPClookupFailures.Count > 0)
            {
                Auxil.LogDefaultNPCs(NPClookupFailures, groupLookupFailures, state.ExtraSettingsDataPath.GetValueOrDefault(), Settings.Value.QualityForNoAssignment);
            }
            
            if (settings.OutfitAssignmentMode == OutfitAssignmentMode.SPID)
            {
                WriteSPIDOutfitAssignments(spidOutfitAssignments, state);
            }
            else
            {
                DeleteSPIDOutfitAssignments(state);
            }
        }

        public static Outfit? CreateOrGetOutfitWithUnderwear(FormKey sourceOutfitFK, IFormLinkGetter<ILeveledItemGetter> currentUW, string npcGroup, Dictionary<FormKey, Dictionary<string, Outfit>> outfitMap, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // if the current outfit modified by the current wealth group doesn't exist, create it
            if (!outfitMap.ContainsKey(sourceOutfitFK) || !outfitMap[sourceOutfitFK].ContainsKey(npcGroup))
            {
                if (!state.LinkCache.TryResolve<IOutfitGetter>(sourceOutfitFK, out var NPCoutfit) || NPCoutfit == null) { return null; }
                var outfitEditorID = "";
                if (NPCoutfit.EditorID != null)
                {
                    outfitEditorID = NPCoutfit.EditorID + "_" + npcGroup;
                }

                Outfit newOutfit = state.PatchMod.Outfits.AddNew(outfitEditorID);
                newOutfit.DeepCopyIn(NPCoutfit);
                newOutfit.EditorID = outfitEditorID;

                if (newOutfit.Items != null)
                {
                    newOutfit.Items.Add(currentUW);
                }
                if (!outfitMap.ContainsKey(sourceOutfitFK))
                {
                    outfitMap[sourceOutfitFK] = new Dictionary<string, Outfit>();
                }
                outfitMap[sourceOutfitFK][npcGroup] = newOutfit;
                return newOutfit;
            }
            else
            {
                return outfitMap[sourceOutfitFK][npcGroup];
            }
        }

        public static void AssignScriptedOutfits(Npc npc, IFormLinkGetter<ILeveledItemGetter> currentUW, string npcGroup, Dictionary<FormKey, Dictionary<string, Outfit>> outfitMap, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (npc.VirtualMachineAdapter != null)
            {
                foreach (var script in npc.VirtualMachineAdapter.Scripts)
                {
                    foreach (var prop in script.Properties)
                    {
                        var objProp = prop as ScriptObjectProperty; // cast property to object propertyif possible
                        if (objProp != null && objProp.Object.TryResolve<IOutfitGetter>(state.LinkCache, out var scriptedOutfit) && scriptedOutfit != null)
                        {
                            var newOutfit = CreateOrGetOutfitWithUnderwear(scriptedOutfit.FormKey, currentUW, npcGroup, outfitMap, state);
                            if (newOutfit != null)
                            {
                                objProp.Object.SetTo(newOutfit);
                            }
                        }
                    }
                }
            }
        }

        public static void AssignOutfitViaSPID(INpcGetter npc, FormKey outfitFormKey, List<string> assignments)
        {
            string startString = "Outfit = " + GetSPIDstring(outfitFormKey) + "|NONE|";

            var existingAssignmentIndex = assignments.FindIndex(x => x.StartsWith(startString));
            if (existingAssignmentIndex >= 0)
            {
                assignments[existingAssignmentIndex] += "," + GetSPIDstring(npc.FormKey);
            }
            else
            {
                string newAssignment = startString + GetSPIDstring(npc.FormKey);
                assignments.Add(newAssignment);
            } 
        }

        public static string GetSPIDstring(FormKey fk)
        {
            return "0x" + fk.IDString().TrimStart('0') + "~" + fk.ModKey.ToString();
        }

        public static void WriteSPIDOutfitAssignments(List<string> assignments, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string destPath = Path.Combine(state.DataFolderPath, "UnderThereOutfits_DISTR.ini");
            try
            {
                File.WriteAllLines(destPath, assignments);
            }

            catch
            {
                throw new Exception("Could not write " + destPath);
            }
        }

        public static void DeleteSPIDOutfitAssignments(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string destPath = Path.Combine(state.DataFolderPath, "UnderThereOutfits_DISTR.ini");
            if (File.Exists(destPath))
            {
                try
                {
                    File.Delete(destPath);
                }

                catch
                {
                    throw new Exception("Could not delete " + destPath);
                }
            }
        }

        public static string GetWealthGroupByFactions(INpcGetter npc, Dictionary<string, HashSet<IFormLinkGetter<IFactionGetter>>> factionDefinitions, Dictionary<string, HashSet<IFormLinkGetter<IFactionGetter>>> fallbackFactionDefinitions, HashSet<IFormLinkGetter<IFactionGetter>> ignoredFactions, HashSet<IFormLinkGetter> GroupLookupFailures)
        {
            Dictionary<string, int> wealthCounts = new Dictionary<string, int>();
            Dictionary<string, int> fallBackwealthCounts = new Dictionary<string, int>();

            string tmpWealthGroup = "";
            bool bPrimaryWealthGroupFound = false;

            // initialize wealth counts
            foreach (var k in factionDefinitions.Keys)
            {
                wealthCounts.Add(k, 0);
                fallBackwealthCounts.Add(k, 0);
            }
            wealthCounts.Add(Default, 0);

            // add each faction by appropriate wealth count
            foreach (var fact in npc.Factions)
            {
                if (ignoredFactions.Contains(fact.Faction) || fact.Rank == 255) // 255 shows up as -1 in SSEedit
                {
                    wealthCounts[Default]++; // "Default" will be ignored if other factions are matched
                    continue;
                }

                tmpWealthGroup = GetWealthGroup(fact.Faction, factionDefinitions, GroupLookupFailures);

                if (wealthCounts.ContainsKey(tmpWealthGroup))
                {
                    wealthCounts[tmpWealthGroup]++;
                }

                if (tmpWealthGroup == Default) // check fallback factions
                {
                    tmpWealthGroup = GetWealthGroup(fact.Faction, fallbackFactionDefinitions, GroupLookupFailures);
                    if (fallBackwealthCounts.ContainsKey(tmpWealthGroup))
                    {
                        fallBackwealthCounts[tmpWealthGroup]++;
                    }
                }
                else
                {
                    bPrimaryWealthGroupFound = true;
                }
            }

            // get the wealth group with the highest number of corresponding factions.

            // If no primary wealth groups were matched, use fallback wealth groups if they were matched
            if (!bPrimaryWealthGroupFound && fallBackwealthCounts.Values.Max() > 0)
            {
                wealthCounts = fallBackwealthCounts;
            }

            // first remove the "Default" wealth group if others are populated
            foreach (string wGroup in wealthCounts.Keys)
            {
                if (wGroup != Default && wealthCounts[wGroup] > 0)
                {
                    wealthCounts.Remove(Default);
                    break;
                }
            } // if "Default" was the only matched wealth group, then it remains in the wealthCounts dictionary and will necessarily be chosen

            // then figure out which wealth group was matched to the highest number of factions
            int maxFactionsMatched = wealthCounts.Values.Max();
            List<string> bestMatches = new List<string>();
            foreach (string x in wealthCounts.Keys)
            {
                if (wealthCounts[x] == maxFactionsMatched)
                {
                    bestMatches.Add(x);
                }
            }

            if (npc.Factions == null || npc.Factions.Count == 0 || maxFactionsMatched == 0)
            {
                if (Settings.Value.QualityForNoAssignment.Trim() != "")
                {
                    return Settings.Value.QualityForNoAssignment.Trim();
                }
                else
                {
                    return "Default";
                }
            }

            // return the wealth group that was matched to the highest number of factions (choose random if tied)
            return bestMatches[Random.Value.Next(bestMatches.Count)];
        }

        public static string GetWealthGroup<T>(IFormLinkGetter<T> link, Dictionary<string, HashSet<IFormLinkGetter<T>>> Definitions, HashSet<IFormLinkGetter> GroupLookupFailures)
            where T : class, IMajorRecordGetter
        {
            foreach (var Def in Definitions)
            {
                if (Def.Value.Contains(link))
                {
                    return Def.Key;
                }
            }

            GroupLookupFailures.Add(link);
            return Default;
        }

        public static void CopyUTScript(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string UTscriptPath = Path.Combine(state.ExtraSettingsDataPath.GetValueOrDefault(), "UnderThereGenderedItemFix.pex");

            if (!File.Exists(UTscriptPath))
            {
                throw new Exception("Could not find " + UTscriptPath);
            }
            else
            {
                string destPath = Path.Combine(state.DataFolderPath, "Scripts\\UnderThereGenderedItemFix.pex");
                try
                {
                    File.Copy(UTscriptPath, destPath, true);
                }
                catch
                {
                    throw new Exception("Could not copy " + UTscriptPath + " to " + destPath);
                }
            }
        }

        public static void CreateInventoryFixSpell(IEnumerable<UTSet> sets, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // get all gendered items
            var genderedItems = GetGenderedItems(sets);

            // create gendered item FormLists
            var editorIDmale = "UT_FLST_MaleOnly";
            FormList maleItems = state.PatchMod.FormLists.AddNew(editorIDmale);
            maleItems.EditorID = editorIDmale;
            foreach (var fk in genderedItems.Male)
            {
                maleItems.Items.Add(fk);
            }

            var editorIDfemale = "UT_FLST_FemaleOnly";
            FormList femaleItems = state.PatchMod.FormLists.AddNew(editorIDfemale);
            femaleItems.EditorID = editorIDfemale;
            foreach (var fk in genderedItems.Female)
            {
                femaleItems.Items.Add(fk);
            }

            // create spell for SPID distribution
            // create MGEF first
            var editorIDmgef = "UT_MGEF_GenderedInventoryFix";
            MagicEffect utItemFixEffect = state.PatchMod.MagicEffects.AddNew(editorIDmgef);
            utItemFixEffect.EditorID = editorIDmgef;
            utItemFixEffect.Name = "Removes female-only items from males and vice-versa";
            utItemFixEffect.Flags |= MagicEffect.Flag.HideInUI;
            utItemFixEffect.Flags |= MagicEffect.Flag.NoDeathDispel;
            utItemFixEffect.Archetype = new MagicEffectArchetype()
            {
                Type = MagicEffectArchetype.TypeEnum.Script
            };
            utItemFixEffect.TargetType = TargetType.Self;
            utItemFixEffect.CastType = CastType.ConstantEffect;
            utItemFixEffect.VirtualMachineAdapter = new VirtualMachineAdapter();

            ScriptEntry UTinventoryFixScript = new ScriptEntry();
            UTinventoryFixScript.Name = "UnderThereGenderedItemFix";

            ScriptObjectProperty mProp = new ScriptObjectProperty();
            mProp.Name = "maleItems";
            mProp.Flags |= ScriptProperty.Flag.Edited;
            mProp.Object.SetTo(maleItems);
            UTinventoryFixScript.Properties.Add(mProp);

            ScriptObjectProperty fProp = new ScriptObjectProperty();
            fProp.Name = "femaleItems";
            fProp.Flags |= ScriptProperty.Flag.Edited;
            fProp.Object.SetTo(femaleItems);
            UTinventoryFixScript.Properties.Add(fProp);

            utItemFixEffect.VirtualMachineAdapter.Scripts.Add(UTinventoryFixScript);

            // create Spell
            var editorIDspell = "UT_SPEL_GenderedInventoryFix";
            Spell utItemFixSpell = state.PatchMod.Spells.AddNew(editorIDspell);
            utItemFixSpell.EditorID = editorIDspell;
            utItemFixSpell.Name = "Fixes gendered UnderThere inventory";
            utItemFixSpell.CastType = CastType.ConstantEffect;
            utItemFixSpell.TargetType = TargetType.Self;
            utItemFixSpell.Type = SpellType.Ability;
            utItemFixSpell.EquipmentType.SetTo(Skyrim.EquipType.EitherHand);
            Effect utItemFixShellEffect = new Effect();
            utItemFixShellEffect.BaseEffect.SetTo(utItemFixEffect);
            utItemFixShellEffect.Data = new EffectData();
            utItemFixSpell.Effects.Add(utItemFixShellEffect);

            // distribute spell via SPID
            string distr = "Spell = " + utItemFixSpell.FormKey.IDString() + " - " + utItemFixSpell.FormKey.ModKey.ToString() + " | ActorTypeNPC | NONE | NONE | NONE";
            string destPath = Path.Combine(state.DataFolderPath, "UnderThereGenderedItemFix_DISTR.ini");
            try
            {
                File.WriteAllLines(destPath, new List<string> { distr });
            }

            catch
            {
                throw new Exception("Could not write " + destPath);
            }
        }

        public static (HashSet<IFormLinkGetter<IArmorGetter>> Male, HashSet<IFormLinkGetter<IArmorGetter>> Female) GetGenderedItems(IEnumerable<UTSet> sets)
        {
            var male = new HashSet<IFormLinkGetter<IArmorGetter>>();
            var female = new HashSet<IFormLinkGetter<IArmorGetter>>();

            foreach (UTSet set in sets)
            {
                male.Add(set.Items.Where(i => i.Gender == GenderTarget.Male).Select(m => m.Record));
                female.Add(set.Items.Where(i => i.Gender == GenderTarget.Female).Select(m => m.Record));
            }

            //make sure that gendered items aren't mixed
            foreach (var maleItem in male)
            {
                if (female.Contains(maleItem))
                {
                    throw new Exception("Error: found item " + maleItem.ToString() + " in both Items_Male and Items_Female. Please move it to Items_Mutual.");
                }
            }
            foreach (var femaleItem in female)
            {
                if (male.Contains(femaleItem))
                {
                    throw new Exception("Error: found item " + femaleItem.ToString() + " in both Items_Male and Items_Female. Please move it to Items_Mutual.");
                }
            }

            return (male, female);
        }

        public static void PatchBodyARMAslots(Dictionary<IArmorGetter, List<BipedObjectFlag>> usedSlots, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaces, IReadOnlyCollection<IFormLinkGetter<IArmorAddonGetter>> excludedArmature, HashSet<ModKey> UWsourcePlugins, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, bool bVerboseMode)
        {
            var allUsedSlots = new HashSet<BipedObjectFlag>();
            foreach(var slotsPerArmor in usedSlots.Values)
            {
                foreach(var slot in slotsPerArmor)
                {
                    if (!allUsedSlots.Contains(slot))
                    {
                        allUsedSlots.Add(slot);
                    }
                }
            }

            foreach (var arma in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
            {
                if (!state.LinkCache.TryResolve<IRaceGetter>(arma.Race.FormKey, out var armaRace) || armaRace.EditorID == null || armaRace.EditorID.Contains("Child", StringComparison.OrdinalIgnoreCase) || UWsourcePlugins.Contains(arma.FormKey.ModKey) || excludedArmature.Contains(arma)) // don't patch armor addons from a UWsourcePlugin because those are meant to be fully merged into synthesis.esp anyway (otherwise they will be added as masters)
                {
                    continue;
                }

                if (arma.Race.Equals(Skyrim.Race.DefaultRace) || patchableRaces.Contains(armaRace.AsLink()))
                {
                    if (arma.BodyTemplate != null && arma.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body))
                    {
                        if (bVerboseMode)
                        {
                            Console.WriteLine("Patching armor addon: {0}", arma.FormKey.ToString());
                        }
                        var patchedAA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(arma);
                        if (patchedAA.BodyTemplate == null) continue;
                        foreach (var uwSlot in allUsedSlots)
                        {
                            try
                            {
                                patchedAA.BodyTemplate.FirstPersonFlags |= uwSlot;
                                if (bVerboseMode)
                                {
                                    Console.WriteLine("added slot {0}", Auxil.MapSlotToInt(uwSlot));
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Failed to add slot {0} to armor addon {1}", Auxil.MapSlotToInt(uwSlot), arma.FormKey.ToString());
                            }
                        }
                    }
                }
            }
        }

        public static void ReportARMAslots(Dictionary<IArmorGetter, List<BipedObjectFlag>> usedSlots, bool bSOS)
        {
            var allSlots = Auxil.GetItemSetARMAslotsAll(usedSlots);
            Console.WriteLine("\nThe following slots are being used by underwear. Please make sure they don't conflict with any other modded armors.");
            foreach (var slot in allSlots)
            {
                Console.WriteLine(Auxil.MapSlotToInt(slot));
            }
            if (bSOS)
            {
                Console.WriteLine("52 (Inserted by patcher for SOS Compatibility)");
            }
        }

        public static void ReportDeactivatablePlugins(IEnumerable<ModKey> plugins)
        {
            Console.WriteLine("\nThe following plugins have been absorbed into the synthesis patch and may now be deactivated. Make sure to keep the associated meshes and textures enabled.");
            foreach (var p in plugins)
            {
                Console.WriteLine(p);
            }
        }

        public static bool UserHasSOS(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var mod in state.LoadOrder)
            {
                if (mod.Key.FileName == "Schlongs of Skyrim - Core.esm")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool UserHasSOS(IRunnabilityState state)
        {
            foreach (var mod in state.LoadOrder)
            {
                if (mod.Key.FileName == "Schlongs of Skyrim - Core.esm")
                {
                    return true;
                }
            }
            return false;
        }

        public static void CheckSettingsPermitSOS(ILinkCache linkCache, IEnumerable<UTSet> sets)
        {
            var usedSlots = Auxil.GetItemSetARMAslotsSorted(sets, linkCache);

            // check to make sure no current armor addons use slot 52
            foreach (var slotsPerArmor in usedSlots)
            {
                foreach (var slot in slotsPerArmor.Value)
                {
                    if (Auxil.MapSlotToInt(slot) == 52)
                    {
                        var offendingItem = slotsPerArmor.Key;
                        var itemString = offendingItem.FormKey.ToString();
                        if (offendingItem.EditorID != null)
                        {
                            itemString += " (" + offendingItem.EditorID + ")";
                        }
                        throw new Exception("Schlongs of Skyrim has been detected, and one of your imported underwear items is slot 52: " + itemString + "). This will cause a clothing conflict in-game where SoS will remove all clothes from NPCs wearing this item. Please edit the offending item, changing both the Armor Addon AND the nif file to a slot other than 52 (49 is recommended).");
                    }
                }
            }
        }

        public static bool AddSOScompatibility(IEnumerable<UTSet> sets, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, SOSmode sosMode)
        {
            bool bSOSdetected;
            if (sosMode == SOSmode.ForceOn) {  bSOSdetected = true; }
            else
            {
                bSOSdetected = UserHasSOS(state);
            }

            if (!bSOSdetected || sosMode == SOSmode.ForceOff)
            {
                if (bSOSdetected)
                {
                    Console.WriteLine("Warning: SOS was detected but your SOS mode is set to Force OFF. Expect visual conflicts.");
                }
                return false;
            }

            // patch all bottoms to use slot 52
            foreach (var set in sets)
            {
                AddSOSslot(set.Items, state);
            }
            return true;
        }

        public static void AddSOSslot(List<UTitem> items, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var item in items)
            {
                if (item.IsBottom && item.Record.TryResolve<IArmor>(state.LinkCache, out var moddedItem))
                {
                    foreach (var aa in moddedItem.Armature)
                    {
                        if (state.LinkCache.TryResolve<IArmorAddonGetter>(aa.FormKey, out var moddedAA))
                        {
                            var moddedAA_override = state.PatchMod.ArmorAddons.GetOrAddAsOverride(moddedAA);
                            if (moddedAA_override.BodyTemplate != null)
                            {
                                moddedAA_override.BodyTemplate.FirstPersonFlags |= Auxil.MapIntToSlot(52);
                            }
                        }
                    }
                }
            }
        }
    }
}
