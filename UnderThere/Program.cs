using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnderThere
{
    public class Program
    {
        public static Task<int> Main(string[] args)
        {
            return SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args, new RunPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        TargetRelease = GameRelease.SkyrimSE
                    }
                });
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var settingsPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereConfig.json");

            UTconfig settings = new UTconfig();

            settings = JsonUtils.FromJson<UTconfig>(settingsPath);

            if (validateSettings(settings) == false)
            {
                throw new Exception("Please fix the errors in the settings file and try again.");
            }

            List<string> UWsourcePlugins = new List<string>(); // list of source mod names for the underwear (to report to user so that they can be disabled)

            createItems(settings.Sets, settings.bMakeItemsEquippable, UWsourcePlugins, state.LinkCache, state.PatchMod);

            // created leveled item lists (to be added to outfits)
            Dictionary<string, FormKey> UT_LeveledItemsByWealth = createLeveledList_ByWealth(settings.Sets, settings.Assignments, state.LinkCache, state.PatchMod);
            FormKey UT_LeveledItemsAll = createLeveledList_AllItems(settings.Sets, state.LinkCache, state.PatchMod);
            FormKey UT_DefaultItem = getDefaultItemFormKey(settings.Sets, settings.Assignments, state.LinkCache, state.PatchMod);

            // modify NPC outfits
            assignOutfits(settings, UT_DefaultItem, UT_LeveledItemsByWealth, UT_LeveledItemsAll, state);

            // Add slots used by underwear items to clothes and armors with 32 - Body slot active
            List<BipedObjectFlag> usedSlots = getARMAslots(settings.Sets, state.LinkCache);
            patchBodyARMAslots(usedSlots, settings.PatchableRaces, state);

            // create and distribute inventory spell 
            copyUTScript(state);
            createInventoryFixSpell(settings.Sets, state);

            // message user
            reportARMAslots(usedSlots);
            reportDeactivatablePlugins(UWsourcePlugins);

            Console.WriteLine("\nDon't forget to install Spell Perk Item Distributor to properly manage gender-specific items.");
            Console.WriteLine("\nEnjoy the underwear. Goodbye.");
        }

        public static FormKey getDefaultItemFormKey(List<UTSet> sets, Dictionary<string, List<string>> assignments, ILinkCache lk, ISkyrimMod PatchMod)
        {
            if (assignments["Default"] == null || assignments["Default"].Count == 0)
            {
                throw new Exception("Error: could not find a default underwear defined in the settings file.");
            }

            string defaultUWname = assignments["Default"][0];

            foreach (UTSet set in sets)
            {
                if (set.Name == defaultUWname)
                {
                    return set.LeveledListFormKey;
                }
            }

            throw new Exception("Error: Could not find a Set with name " + defaultUWname);
        }

        public static FormKey createLeveledList_AllItems(List<UTSet> sets, ILinkCache lk, ISkyrimMod PatchMod)
        {
            var allItems = PatchMod.LeveledItems.AddNew();
            allItems.EditorID = "UnderThereAllItems";
            allItems.Entries = new Noggog.ExtendedList<LeveledItemEntry>();
            foreach (UTSet set in sets)
            {
                addUTitemsToLeveledList(set.Items_Mutual, allItems);
                addUTitemsToLeveledList(set.Items_Male, allItems);
                addUTitemsToLeveledList(set.Items_Female, allItems);
            }

            return allItems.FormKey;
        }

        public static void addUTitemsToLeveledList(List<UTitem> items, LeveledItem allItems)
        {
            if (allItems.Entries == null)
            {
                return;
            }

            foreach (UTitem item in items)
            {
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference = item.formKey;
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                allItems.Entries.Add(entry);
            }
        }

        public static Dictionary<string, FormKey> createLeveledList_ByWealth(List<UTSet> sets, Dictionary<string, List<string>> assignments, ILinkCache lk, ISkyrimMod PatchMod)
        {
            Dictionary<string, FormKey> itemsByWealth = new Dictionary<string, FormKey>();

            foreach (KeyValuePair<string, List<string>> assignment in assignments)
            {
                if (assignment.Value.Count == 0)
                {
                    continue;
                }

                var currentItems = PatchMod.LeveledItems.AddNew();
                currentItems.EditorID = "UnderThereItems_" + assignment.Key;
                currentItems.Entries = new Noggog.ExtendedList<LeveledItemEntry>();

                foreach (UTSet set in sets)
                {
                    if (assignment.Value.Contains(set.Name))
                    {
                        LeveledItemEntry entry = new LeveledItemEntry();
                        LeveledItemEntryData data = new LeveledItemEntryData();
                        data.Reference = set.LeveledListFormKey;
                        data.Level = 1;
                        data.Count = 1;
                        entry.Data = data;
                        currentItems.Entries.Add(entry);
                    }
                }

                itemsByWealth[assignment.Key] = currentItems.FormKey;
            }

            return itemsByWealth;
        }

        public static void assignOutfits(UTconfig settings, FormKey UT_DefaultItem, Dictionary<string, FormKey> UT_LeveledItemsByWealth, FormKey UT_LeveledItemsAll, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string npcGroup = "";
            FormKey currentOutfitKey = new FormKey();
            FormKey currentUWkey = new FormKey();
            List<String> GroupLookupFailures = new List<string>();
            Dictionary<FormKey, Dictionary<string, Outfit>> OutfitMap = new Dictionary<FormKey, Dictionary<string, Outfit>>();

            string mode = settings.AssignmentMode.ToLower();

            Outfit underwearOnly = state.PatchMod.Outfits.AddNew();
            underwearOnly.EditorID = "No_Clothes";
            underwearOnly.Items = new Noggog.ExtendedList<IFormLink<IOutfitTargetGetter>>();

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                NPCassignment specificAssignment = NPCassignment.getSpecificNPC(npc.FormKey, settings.SpecificNPCs);

                // check if NPC race should be patched
                if (!state.LinkCache.TryResolve<IRaceGetter>(npc.Race.FormKey, out var currentRace) || currentRace == null || currentRace.EditorID == null || settings.PatchableRaces.Contains(currentRace.EditorID) == false)
                {
                    continue;
                }

                // check if NPC gender should be patched
                if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) && settings.bPatchFemales == false)
                {
                    continue;
                }
                else if (!npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) && settings.bPatchMales == false)
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
                    else if (settings.bPatchNakedNPCs == false && specificAssignment.isNull)
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
                    if (state.LinkCache.TryResolve<IOutfitGetter>(npc.DefaultOutfit.FormKey, out var NPCoutfit) && NPCoutfit != null && NPCoutfit.Items != null && NPCoutfit.Items.Count == 0 && settings.bPatchNakedNPCs == false)
                    {
                        continue;
                    }
                }

                var NPCoverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                if (!specificAssignment.isNull)
                {
                    switch(specificAssignment.Type)
                    {
                        case "set":
                            npcGroup = specificAssignment.Assignment_Set;
                            currentUWkey = specificAssignment.Assignment_Set_Obj.LeveledListFormKey;
                            break;
                        case "group":
                            npcGroup = specificAssignment.Assignment_Group;
                            currentUWkey = UT_LeveledItemsByWealth[npcGroup];
                            break;
                    }    
                }
                else
                {
                    // get the wealth of current NPC
                    switch (mode)
                    {
                        case "default":
                            npcGroup = "Default";
                            currentUWkey = UT_DefaultItem; break;
                        case "class":
                            if (state.LinkCache.TryResolve<IClassGetter>(npc.Class.FormKey, out var NPCclass) && NPCclass != null && NPCclass.EditorID != null)
                            {
                                if (npc.EditorID == "Hroki" && npc.FormKey.IDString() == "01339E")
                                {
                                    npcGroup = "Default"; // hardcoded due to a particular idiosyncratic issue due to Bethesda's weird choice of Class for Hroki.
                                    break;
                                }
                                npcGroup = getWealthGroupByEDID(NPCclass.EditorID, settings.ClassDefinitions, GroupLookupFailures);
                                currentUWkey = UT_LeveledItemsByWealth[npcGroup];
                            }
                            break;
                        case "faction":
                            npcGroup = getWealthGroupByFactions(npc, settings.FactionDefinitions, settings.IgnoreFactionsWhenScoring, GroupLookupFailures, state);
                            currentUWkey = UT_LeveledItemsByWealth[npcGroup];
                            break;
                        case "random":
                            npcGroup = "Random";
                            currentUWkey = UT_LeveledItemsAll;
                            break;
                    }
                }

                // if the current outfit modified by the current wealth group doesn't exist, create it
                if (OutfitMap.ContainsKey(currentOutfitKey) == false || OutfitMap[currentOutfitKey].ContainsKey(npcGroup) == false)
                {
                    if (!state.LinkCache.TryResolve<IOutfitGetter>(currentOutfitKey, out var NPCoutfit) || NPCoutfit == null) { continue; }
                    Outfit newOutfit = state.PatchMod.Outfits.AddNew();
                    newOutfit.DeepCopyIn(NPCoutfit);
                    if (newOutfit.EditorID != null)
                    {
                        newOutfit.EditorID += "_" + npcGroup;
                    }
                    if (newOutfit.Items != null)
                    {
                        newOutfit.Items.Add(currentUWkey);
                    }
                    if (OutfitMap.ContainsKey(currentOutfitKey) == false)
                    {
                        OutfitMap[currentOutfitKey] = new Dictionary<string, Outfit>();
                    }
                    OutfitMap[currentOutfitKey][npcGroup] = newOutfit;
                }

                NPCoverride.DefaultOutfit = OutfitMap[currentOutfitKey][npcGroup]; // assign the correct outfit to the current NPC
            }

            //report failed lookups
            if (GroupLookupFailures.Count > 0)
            {
                switch (settings.AssignmentMode)
                {
                    case "class":
                        Console.WriteLine("The following classes were not found in your settings file's ClassDefinitions:");
                        foreach (string s in GroupLookupFailures)
                        {
                            Console.WriteLine(s);
                        }
                        Console.WriteLine("NPCs of this class were assigned default underwear.");
                        break;
                    case "faction":
                        Console.WriteLine("The following factions were not found in your settings file's FactionDefinitions:");
                        foreach (string s in GroupLookupFailures)
                        {
                            Console.WriteLine(s);
                        }
                        Console.WriteLine("These factions were ignored when assigning underwear. NPCs that only belonged to these factions were assigned default underwear.");
                        break;
                }
            }
        }

        public static string getWealthGroupByFactions(INpcGetter npc, Dictionary<string, List<string>> factionDefinitions, List<string> ignoredFactions, List<string> GroupLookupFailures, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<string, int> wealthCounts = new Dictionary<string, int>();
            string tmpWealthGroup = "";

            // initialize wealth counts
            foreach (KeyValuePair<string, List<string>> Def in factionDefinitions)
            {
                wealthCounts.Add(Def.Key, 0);
            }
            wealthCounts.Add("Default", 0);

            // add each faction by appropriate wealth count
            foreach (var fact in npc.Factions)
            {
                if (!state.LinkCache.TryResolve<IFactionGetter>(fact.Faction.FormKey, out var currentFaction) || currentFaction == null || currentFaction.EditorID == null) { continue; }

                if (ignoredFactions.Contains(currentFaction.EditorID))
                {
                    wealthCounts["Default"]++; // "Default" will be ignored if other factions are matched
                }

                tmpWealthGroup = getWealthGroupByEDID(currentFaction.EditorID, factionDefinitions, GroupLookupFailures);

                if (wealthCounts.ContainsKey(tmpWealthGroup))
                {
                    wealthCounts[tmpWealthGroup]++;
                }
            }

            // get the wealth group with the highest number of corresponding factions.

            // first remove the "Default" wealth group if others are populated
            if (wealthCounts.ContainsKey("Default"))
            {
                foreach (string wGroup in wealthCounts.Keys)
                {
                    if (wGroup != "Default" && wealthCounts[wGroup] > 0)
                    {
                        wealthCounts.Remove("Default");
                    }
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

            // return the wealth group that was matched to the highest number of factions (choose random if tied)
            var random = new Random();
            return bestMatches[random.Next(bestMatches.Count)]; 
        }

        public static string getWealthGroupByEDID(string EDID, Dictionary<string, List<string>> Definitions, List<string> GroupLookupFailures)
        {
            foreach (KeyValuePair<string, List<string>> Def in Definitions)
            {
                if (Def.Value.Contains(EDID))
                {
                    return Def.Key;
                }
            }

            // if EDID wasn't found in definitions
            if (GroupLookupFailures.Contains(EDID) == false)
            {
                GroupLookupFailures.Add(EDID);
            }

            return "Default";
        }

        public static bool validateSettings(UTconfig settings)
        {
            if (settings == null)
            {
                return false;
            }

            if (settings.PatchableRaces == null)
            {
                return false;
            }

            if (settings.ClassDefinitions == null)
            {
                return false;
            }
            if (settings.FactionDefinitions == null)
            {
                return false;
            }
            if (settings.AssignmentMode == null)
            {
                return false;
            }
            else
            {
                settings.AssignmentMode = settings.AssignmentMode.ToLower();
                string[] validModes = new string[] { "default", "random", "class", "faction" };
                if (validModes.Contains(settings.AssignmentMode) == false)
                {
                    Console.WriteLine("Error: AssignmentMode can only contain one of the following:");
                    foreach (String m in validModes)
                    {
                        Console.WriteLine(m);
                    }
                    return false;
                }
            }

            if (settings.Assignments == null)
            {
                return false;
            }

            if (settings.Sets == null)
            {
                return false;
            }

            if (settings.SpecificNPCs == null)
            {
                return false;
            }
            else
            {
                foreach (var npc in settings.SpecificNPCs)
                {
                    if (!FormKey.TryFactory(npc.FormKey, out var currentFK) || currentFK == null)
                    {
                        return false;
                    }
                    else
                    {
                        npc.FormKeyObj = currentFK;
                    }

                    string npcID = npc.Name + " (" + npc.FormKey + ")";

                    npc.Type = npc.Type.ToLower();
                    switch(npc.Type)
                    {
                        case "set":
                            if (npc.Assignment_Set == null)
                            {
                                return false;
                            }
                            bool bFound = false;
                            foreach (UTSet set in settings.Sets)
                            {
                                if (set.Name == npc.Assignment_Set)
                                {
                                    bFound = true;
                                    npc.Assignment_Set_Obj = set;
                                    break;
                                }
                            }
                            if (bFound == false)
                            {
                                throw new Exception("Specific NPC Assignment " + npcID + "'s Assignemnt_Set could not be matched to one of the defined sets.");
                            }
                            break;

                        case "group":
                            if (npc.Assignment_Group == null)
                            {
                                return false;
                            }
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
            }

            return true;
        }

        public static void createItems(List<UTSet> Sets, bool bMakeItemsEquipable, List<string> UWsourcePlugins, ILinkCache lk, ISkyrimMod PatchMod)
        {
            deepCopyItems(Sets, UWsourcePlugins, lk, PatchMod); // copy all armor records along with their linked subrecords into PatchMod to get rid of dependencies on the original plugins. Sets[i].FormKeyObject will now point to the new FormKey in PatchMod

            // create a leveled list entry for each set
            foreach (var set in Sets)
            {
                var currentItems = PatchMod.LeveledItems.AddNew();
                currentItems.EditorID = "LItems_" + set.Name;
                currentItems.Flags |= LeveledItem.Flag.UseAll;
                currentItems.Entries = new Noggog.ExtendedList<LeveledItemEntry>();

                editAndStoreUTitems(set.Items_Mutual, currentItems, bMakeItemsEquipable, lk);
                editAndStoreUTitems(set.Items_Male, currentItems, bMakeItemsEquipable, lk);
                editAndStoreUTitems(set.Items_Female, currentItems, bMakeItemsEquipable, lk);
                set.LeveledListFormKey = currentItems.FormKey;
            }
        }

        public static void deepCopyItems(List<UTSet> Sets, List<string> UWsourcePlugins, ILinkCache lk, ISkyrimMod PatchMod)
        {
            var recordsToDup = new HashSet<FormLinkInformation>();

            foreach (var set in Sets)
            {
                getFormLinksToDuplicate(set.Items_Mutual, recordsToDup, lk);
                getFormLinksToDuplicate(set.Items_Male, recordsToDup, lk);
                getFormLinksToDuplicate(set.Items_Female, recordsToDup, lk);
            }

            // store the original source mod names to notify user that they can be disabled.
            foreach (var td in recordsToDup)
            {
                if (UWsourcePlugins.Contains(td.FormKey.ModKey.ToString()) == false)
                {
                    UWsourcePlugins.Add(td.FormKey.ModKey.ToString());
                }
            }

            var deleteMeEventually = (ILinkCache<ISkyrimMod>)lk; // will be moved to lk directly in next Mutagen version.
            var duplicated = recordsToDup
                .Select(toDup =>
                {
                    if (!deleteMeEventually.TryResolveContext(toDup.FormKey, toDup.Type, out var existingContext))
                    {
                        throw new ArgumentException($"Couldn't find {toDup.FormKey}?");
                    }
                    return (OldFormKey: toDup.FormKey, Duplicate: existingContext.DuplicateIntoAsNewRecord(PatchMod));
                })
                .ToList();

            // Remap form links in each record to point to the duplicated versions
            var remap = duplicated.ToDictionary(x => x.OldFormKey, x => x.Duplicate.FormKey);
            foreach (var dup in duplicated)
            {
                dup.Duplicate.RemapLinks(remap);
            }

            // remap Set formlinks to the duplicated ones
            foreach (UTSet set in Sets)
            {
                remapSetItemList(set.Items_Mutual, remap);
                remapSetItemList(set.Items_Male, remap);
                remapSetItemList(set.Items_Female, remap);
            }
        }

        public static void getFormLinksToDuplicate(List<UTitem> UTitemList, HashSet<FormLinkInformation> recordsToDup, ILinkCache lk)
        {
            foreach (var item in UTitemList)
            {
                if (FormKey.TryFactory(item.Record, out var origFormKey) && !origFormKey.IsNull)
                {
                    if (!lk.TryResolve<IArmorGetter>(origFormKey, out var origItem))
                    {
                        throw new Exception("Could not find item with formKey " + origFormKey + ". Please make sure that " + origFormKey.ModKey.ToString() + " is active in your load order.");
                    }

                    foreach (FormLinkInformation FLI in origItem.ContainedFormLinks)
                    {
                        if (FLI.FormKey.ModKey == origItem.FormKey.ModKey) // only copy subrecord as new record if it comes from the same mod as the armor itself
                        {
                            recordsToDup.Add(FLI);
                        }
                    }
                    recordsToDup.Add(origItem.ToFormLinkInformation());
                }
            }
        }

        public static void remapSetItemList(List<UTitem> UTitemList, Dictionary<FormKey, FormKey> remap)
        {
            foreach (var item in UTitemList)
            {
                FormKey.TryFactory(item.Record, out var origFormKey);
                item.formKey = remap[origFormKey];
            }
        }

        public static void editAndStoreUTitems(List<UTitem> items, LeveledItem currentItems, bool bMakeItemsEquipable, ILinkCache lk)
        {
            foreach (UTitem item in items)
            {
                if (lk.TryResolve<IArmor>(item.formKey, out var moddedItem) && currentItems.Entries != null)
                {
                    moddedItem.Name = item.DispName;
                    moddedItem.EditorID = "UT_" + moddedItem.EditorID;
                    moddedItem.Weight = item.Weight;
                    moddedItem.Value = item.Value;

                    switch (bMakeItemsEquipable)
                    {
                        case true: moddedItem.MajorFlags &= Armor.MajorFlag.NonPlayable; break;
                        case false: moddedItem.MajorFlags |= Armor.MajorFlag.NonPlayable; break;
                    }

                    LeveledItemEntry entry = new LeveledItemEntry();
                    LeveledItemEntryData data = new LeveledItemEntryData();
                    data.Reference = moddedItem.FormKey;
                    data.Level = 1;
                    data.Count = 1;
                    entry.Data = data;
                    currentItems.Entries.Add(entry);
                }
            }
        }

        public static void copyUTScript(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string UTscriptPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereGenderedItemFix.pex");

            if (File.Exists(UTscriptPath) == false)
            {
                throw new Exception("Could not find " + UTscriptPath);
            }
            else
            {
                string destPath = Path.Combine(state.Settings.DataFolderPath, "Scripts\\UnderThereGenderedItemFix.pex");
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

        public static void createInventoryFixSpell(List<UTSet> sets, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // get all gendered items
            Dictionary<string, List<FormKey>> genderedItems = getGenderedItems(sets);

            // create gendered item FormLists
            FormList maleItems = state.PatchMod.FormLists.AddNew();
            maleItems.EditorID = "UT_FLST_MaleOnly";
            foreach (var fk in genderedItems["male"])
            {
                maleItems.Items.Add(fk);
            }

            FormList femaleItems = state.PatchMod.FormLists.AddNew();
            femaleItems.EditorID = "UT_FLST_FemaleOnly";
            foreach (var fk in genderedItems["female"])
            {
                femaleItems.Items.Add(fk);
            }

            // create spell for SPID distribution
            // create MGEF first
            MagicEffect utItemFixEffect = state.PatchMod.MagicEffects.AddNew();
            utItemFixEffect.EditorID = "UT_MGEF_GenderedInventoryFix";
            utItemFixEffect.Name = "Removes female-only items from males and vice-versa";
            utItemFixEffect.Flags |= MagicEffect.Flag.HideInUI;
            utItemFixEffect.Flags |= MagicEffect.Flag.NoDeathDispel;
            utItemFixEffect.Archetype.Type = MagicEffectArchetype.TypeEnum.Script;
            utItemFixEffect.TargetType = TargetType.Self;
            utItemFixEffect.CastType = CastType.ConstantEffect;
            utItemFixEffect.VirtualMachineAdapter = new VirtualMachineAdapter();

            ScriptEntry UTinventoryFixScript = new ScriptEntry();
            UTinventoryFixScript.Name = "UnderThereGenderedItemFix";

            ScriptObjectProperty mProp = new ScriptObjectProperty();
            mProp.Name = "maleItems";
            mProp.Flags |= ScriptProperty.Flag.Edited;
            mProp.Object = maleItems;
            UTinventoryFixScript.Properties.Add(mProp);

            ScriptObjectProperty fProp = new ScriptObjectProperty();
            fProp.Name = "femaleItems";
            fProp.Flags |= ScriptProperty.Flag.Edited;
            fProp.Object = femaleItems;
            UTinventoryFixScript.Properties.Add(fProp);

            utItemFixEffect.VirtualMachineAdapter.Scripts.Add(UTinventoryFixScript);

            // create Spell

            //the following does not fix the issue - check later if it's deletable
            
            if (!FormKey.TryFactory("013F44:skyrim.esm", out var equipTypeEitherHandKey) || equipTypeEitherHandKey.IsNull)
            {
                throw new Exception("Could not create FormKey 013F44:skyrim.esm");
            }
            if (!state.LinkCache.TryResolve<IEquipTypeGetter>(equipTypeEitherHandKey, out var equipTypeEitherHand) || equipTypeEitherHand == null)
            {
                throw new Exception("Could not resolve FormKey 013F44:skyrim.esm");
            }
            ///

            Spell utItemFixSpell = state.PatchMod.Spells.AddNew();
            utItemFixSpell.EditorID = "UT_SPEL_GenderedInventoryFix";
            utItemFixSpell.Name = "Fixes gendered UnderThere inventory";
            utItemFixSpell.CastType = CastType.ConstantEffect;
            utItemFixSpell.TargetType = TargetType.Self;
            utItemFixSpell.Type = SpellType.Ability;
            utItemFixSpell.EquipmentType = equipTypeEitherHandKey;
            Effect utItemFixShellEffect = new Effect();
            utItemFixShellEffect.BaseEffect = utItemFixEffect;
            utItemFixShellEffect.Data = new EffectData();
            utItemFixSpell.Effects.Add(utItemFixShellEffect);

            // distribute spell via SPID
            string distr = "Spell = " + utItemFixSpell.FormKey.IDString() + " - " + utItemFixSpell.FormKey.ModKey.ToString() + " | ActorTypeNPC | NONE | NONE | NONE";
            string destPath = Path.Combine(state.Settings.DataFolderPath, "UnderThereGenderedItemFix_DISTR.ini");
            try
            {
                File.WriteAllLines(destPath, new List<string> { distr });
            }

            catch
            {
                throw new Exception("Could not write " + destPath);
            }
        }

        public static Dictionary<string, List<FormKey>> getGenderedItems(List<UTSet> sets)
        {
            Dictionary<string, List<FormKey>> genderedItems = new Dictionary<string, List<FormKey>>();
            genderedItems["male"] = new List<FormKey>();
            genderedItems["female"] = new List<FormKey>();

            foreach (UTSet set in sets)
            {
                getGenderedItemsFromList(set.Items_Male, genderedItems["male"]);
                getGenderedItemsFromList(set.Items_Female, genderedItems["female"]);
            }

            //make sure that gendered items aren't mixed
            foreach (FormKey maleItem in genderedItems["male"])
            {
                if (genderedItems["female"].Contains(maleItem))
                {
                    throw new Exception("Error: found item " + maleItem.ToString() + " in both Items_Male and Items_Female. Please move it to Items_Mutual.");
                }
            }
            foreach (FormKey femaleItem in genderedItems["female"])
            {
                if (genderedItems["male"].Contains(femaleItem))
                {
                    throw new Exception("Error: found item " + femaleItem.ToString() + " in both Items_Male and Items_Female. Please move it to Items_Mutual.");
                }
            }

            return genderedItems;
        }

        public static void getGenderedItemsFromList(List<UTitem> items, List<FormKey> uniqueFormKeys)
        {
            foreach (UTitem item in items)
            {
                if (uniqueFormKeys.Contains(item.formKey) == false)
                {
                    uniqueFormKeys.Add(item.formKey);
                }
            }
        }

        public static List<BipedObjectFlag> getARMAslots(List<UTSet> sets, ILinkCache lk)
        {
            List<BipedObjectFlag> usedSlots = new List<BipedObjectFlag>();

            foreach (UTSet set in sets)
            {
                getContainedSlots(set.Items_Mutual, usedSlots, lk);
                getContainedSlots(set.Items_Male, usedSlots, lk);
                getContainedSlots(set.Items_Female, usedSlots, lk);
            }

            return usedSlots;
        }

        public static void patchBodyARMAslots(List<BipedObjectFlag> usedSlots, List<string> patchableRaces, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!FormKey.TryFactory("000019:Skyrim.esm", out var defaultRaceKey) || defaultRaceKey.IsNull)
            {
                throw new Exception("Could not get FormKey " + defaultRaceKey.ToString());
            }

            foreach (var arma in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
            {
                if (!state.LinkCache.TryResolve<IRaceGetter>(arma.Race.FormKey, out var armaRace) || armaRace == null || armaRace.EditorID == null || armaRace.EditorID.Contains("Child"))
                {
                    continue;
                }

                if (arma.Race.FormKey == defaultRaceKey || patchableRaces.Contains(armaRace.EditorID))
                { 
                    if (arma.BodyTemplate != null && arma.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body))
                    {
                        var patchedAA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(arma);
                        if (patchedAA.BodyTemplate == null) { continue; }
                        foreach (var uwSlot in usedSlots)
                        {
                            patchedAA.BodyTemplate.FirstPersonFlags |= uwSlot;
                        }
                    }
                }
            }
        }

        public static void reportARMAslots(List<BipedObjectFlag> usedSlots)
        {
            Console.WriteLine("The following slots are being used by underwear. Please make sure they don't conflict with any other modded armors.");
            foreach (var slot in usedSlots)
            {
                Console.WriteLine(mapSlotToInt(slot));
            }
        }

        public static void getContainedSlots(List<UTitem> items, List<BipedObjectFlag> usedSlots, ILinkCache lk)
        {
            foreach (UTitem item in items)
            {
                if (!lk.TryResolve<IArmor>(item.formKey, out var itemObj) || itemObj == null)
                {
                    continue;
                }

                foreach (IFormLink<IArmorAddonGetter> AAgetter in itemObj.Armature)
                {
                    if (!lk.TryResolve<IArmorAddon>(AAgetter.FormKey, out var ARMAobj) || ARMAobj == null || ARMAobj.BodyTemplate == null)
                    {
                        continue;
                    }

                    List<BipedObjectFlag> currentUsedSlots = getARMAslots(ARMAobj.BodyTemplate);
                    foreach (var usedFlag in currentUsedSlots)
                    {
                        if (usedSlots.Contains(usedFlag) == false)
                        {
                            usedSlots.Add(usedFlag);
                        }
                    }
                }
            }
        }

        public static void reportDeactivatablePlugins(List<string> plugins)
        {
            Console.WriteLine("The following plugins have been absorbed into the synthesis patch and may now be deactivated. Make sure to keep the associated meshes and textures enabled.");
            foreach (string p in plugins)
            {
                Console.WriteLine(p);
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
                default: return new BipedObjectFlag();
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
    }

    public class UTconfig
    {
        public string AssignmentMode { get; set; }
        public bool bPatchMales { get; set; }
        public bool bPatchFemales { get; set; }
        public bool bPatchNakedNPCs { get; set; }
        public bool bMakeItemsEquippable { get; set; }
        public List<string> PatchableRaces { get; set; }
        public Dictionary<string, List<string>> ClassDefinitions { get; set; }
        public Dictionary<string, List<string>> FactionDefinitions { get; set; }
        public List<string> IgnoreFactionsWhenScoring { get; set; }
        public List<NPCassignment> SpecificNPCs { get; set; }
        public Dictionary<string, List<string>> Assignments { get; set; }
        public List<UTSet> Sets { get; set; }

        public UTconfig()
        {
            AssignmentMode = "";
            bPatchMales = true;
            bPatchFemales = true;
            bPatchNakedNPCs = true;
            PatchableRaces = new List<string>();
            ClassDefinitions = new Dictionary<string, List<string>>();
            FactionDefinitions = new Dictionary<string, List<string>>();
            IgnoreFactionsWhenScoring = new List<string>();
            SpecificNPCs = new List<NPCassignment>();
            Assignments = new Dictionary<string, List<string>>();
            Sets = new List<UTSet>();
        }
    }

    public class UTSet
    {
        public string Name { get; set; }
        public List<UTitem> Items_Mutual { get; set; }
        public List<UTitem> Items_Male { get; set; }
        public List<UTitem> Items_Female { get; set; }
        public FormKey LeveledListFormKey { get; set; }

        public UTSet()
        {
            Name = "";
            Items_Mutual = new List<UTitem>();
            Items_Male = new List<UTitem>();
            Items_Female = new List<UTitem>();
            LeveledListFormKey = new FormKey();
        }
    }
    public class UTitem
    {
        public string Record { get; set; }

        public string DispName { get; set; }
        public float Weight { get; set; }
        public UInt32 Value { get; set; }
        public FormKey formKey { get; set; }
        public UTitem()
        {
            Record = "";
            DispName = "";
            Weight = 0;
            Value = 0;
            formKey = new FormKey();
        }
    }

    public class NPCassignment
    {
        public string Name { get; set; }
        public string FormKey { get; set; }
        public string Type { get; set; }
        public string Assignment_Set { get; set; }
        public string Assignment_Group { get; set; }
        public UTSet Assignment_Set_Obj { get; set; }
        public FormKey FormKeyObj { get; set; }
        public bool isNull { get; set; }
        public NPCassignment()
        {
            Name = "";
            FormKey = "";
            Type = "";
            Assignment_Set = "";
            Assignment_Group = "";
            Assignment_Set_Obj = new UTSet();
            FormKeyObj = new FormKey();
            isNull = true;
        }

        public static NPCassignment getSpecificNPC(FormKey fk, List<NPCassignment> assigments)
        {
            foreach (var assignment in assigments)
            {
               if (assignment.FormKeyObj == fk)
               {
                    return assignment;
               }
            }

            return new NPCassignment();
        }
    }
}
