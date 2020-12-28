using System;
using System.Collections.Generic;
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
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                userPreferences: new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "UnderThere.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                        BlockAutomaticExit = true,
                    }
                });
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //var settingsPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereConfig.json");
            var settingsPath = "Data\\UnderThereConfig.json";

            UTconfig settings = new UTconfig();

            settings = JsonUtils.FromJson<UTconfig>(settingsPath);

            OutfitMapping OutfitMap = new OutfitMapping();

            //Your code here!
            if (validateSettings(settings) == false)
            {
                Console.WriteLine("Please fix the errors in the settings file and try again.");
                Console.ReadLine();
                return;
            }

            createItems(settings.Items, settings.bMakeItemsEquippable, settings.Slots, state.LinkCache, state.PatchMod);

            Dictionary<string, List<string>> ClassLookupFailures = new Dictionary<string, List<string>>();
            string gender = "";

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                //check if NPC has a template
                if (npc.Template.TryResolve<INpcSpawnGetter>(state.LinkCache, out var npcTemplate) && npcTemplate != null)
                {

                }

                // check if NPC race is patchable
                if (npc.Race.TryResolve<IRaceGetter>(state.LinkCache, out var NPCrace) && npc != null && NPCrace.EditorID != null && settings.PatchableRaces.Contains(NPCrace.EditorID))
                {
                    var moddedNPC = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    // check which ClassDefinition this NPC belongs to
                    if (npc.Class.TryResolve<IClassGetter>(state.LinkCache, out var NPCclass) && NPCclass != null && NPCclass.EditorID != null)
                    {
                        string assignmentClass = GetClassDef(NPCclass.EditorID, settings.ClassDefinitions, settings.bAssignByClass, npc, ClassLookupFailures);

                        // get NPC's gender
                        if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                        {
                            gender = "female";
                            if (settings.bPatchFemales == false)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            gender = "male";
                            if (settings.bPatchMales == false)
                            {
                                continue;
                            }
                        }

                        // Get NPC's current outfit
                        if (npc.DefaultOutfit.TryResolve<IOutfitGetter>(state.LinkCache, out var NPCoutfit) && NPCoutfit != null)
                        {
                            // pick an item set based on NPC's gender and assignmentClass
                            string chosenItemSet = chooseItemSet(gender, assignmentClass, settings.Assignments);

                            // check if the given outfit + itemset combination has already been assigned
                            FormKey chosenOutfit = getModifiedOutfit(gender, chosenItemSet, NPCoutfit.FormKey, OutfitMap);
                            if (chosenOutfit == NPCoutfit.FormKey) // getModifiedOutfitKey returns the original outfit formlink if it can't find a modified one in the mapping
                            {
                                chosenOutfit = createModifiedOutfit(gender, chosenItemSet, NPCoutfit, settings, OutfitMap, state.PatchMod);
                                moddedNPC.DefaultOutfit = chosenOutfit;
                            }
                            else
                            {
                                moddedNPC.DefaultOutfit = chosenOutfit;
                            }

                            Console.WriteLine($"{moddedNPC.Name} ({moddedNPC.EditorID}) was assigned outfit {chosenOutfit.ToString()}");
                        }
                    }
                }
            }

            if (ClassLookupFailures.Count > 0)
            {
                Console.WriteLine("Some NPCs' classes were not defined in the json settings file and were therefore assigned to the Medium group.");
                foreach (string nC in ClassLookupFailures.Keys)
                {
                    Console.WriteLine(nC + ":");
                    foreach (string label in ClassLookupFailures[nC])
                    {
                        Console.WriteLine("\t" + label);
                    }
                    Console.WriteLine();
                }
            }

            // add flags to all Body Armor Addons to prevent clipping
            foreach (var AA in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorAddonGetter>())
            {
                if (AA.BodyTemplate != null && AA.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body))
                {
                    var editedAA = state.PatchMod.ArmorAddons.GetOrAddAsOverride(AA);
                    if (editedAA.BodyTemplate != null)
                    {
                        editedAA.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(settings.Slots.Bottom);
                        editedAA.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(settings.Slots.Top);
                    }
                }
            }

            Console.WriteLine("\nEnjoy the underwear. Goodbye.");
        }

        public static string getAssignmentForTemplatedNPC(INpcSpawnGetter template)
        {


            return "";
        }

        public static FormKey createModifiedOutfit(string gender, string itemSet, IOutfitGetter origOutfit, UTconfig config, OutfitMapping map, ISkyrimMod PatchMod)
        {
            var moddedOutfit = PatchMod.Outfits.AddNew(); // Create new record, /w new FormKey
            moddedOutfit.DeepCopyIn(origOutfit);

            List<UTitemset> availableSets = new List<UTitemset>();
            List<string> chosenItems = new List<string>();

            switch(gender)
            {
                case "male":
                    availableSets = config.Sets.Male;
                    moddedOutfit.EditorID += "_M";
                    break;
                case "female":
                    availableSets = config.Sets.Female;
                    moddedOutfit.EditorID += "_F";
                    break;
            }

            moddedOutfit.EditorID += "_" + itemSet;
            
            foreach (UTitemset set in availableSets)
            {
                if (set.Name == itemSet)
                {
                    chosenItems = set.Members;
                    break;
                }
            }

            foreach (string item in chosenItems)
            {
                foreach(UTitem uTitem in config.Items)
                {
                    if (uTitem.Name == item && moddedOutfit.Items != null)
                    {
                        moddedOutfit.Items.Add(uTitem.FormKeyObject);
                        break;
                    }
                }
            }

            // add outfit to the dictionary
            Dictionary<FormKey, List<NewOutfitContainer>> mapping = new Dictionary<FormKey, List<NewOutfitContainer>>();
            switch(gender)
            {
                case "male":
                    mapping = map.Male;
                    break;
                case "female":
                    mapping = map.Female;
                    break;
            }

            NewOutfitContainer NOC = new NewOutfitContainer();
            NOC.itemSet = itemSet;
            NOC.FormKey = moddedOutfit.FormKey;
            
            if (mapping.ContainsKey(origOutfit.FormKey))
            {
                mapping[origOutfit.FormKey].Add(NOC);
            }
            else
            {
                mapping.Add(origOutfit.FormKey, new List<NewOutfitContainer>() { NOC });
            }

            return moddedOutfit.FormKey;
        }

        public static FormKey getModifiedOutfit(string gender, string itemSet, FormKey origFormKey, OutfitMapping map)
        {
            Dictionary<FormKey, List<NewOutfitContainer>> toSearch = new Dictionary<FormKey, List<NewOutfitContainer>>();

            switch(gender)
            {
                case "male":
                    toSearch = map.Male;
                    break;
                case "female":
                    toSearch = map.Female;
                    break;
            }

            if (toSearch.ContainsKey(origFormKey))
            {
                foreach (NewOutfitContainer NOC in toSearch[origFormKey])
                {
                    if (NOC.itemSet == itemSet)
                    {
                        return NOC.FormKey;
                    }
                }
            }

            return origFormKey;
        }

        public static string chooseItemSet(string gender, string classAssignment, UTassignmentContainer Assignments)
        {
            UTassignment possibleAssignments = new UTassignment();
            switch(gender)
            {
                case "male":
                    possibleAssignments = Assignments.Male;
                    break;
                case "female":
                    possibleAssignments = Assignments.Female;
                    break;
            }

            List<string> availableItemSets = new List<string>();
            switch(classAssignment)
            {
                case "Default":
                    availableItemSets = possibleAssignments.Default;
                    break;
                case "Poor":
                    availableItemSets = possibleAssignments.Poor;
                    break;
                case "Medium":
                    availableItemSets = possibleAssignments.Medium;
                    break;
                case "Rich":
                    availableItemSets = possibleAssignments.Rich;
                    break;
            }

            // choose random index
            Random r = new Random();
            int rIndex = r.Next(0, availableItemSets.Count);
            return availableItemSets[rIndex];
        }

        public static string GetClassDef(string classEDID, UTclassDef ClassDefinitions, bool bAssignByClass, INpcGetter npc, Dictionary<string, List<string>> ClassLookupFailures)
        {
            string classDef = "";

            if (bAssignByClass == false)
            {
                classDef = "Default";
            }
            else
            {
                if (ClassDefinitions.Poor.Contains(classEDID))
                {
                    classDef = "Poor";
                }
                else if (ClassDefinitions.Medium.Contains(classEDID))
                {
                    classDef = "Medium";
                }
                else if (ClassDefinitions.Rich.Contains(classEDID))
                {
                    classDef = "Rich";
                }
                else
                {
                    //Console.WriteLine("Warning: Class " + classEDID + " was not assigned to any of ther (Poor/Medium/Rich) categories. Assigning Medium to NPCs of this class.");
                    string label = npc?.Name?.ToString() ?? "No Name";

                    if (ClassLookupFailures.ContainsKey(classEDID) == false)
                    {
                        ClassLookupFailures.Add(classEDID, new List<string> { label + " (" + npc?.EditorID + ")" });
                    }
                    else
                    {
                        ClassLookupFailures[classEDID].Add(label + " (" + npc?.EditorID + ")");
                    }

                    classDef = "Medium";
                }
            }

            return classDef;
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

            if (settings.Assignments == null)
            {
                return false;
            }

            if (settings.Sets == null)
            {
                return false;
            }

            if (settings.Items == null)
            {
                return false;
            }

            return true;
        }

        public static void createItems(List<UTitem> Items, bool bMakeItemsEquipable, UTslots slots, ILinkCache lk, ISkyrimMod PatchMod)
        {
            var recordsToDup = new HashSet<FormLinkInformation>();

            List<UTitem> toRemove = new List<UTitem>();
            foreach (var item in Items)
            {
                if (FormKey.TryFactory(item.FormKey, out var formKey) && !formKey.IsNull)
                {
                    // If conversion successful
                    if (!lk.TryResolve<IArmorGetter>(formKey, out var origItem))
                    {
                        Console.WriteLine("Could not find item with formKey " + formKey);
                        toRemove.Add(item);
                    }
                    else
                    {
                        item.FormKeyObject = formKey;

                        var moddedItem = PatchMod.Armors.AddNew(); // Create new record, /w new FormKey
                        moddedItem.DeepCopyIn(origItem); // Copy some data from another record
                        moddedItem.Name = item.DispName;
                        item.FormKeyObject = moddedItem.FormKey;

                        if (bMakeItemsEquipable == false)
                        {
                            moddedItem.MajorFlags |= Armor.MajorFlag.NonPlayable;
                        }

                        // set slots in armor
                        if (moddedItem.BodyTemplate != null)
                        {
                            moddedItem.BodyTemplate.FirstPersonFlags = new BipedObjectFlag();
                            switch(item.Type)
                            {
                                case "Bottom":
                                    moddedItem.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(slots.Bottom);
                                    break;
                                case "Top":
                                    moddedItem.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(slots.Top);
                                    break;
                            }
                        }

                        // set slots in armor addon
                        List<IFormLink<IArmorAddonGetter>> newAAs = new List<IFormLink<IArmorAddonGetter>>();
                        foreach (IFormLink<IArmorAddonGetter> aa in moddedItem.Armature)
                        {
                            var newAA = PatchMod.ArmorAddons.AddNew();
                            
                            if (lk.TryResolve<IArmorAddonGetter>(aa.FormKey, out var origAA))
                            {
                                newAA.DeepCopyIn(origAA);
                                if (newAA.BodyTemplate != null)
                                {
                                    newAA.BodyTemplate.FirstPersonFlags = new BipedObjectFlag();

                                    switch (item.Type)
                                    {
                                        case "Bottom":
                                            newAA.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(slots.Bottom);
                                            break;
                                        case "Top":
                                            newAA.BodyTemplate.FirstPersonFlags |= UTslots.mapIntToSlot(slots.Top);
                                            break;
                                    }
                                }
                                newAAs.Add(newAA);
                            }
                        }

                        moddedItem.Armature.Clear();
                        foreach (IFormLink<IArmorAddonGetter> nAA in newAAs)
                        {
                            moddedItem.Armature.Add(nAA);
                        }

                        //copy any remaining records from the source mod
                        recordsToDup.Add(moddedItem.ToFormLinkInformation());
                        foreach (var link in moddedItem.ContainedFormLinks)
                        {
                            // Only if from source mod
                            if (link.FormKey.ModKey == origItem.FormKey.ModKey)
                            {
                                recordsToDup.Add(link);
                            }
                        }
                    }                  
                }
                else
                {
                    Console.WriteLine($"Could not load item {item.Name}. Could not create a formKey: {item.FormKey}.");
                    toRemove.Add(item);
                }
            }

            foreach (UTitem i in toRemove)
            {
                Items.Remove(i);
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
            
        }
    }

    public class UTconfig
    {
        public bool bPatchMales { get; set; }
        public bool bPatchFemales { get; set; }
        public bool bMakeItemsEquippable { get; set; }
        public bool bAssignByClass {get; set;}
        public UTslots Slots { get; set; }
        public List<string> PatchableRaces { get; set; }
        public UTclassDef ClassDefinitions { get; set; }
        public UTassignmentContainer Assignments { get; set; }
        public UTsetsByGender Sets { get; set; }
        public List<UTitem> Items { get; set; }

        public UTconfig()
        {
            bAssignByClass = false;
            Slots = new UTslots();
            PatchableRaces = new List<string>();
            ClassDefinitions = new UTclassDef();
            Assignments = new UTassignmentContainer();
            Sets = new UTsetsByGender();
            Items = new List<UTitem>();
        }
    }

    public class UTslots
    {
        public int Bottom { get; set; }
        public int Top { get; set; }

        public UTslots()
        {
            Bottom = 0;
            Top = 0;
        }

        public static BipedObjectFlag mapIntToSlot(int iFlag)
        {
            switch(iFlag)
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

    public class UTclassDef
    {
        public List<string> Poor { get; set; }
        public List<string> Medium { get; set; }
        public List<string> Rich { get; set; }

        public UTclassDef()
        {
            Poor = new List<string>();
            Medium = new List<string>();
            Rich = new List<string>();
        }
    }

    public class UTassignmentContainer
    {
        public UTassignment Male { get; set; }
        public UTassignment Female { get; set; }

        public UTassignmentContainer()
        {
            Male = new UTassignment();
            Female = new UTassignment();
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

    public class UTitem
    {
        public string Name { get; set; }
        public string DispName { get; set; }
        public string Type { get; set; }
        public string FormKey { get; set; }
        public FormKey FormKeyObject { get; set; }

        public UTitem()
        {
            Name = "";
            DispName = "Undergarments";
            Type = "";
            FormKey = "";
            FormKeyObject = new FormKey();
        }
    }

    public class UTsetsByGender
    {
        public List<UTitemset> Male { get; set; }
        public List<UTitemset> Female { get; set; }

        public UTsetsByGender()
        {
            Male = new List<UTitemset>();
            Female = new List<UTitemset>();
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
