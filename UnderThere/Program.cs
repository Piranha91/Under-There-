using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
using UnderThere.Settings;

namespace UnderThere
{
    public class Program
    {
        const string Default = "Default";
        static Lazy<UTconfig> Settings = null!;
        static Lazy<Random> Random = new Lazy<Random>(() => new Random(Settings.Value.RandomSeed));

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "UnderThereConfig.json",
                    out Settings)
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
            string SPIDpath = Path.Combine(state.Settings.DataFolderPath, "skse\\plugins\\po3_SpellPerkItemDistributor.dll");
            if (!File.Exists(SPIDpath)) //SPIDtest (dual-level pun - whoa!)
            {
                throw new Exception("Spell Perk Item Distributor was not detected at " + SPIDpath + "\nAborting patch");
            }

            UTconfig settings = Settings.Value;

            Validator.validateSettings(settings);

            // create underwear items
            var UWsourcePlugins = new HashSet<ModKey>(); // set of source mod names for the underwear (to report to user so that they can be disabled)
            ItemImport.createItems(settings, UWsourcePlugins, state);

            // created leveled item lists (to be added to outfits)
            FormLink<ILeveledItemGetter> UT_LeveledItemsAll = createLeveledList_AllItems(settings.AllSets, state.LinkCache, state.PatchMod);
            Dictionary<string, FormLink<ILeveledItemGetter>> UT_LeveledItemsByWealth = createLeveledList_ByWealth(settings.AllSets, state.PatchMod);

            // modify NPC outfits
            assignOutfits(settings, settings.DefaultSet.LeveledList, UT_LeveledItemsByWealth, UT_LeveledItemsAll, state);

            // Add slots used by underwear items to clothes and armors with 32 - Body slot active
            List<BipedObjectFlag> usedSlots = Auxil.getItemSetARMAslots(settings.AllSets, state.LinkCache);
            patchBodyARMAslots(usedSlots, settings.PatchableRaces, state, settings.VerboseMode);

            // set SOS compatibiilty if needed
            bool bSOS = addSOScompatibility(settings.AllSets, usedSlots, state);

            // create and distribute gendered item inventory spell 
            copyUTScript(state);
            createInventoryFixSpell(settings.AllSets, state);

            // message user
            reportARMAslots(usedSlots, bSOS);
            reportDeactivatablePlugins(UWsourcePlugins);

            Console.WriteLine("\nDon't forget to install Spell Perk Item Distributor to properly manage gender-specific items.");
            Console.WriteLine("\nEnjoy the underwear. Goodbye.");
        }

        public static FormLink<ILeveledItemGetter> createLeveledList_AllItems(IEnumerable<UTSet> sets, ILinkCache lk, ISkyrimMod PatchMod)
        {
            var allItems = PatchMod.LeveledItems.AddNew();
            allItems.EditorID = "UnderThereAllItems";
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

        public static Dictionary<string, FormLink<ILeveledItemGetter>> createLeveledList_ByWealth(IEnumerable<UTSet> sets, ISkyrimMod PatchMod)
        {
            var itemsByWealth = new Dictionary<string, FormLink<ILeveledItemGetter>>();

            foreach (var group in sets.GroupBy(s => s.Category))
            {
                itemsByWealth[group.Key] = CreateLList(group.Key, group, PatchMod);
            }

            return itemsByWealth;
        }

        public static LeveledItem CreateLList(string nickname, IEnumerable<UTSet> sets, ISkyrimMod PatchMod)
        {
            var currentItems = PatchMod.LeveledItems.AddNew();
            currentItems.EditorID = "UnderThereItems_" + nickname;
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

        public static void assignOutfits(UTconfig settings, FormLink<ILeveledItemGetter> UT_DefaultItem, Dictionary<string, FormLink<ILeveledItemGetter>> UT_LeveledItemsByWealth, FormLink<ILeveledItemGetter> UT_LeveledItemsAll, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string npcGroup = "";
            FormKey currentOutfitKey = FormKey.Null;
            FormLink<ILeveledItemGetter> currentUW = FormKey.Null;
            var GroupLookupFailures = new HashSet<IFormLink>();
            List<string> NPClookupFailures = new List<string>();
            Dictionary<FormKey, Dictionary<string, Outfit>> OutfitMap = new Dictionary<FormKey, Dictionary<string, Outfit>>();

            Outfit underwearOnly = state.PatchMod.Outfits.AddNew();
            underwearOnly.EditorID = "No_Clothes";
            underwearOnly.Items = new ExtendedList<IFormLink<IOutfitTargetGetter>>();

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                NPCassignment specificAssignment = NPCassignment.getSpecificNPC(npc.FormKey, settings.SpecificNPCs);

                // check if NPC race should be patched
                bool isInventoryTemplate = !npc.DefaultOutfit.IsNull && !npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Inventory);
                bool isGhost = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsGhost)
                    || npc.Voice.FormKey == Skyrim.VoiceType.FemaleUniqueGhost
                    || npc.Voice.FormKey == Skyrim.VoiceType.MaleUniqueGhost
                    || Auxil.hasGhostAbility(npc) 
                    || Auxil.hasGhostScript(npc);

                if (!state.LinkCache.TryResolve<IRaceGetter>(npc.Race.FormKey, out var currentRace) ||
                    currentRace.EditorID == null ||
                    settings.NonPatchableRaces.Contains(currentRace) ||
                    Auxil.isNonHumanoid(npc, currentRace, state.LinkCache) ||
                    (!settings.PatchSummonedNPCs && npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Summonable)) ||
                    (!settings.PatchGhosts && isGhost) ||
                    currentRace.EditorID.Contains("Child", StringComparison.OrdinalIgnoreCase) ||
                    (!settings.PatchableRaces.Contains(currentRace) && !isInventoryTemplate) ||
                    NPCassignment.isBlocked(npc.FormKey, settings.BlockedNPCs))
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
                if (npc.FormKey == Skyrim.Npc.Player  || npc.FormKey == Skyrim.Npc.PlayerInventory)
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
                    else if (!settings.PatchNakedNPCs && specificAssignment.isNull)
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
                    if (state.LinkCache.TryResolve<IOutfitGetter>(npc.DefaultOutfit.FormKey, out var NPCoutfit) && NPCoutfit.Items != null && NPCoutfit.Items.Count == 0 && !settings.PatchNakedNPCs)
                    {
                        continue;
                    }
                }

                var NPCoverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                if (!specificAssignment.isNull)
                {
                    switch (specificAssignment.Type)
                    {
                        case NpcAssignmentType.Set:
                            npcGroup = specificAssignment.Assignment_Set;
                            currentUW = specificAssignment.AssignmentSet_Obj.LeveledList;
                            break;
                        case NpcAssignmentType.Group:
                            npcGroup = specificAssignment.Assignment_Group;
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
                            if (npc.FormKey == Skyrim.Npc.Hroki)
                            {
                                npcGroup = Default; // hardcoded due to a particular idiosyncratic issue caused by Bethesda's weird choice of Class for Hroki.
                                break;
                            }
                            npcGroup = getWealthGroup(npc.Class, settings.ClassDefinitions, GroupLookupFailures);
                            currentUW = UT_LeveledItemsByWealth[npcGroup];
                            if (npcGroup == Default) { NPClookupFailures.Add(npc.EditorID + " (" + npc.FormKey.ToString() + ")"); }
                            break;
                        case AssignmentMode.Faction:
                            npcGroup = getWealthGroupByFactions(npc, settings.FactionDefinitions, settings.FallBackFactionDefinitions, settings.IgnoreFactionsWhenScoring, GroupLookupFailures);
                            currentUW = UT_LeveledItemsByWealth[npcGroup];
                            if (npcGroup == Default) { NPClookupFailures.Add(npc.EditorID + " (" + npc.FormKey.ToString() + ")"); }
                            break;
                        case AssignmentMode.Random:
                            npcGroup = "Random";
                            currentUW = UT_LeveledItemsAll;
                            break;
                    }
                }

                // if the current outfit modified by the current wealth group doesn't exist, create it
                if (!OutfitMap.ContainsKey(currentOutfitKey) || !OutfitMap[currentOutfitKey].ContainsKey(npcGroup))
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
                        newOutfit.Items.Add(currentUW);
                    }
                    if (!OutfitMap.ContainsKey(currentOutfitKey))
                    {
                        OutfitMap[currentOutfitKey] = new Dictionary<string, Outfit>();
                    }
                    OutfitMap[currentOutfitKey][npcGroup] = newOutfit;
                }

                NPCoverride.DefaultOutfit = OutfitMap[currentOutfitKey][npcGroup]; // assign the correct outfit to the current NPC
            }

            //report failed lookups
            if (GroupLookupFailures.Count > 0 || NPClookupFailures.Count > 0)
            {
                Auxil.LogDefaultNPCs(NPClookupFailures, GroupLookupFailures, state.ExtraSettingsDataPath);
            }
        }

        public static string getWealthGroupByFactions(INpcGetter npc, Dictionary<string, HashSet<FormLink<IFactionGetter>>> factionDefinitions, Dictionary<string, HashSet<FormLink<IFactionGetter>>> fallbackFactionDefinitions, HashSet<FormLink<IFactionGetter>> ignoredFactions, HashSet<IFormLink> GroupLookupFailures)
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
                if (ignoredFactions.Contains(fact.Faction))
                {
                    wealthCounts[Default]++; // "Default" will be ignored if other factions are matched
                    continue;
                }

                tmpWealthGroup = getWealthGroup(fact.Faction, factionDefinitions, GroupLookupFailures);

                if (wealthCounts.ContainsKey(tmpWealthGroup))
                {
                    wealthCounts[tmpWealthGroup]++;
                }

                if (tmpWealthGroup == Default) // check fallback factions
                {
                    tmpWealthGroup = getWealthGroup(fact.Faction, fallbackFactionDefinitions, GroupLookupFailures);
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

            // fallback if NPC has no factions
            if (npc.Factions == null || npc.Factions.Count == 0)
            {
                tmpWealthGroup = Settings.Value.QualityForNoFaction;
                if (wealthCounts.ContainsKey(tmpWealthGroup))
                {
                    wealthCounts[tmpWealthGroup]++;
                }

                if (tmpWealthGroup == Default)
                {
                    tmpWealthGroup = Settings.Value.QualityForNoFactionFallback;
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

            // return the wealth group that was matched to the highest number of factions (choose random if tied)
            return bestMatches[Random.Value.Next(bestMatches.Count)];
        }

        public static string getWealthGroup<T>(FormLink<T> link, Dictionary<string, HashSet<FormLink<T>>> Definitions, HashSet<IFormLink> GroupLookupFailures)
            where T : class, IMajorRecordCommonGetter
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

        public static void copyUTScript(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string UTscriptPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereGenderedItemFix.pex");

            if (!File.Exists(UTscriptPath))
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

        public static void createInventoryFixSpell(IEnumerable<UTSet> sets, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // get all gendered items
            var genderedItems = getGenderedItems(sets);

            // create gendered item FormLists
            FormList maleItems = state.PatchMod.FormLists.AddNew();
            maleItems.EditorID = "UT_FLST_MaleOnly";
            foreach (var fk in genderedItems.Male)
            {
                maleItems.Items.Add(fk);
            }

            FormList femaleItems = state.PatchMod.FormLists.AddNew();
            femaleItems.EditorID = "UT_FLST_FemaleOnly";
            foreach (var fk in genderedItems.Female)
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
            Spell utItemFixSpell = state.PatchMod.Spells.AddNew();
            utItemFixSpell.EditorID = "UT_SPEL_GenderedInventoryFix";
            utItemFixSpell.Name = "Fixes gendered UnderThere inventory";
            utItemFixSpell.CastType = CastType.ConstantEffect;
            utItemFixSpell.TargetType = TargetType.Self;
            utItemFixSpell.Type = SpellType.Ability;
            utItemFixSpell.EquipmentType = Skyrim.EquipType.EitherHand;
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

        public static (HashSet<FormLink<IArmorGetter>> Male, HashSet<FormLink<IArmorGetter>> Female) getGenderedItems(IEnumerable<UTSet> sets)
        {
            var male = new HashSet<FormLink<IArmorGetter>>();
            var female = new HashSet<FormLink<IArmorGetter>>();

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

        public static void patchBodyARMAslots(List<BipedObjectFlag> usedSlots, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaces, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, bool bVerboseMode)
        {
            foreach (var arma in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
            {
                if (!state.LinkCache.TryResolve<IRaceGetter>(arma.Race.FormKey, out var armaRace) || armaRace.EditorID == null || armaRace.EditorID.Contains("Child", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (arma.Race.FormKey == Skyrim.Race.DefaultRace || patchableRaces.Contains(armaRace.AsLink()))
                {
                    if (arma.BodyTemplate != null && arma.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body))
                    {
                        if (bVerboseMode)
                        {
                            Console.WriteLine("Patching armor addon: {0}", arma.FormKey.ToString());
                        }
                        var patchedAA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(arma);
                        if (patchedAA.BodyTemplate == null) continue;
                        foreach (var uwSlot in usedSlots)
                        {
                            try
                            {
                                patchedAA.BodyTemplate.FirstPersonFlags |= uwSlot;
                                if (bVerboseMode)
                                {
                                    Console.WriteLine("added slot {0}", Auxil.mapSlotToInt(uwSlot));
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Failed to add slot {0} to armor addon {1}", Auxil.mapSlotToInt(uwSlot), arma.FormKey.ToString());
                            }
                        }
                    }
                }
            }
        }

        public static void reportARMAslots(List<BipedObjectFlag> usedSlots, bool bSOS)
        {
            Console.WriteLine("\nThe following slots are being used by underwear. Please make sure they don't conflict with any other modded armors.");
            foreach (var slot in usedSlots)
            {
                Console.WriteLine(Auxil.mapSlotToInt(slot));
            }
            if (bSOS)
            {
                Console.WriteLine("52 (Inserted by patcher for SOS Compatibility)");
            }
        }

        public static void reportDeactivatablePlugins(IEnumerable<ModKey> plugins)
        {
            Console.WriteLine("\nThe following plugins have been absorbed into the synthesis patch and may now be deactivated. Make sure to keep the associated meshes and textures enabled.");
            foreach (var p in plugins)
            {
                Console.WriteLine(p);
            }
        }

        public static bool addSOScompatibility(IEnumerable<UTSet> sets, List<BipedObjectFlag> usedSlots, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            bool bSOSdetected = false;
            foreach (var mod in state.LoadOrder)
            {
                if (mod.Key.FileName == "Schlongs of Skyrim - Core.esm")
                {
                    bSOSdetected = true;
                    break;
                }
            }
            if (!bSOSdetected)
            {
                return false;
            }

            // check to make sure no current armor addons use slot 52
            foreach (var slot in usedSlots)
            {
                if (Auxil.mapSlotToInt(slot) == 52)
                {
                    throw new Exception("Schlongs of Skyrim has been detected, and one of your imported underwear items is slot 52. This will cause a clothing conflict in-game. Please edit the offending item, changing both the armor addon AND the nif file to a slot other than 52 (49 is recommended).");
                }
            }

            // patch all bottoms to use slot 52
            foreach (var set in sets)
            {
                addSOSslot(set.Items, state);
            }
            return true;
        }

        public static void addSOSslot(List<UTitem> items, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
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
                                moddedAA_override.BodyTemplate.FirstPersonFlags |= Auxil.mapIntToSlot(52);
                            }
                        }
                    }
                }
            }
        }
    }
}
