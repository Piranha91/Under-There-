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
                        //IdentifyingModKey = "FoodRemover.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                });
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var settingsPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereConfig.json");
            //var settingsPath = "Data\\UnderThereConfig.json";

            UTconfig settings = new UTconfig();

            settings = JsonUtils.FromJson<UTconfig>(settingsPath);

            OutfitMapping OutfitMap = new OutfitMapping();

            //Your code here!
            if (validateSettings(settings) == false)
            {
                throw new Exception("Please fix the errors in the settings file and try again.");
            }
            
            createItems(settings.Sets, settings.bMakeItemsEquippable, settings.Slots, state.LinkCache, state.PatchMod);

            Dictionary<string, FormKey> UT_LeveledItemsByWealth = new Dictionary<string, FormKey>();
            FormKey UT_LeveledItemsAll = new FormKey();
            FormKey UT_DefaultItem = new FormKey();

            // created leveled item lists (to be added to outfits)
            if (settings.AssignmentMode.ToLower() == "random")
            {
                UT_LeveledItemsAll = createLeveledList_AllItems(settings.Sets, state.LinkCache, state.PatchMod);
            }
            else if (settings.AssignmentMode.ToLower() == "class" || settings.AssignmentMode.ToLower() == "faction")
            {
                UT_LeveledItemsByWealth = createLeveledList_ByWealth(settings.Sets, settings.Assignments, state.LinkCache, state.PatchMod);
            }
            else
            {
                UT_DefaultItem = getDefaultItemFormKey(settings.Sets, settings.Assignments, state.LinkCache, state.PatchMod);
            }
            
            // add leveled item lists to outfits
            if (settings.AssignmentMode.ToLower() == "class" || settings.AssignmentMode.ToLower() == "faction")
            {
                Dictionary<FormKey, string> outfitsByWealth = assignOutfitsByWealth(settings.Sets, settings.ClassDefinitions, settings.FactionDefinitions, settings.AssignmentMode, state);
                modifyOutfitsByWealth(outfitsByWealth, UT_LeveledItemsByWealth, UT_DefaultItem, state);
            }
            else if (settings.AssignmentMode.ToLower() == "random")
            {
                modifyOutfitsByRandom(UT_LeveledItemsAll, state);
            }
            else
            {
                modifyOutfitsByDefault(UT_DefaultItem, state);
            }

            // patch armor addons containing body slot to also contain underwear slots

            foreach (var aa in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
            {
                if (aa.BodyTemplate != null && aa.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body))
                {
                    var editedAA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(aa);
                    if (editedAA != null && editedAA.BodyTemplate != null)
                    {
                        foreach (int slot in settings.Slots)
                        {
                            editedAA.BodyTemplate.FirstPersonFlags |= mapIntToSlot(slot);
                        }
                    }
                }

            }

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
                    return set.FormKeyObject;
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
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference = set.FormKeyObject;
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                allItems.Entries.Add(entry);
            }

            return allItems.FormKey;
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
                        data.Reference = set.FormKeyObject;
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

        public static Dictionary<FormKey, string> assignOutfitsByWealth(List<UTSet> sets, Dictionary<string, List<string>> classDefinitions, Dictionary<string, List<string>> factionDefinitions, string mode, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<FormKey, string> outfitsByWealth = new Dictionary<FormKey, string>();

            Dictionary<FormKey, Dictionary<string, int>> outfitsByWealth_Tally = new Dictionary<FormKey, Dictionary<string, int>>();

            string npcWealthGroup = "";
            List<String> GroupLookupFailures = new List<string>();

            // intialize outfit wealth group tally lists
            foreach (var outfit in state.LoadOrder.PriorityOrder.WinningOverrides<IOutfitGetter>())
            {
                outfitsByWealth[outfit.FormKey] = "";
                outfitsByWealth_Tally[outfit.FormKey] = new Dictionary<string, int>();
                switch(mode)
                {
                    case "class":
                        foreach (KeyValuePair<string, List<string>> Def in classDefinitions)
                        {
                            outfitsByWealth_Tally[outfit.FormKey].Add(Def.Key, 0);
                        }
                        break;
                    case "faction":
                        foreach (KeyValuePair<string, List<string>> Def in factionDefinitions)
                        {
                            outfitsByWealth_Tally[outfit.FormKey].Add(Def.Key, 0);
                        }
                        break;
                }
            }

            // "score" each outfit by the number of NPCs wearing that outfit that fall into a given wealth group "bin"
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                // get the wealth of current NPC
                switch(mode)
                {
                    case "class":
                        if (state.LinkCache.TryResolve<IClassGetter>(npc.Class.FormKey, out var NPCclass) && NPCclass != null && NPCclass.EditorID != null)
                        {
                            npcWealthGroup = getWealthGroupByEDID(NPCclass.EditorID, classDefinitions, GroupLookupFailures);
                        }
                        break;
                    case "faction":
                        npcWealthGroup = getWealthGroupByFactions(npc, factionDefinitions, GroupLookupFailures, state);
                        break;
                }

                // get current NPC's outfit formKey and add its score
                if (npcWealthGroup != "Default" && state.LinkCache.TryResolve<IOutfitGetter>(npc.DefaultOutfit.FormKey, out var NPCoutfit) && NPCoutfit != null)
                {
                    outfitsByWealth_Tally[NPCoutfit.FormKey][npcWealthGroup]++;
                }
            }

            // assign the highest scoring wealth group to each outfit
            Dictionary<string, int> score = new Dictionary<string, int>();
            foreach (KeyValuePair<FormKey, Dictionary<string, int>> OutfitScores in outfitsByWealth_Tally)
            {
                score = OutfitScores.Value;
                int maxScore = score.Values.Max();

                if (maxScore == 0)
                {
                    Console.WriteLine("No wealth assignments could be made for outfit " + OutfitScores.Key.ToString() + ". Assigning default underwear.");
                    outfitsByWealth[OutfitScores.Key] = "Default";
                }
                else
                {
                    foreach (string wealthGroup in score.Keys)
                    {
                        if (score[wealthGroup] == maxScore)
                        {
                            outfitsByWealth[OutfitScores.Key] = wealthGroup;
                            break;
                        }
                    }
                }
            }

            return outfitsByWealth;
        }

        public static string getWealthGroupByFactions(INpcGetter npc, Dictionary<string, List<string>> factionDefinitions, List<string> GroupLookupFailures, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<string, int> wealthCounts = new Dictionary<string, int>();
            string tmpWealthGroup = "";

            // initialize wealth counts
            foreach (KeyValuePair<string, List<string>> Def in factionDefinitions)
            {
                wealthCounts.Add(Def.Key, 0);
            }

            // add each faction by appropriate wealth count
            foreach (Faction fact in npc.Factions)
            {
                if (fact.EditorID == null) { continue; }
                tmpWealthGroup = getWealthGroupByEDID(fact.EditorID, factionDefinitions, GroupLookupFailures);

                if (wealthCounts.ContainsKey(tmpWealthGroup))
                {
                    wealthCounts[tmpWealthGroup]++;
                }
            }

            // get the wealth group with the highest number of corresponding factions.
            int maxFactionsMatched = wealthCounts.Values.Max();
            foreach (string x in wealthCounts.Keys)
            {
                if (wealthCounts[x] == maxFactionsMatched)
                {
                    return x;
                }
            }

            // if no matches, return default
            return "Default";
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

        public static void modifyOutfitsByWealth(Dictionary<FormKey, string> outfitsByWealth, Dictionary<string, FormKey> UT_LeveledItemsByWealth, FormKey UT_DefaultItem, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var outfit in state.LoadOrder.PriorityOrder.WinningOverrides<IOutfitGetter>())
            {
                var editedOutfit = state.PatchMod.Outfits.GetOrAddAsOverride(outfit);

                if (editedOutfit != null && editedOutfit.Items != null)
                {
                    string wealthGroupForOutfit = outfitsByWealth[editedOutfit.FormKey];

                    if (wealthGroupForOutfit == "Default")
                    {
                        editedOutfit.Items.Add(UT_DefaultItem);
                    }
                    else
                    {
                        editedOutfit.Items.Add(UT_LeveledItemsByWealth[wealthGroupForOutfit]);
                    }
                }
            }
        }

        public static void modifyOutfitsByRandom(FormKey UT_LeveledItemsAll, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var outfit in state.LoadOrder.PriorityOrder.WinningOverrides<IOutfitGetter>())
            {
                var editedOutfit = state.PatchMod.Outfits.GetOrAddAsOverride(outfit);

                if (editedOutfit != null && editedOutfit.Items != null)
                {
                    editedOutfit.Items.Add(UT_LeveledItemsAll);
                }
            }
        }
        public static void modifyOutfitsByDefault(FormKey UT_DefaultItem, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var outfit in state.LoadOrder.PriorityOrder.WinningOverrides<IOutfitGetter>())
            {
                var editedOutfit = state.PatchMod.Outfits.GetOrAddAsOverride(outfit);

                if (editedOutfit != null && editedOutfit.Items != null)
                {
                    editedOutfit.Items.Add(UT_DefaultItem);
                }
            }
        }

        public static bool validateSettings(UTconfig settings)
        {
            if (settings == null)
            {
                return false;
            }
            if (settings.Slots == null)
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

            return true;
        }



        public static void createItems(List<UTSet> Sets, bool bMakeItemsEquipable, List<int> slots, ILinkCache lk, ISkyrimMod PatchMod)
        {
            deepCopyItems(Sets, lk, PatchMod); // copy all armor records along with their linked subrecords into PatchMod to get rid of dependencies on the original plugins. Sets[i].FormKeyObject will now point to the new FormKey in PatchMod

            List<UTSet> toRemove = new List<UTSet>();

            foreach (var Set in Sets)
            {
                // Get the first armor in the set. 
                if (lk.TryResolve<IArmor>(Set.FormKeyObject, out var moddedItem))
                {
                    // set slots in armor
                    if (moddedItem != null && moddedItem.BodyTemplate != null)
                    {
                        foreach (int slot in slots)
                        {
                            moddedItem.BodyTemplate.FirstPersonFlags |= mapIntToSlot(slot);
                        }
                        moddedItem.Name = Set.DispName;
                        moddedItem.EditorID = Set.Name;
                        moddedItem.Weight = Set.Weight;
                        moddedItem.Value = Convert.ToUInt32(Set.Value);
                        if (bMakeItemsEquipable == false)
                        {
                           moddedItem.MajorFlags |= Armor.MajorFlag.NonPlayable;
                        }

                        // if there is more than one item in the set, copy the rest as additional armor addons
                        foreach (IFormLink < IArmorAddonGetter> additionalARMA_FL in Set.AdditionalAAs)
                        {
                            moddedItem.Armature.Add(additionalARMA_FL);
                        }

                        // set armor addon slots
                        foreach (var aaInList in moddedItem.Armature)
                        {
                            if (!lk.TryResolve<IArmorAddonGetter>(aaInList.FormKey, out var AAhandle) || AAhandle == null || AAhandle.BodyTemplate == null)
                            {
                                continue;
                            }

                            var AA = PatchMod.ArmorAddons.GetOrAddAsOverride(AAhandle);
                            if (AA.BodyTemplate == null)
                            {
                                continue;
                            }

                            foreach (int slot in slots)
                            {
                                AA.BodyTemplate.FirstPersonFlags |= mapIntToSlot(slot);
                            }
                        }
                    }
                }
            }
              

            foreach (UTSet i in toRemove)
            {
                Sets.Remove(i);
            }        
        }

        public static void deepCopyItems(List<UTSet> Sets, ILinkCache lk, ISkyrimMod PatchMod)
        {
            List<UTSet> toRemove = new List<UTSet>();
            var recordsToDup = new HashSet<FormLinkInformation>();

            Dictionary<UTSet, List<FormKey>> additionalAAformkeys = new Dictionary<UTSet, List<FormKey>>();

            foreach (var Set in Sets)
            {
                additionalAAformkeys[Set] = new List<FormKey>();
                for (int i = 0; i < Set.Items.Count; i++)
                {
                    var item = Set.Items[i];
                    if (FormKey.TryFactory(item, out var origFormKey) && !origFormKey.IsNull)
                    {
                        if (!lk.TryResolve<IArmorGetter>(origFormKey, out var origItem))
                        {
                            Console.WriteLine("Could not find item with formKey " + origFormKey);
                            toRemove.Add(Set); // remove this set from the list
                            break;
                        }

                        foreach (FormLinkInformation FLI in origItem.ContainedFormLinks)
                        {
                            recordsToDup.Add(FLI);
                        }

                        if (i == 0)
                        {
                            recordsToDup.Add(origItem.ToFormLinkInformation());
                        }
                        else
                        {
                            foreach (var aa in origItem.Armature)
                            {
                                additionalAAformkeys[Set].Add(aa.FormKey);
                            }
                        }
                    }
                }
            }

            foreach (UTSet i in toRemove)
            {
                Sets.Remove(i);
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
            // note: Only the first armor in the set needs to be remapped - the rest will have their armatures added to the first armor
            foreach (UTSet set in Sets)
            {
                FormKey.TryFactory(set.Items[0], out var itemFormKey);
                set.FormKeyObject = remap[itemFormKey];

                // store additional armor addons

                foreach (FormKey origAAformKey in additionalAAformkeys[set])
                {
                    if (remap.ContainsKey(origAAformKey) && lk.TryResolve<IArmorAddonGetter>(remap[origAAformKey], out var additionalARMA_FL)) // should always be true but checking just in case
                    {
                        set.AdditionalAAs.Add(additionalARMA_FL);
                    }
                }
            }
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
    }


   

    public class UTconfig
    {
        public string AssignmentMode { get; set; }
        public bool bMakeItemsEquippable { get; set; }
        public List<int> Slots { get; set; }
        public List<string> PatchableRaces { get; set; }
        public Dictionary<string, List<string>> ClassDefinitions { get; set; }
        public Dictionary<string, List<string>> FactionDefinitions { get; set; }
        public Dictionary<string, List<string>> Assignments { get; set; }
        public List<UTSet> Sets { get; set; }

        public UTconfig()
        {
            AssignmentMode = "";
            Slots = new List<int>();
            PatchableRaces = new List<string>();
            ClassDefinitions = new Dictionary<string, List<string>>();
            FactionDefinitions = new Dictionary<string, List<string>>();
            Assignments = new Dictionary<string, List<string>>();
            Sets = new List<UTSet>();
        }
    }

    public class UTassignment
    {
        public List<string> Default { get; set; }
        public List<string> Poor { get; set; }
        public List<string> Medium { get; set; }
        public List<string> Rich { get; set; }

        public UTassignment()
        {
            Default = new List<string>();
            Poor = new List<string>();
            Medium = new List<string>();
            Rich = new List<string>();
        }
    }

    public class UTSet
    {
        public string Name { get; set; }
        public string DispName { get; set; }
        public float Weight { get; set; }
        public float Value { get; set; }
        public List<string> Items { get; set; }
        public FormKey FormKeyObject { get; set; }

        public List<IFormLink<IArmorAddonGetter>> AdditionalAAs { get; set; }

        public UTSet()
        {
            Name = "";
            DispName = "Undergarments";
            Weight = 0;
            Value = 0;
            Items = new List<string>();
            FormKeyObject = new FormKey();
            AdditionalAAs = new List<IFormLink<IArmorAddonGetter>>();
        }
    }
    public class UTitemset
    {
        public string Name { get; set; }
        
        public List<string> Members { get; set; }

        public UTitemset()
        {
            Name = "";
            Members = new List<string>();
        }
    }

    public class OutfitMapping
    {
        public Dictionary<FormKey, List<NewOutfitContainer>> Male { get; set; }
        public Dictionary<FormKey, List<NewOutfitContainer>> Female { get; set; }

        public OutfitMapping()
        {
            Male = new Dictionary<FormKey, List<NewOutfitContainer>>();
            Female =new  Dictionary<FormKey, List<NewOutfitContainer>>();
        }
    }

    public class NewOutfitContainer
    {
        public string itemSet { get; set; }
        public FormKey FormKey { get; set; }

        public NewOutfitContainer()
        {
            itemSet = "";
            FormKey = new FormKey();
        }
    }
}
