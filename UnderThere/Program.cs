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
            //var loadOrder = LoadOrder.Import<SkyrimMod>(GameRelease.SkyrimSE);
            //ILinkCache<SkyrimMod> loadOrderLinkCache = state.LoadOrder.create

            var settingsPath = Path.Combine(state.ExtraSettingsDataPath, "UnderThereConfig.json");

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

            createItems(settings.Items, state.LinkCache);

            Dictionary<string, List<string>> ClassLookupFailures = new Dictionary<string, List<string>>();
            string gender = "";

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
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
                        }
                        else
                        {
                            gender = "male";
                        }

                        // Get NPC's current outfit
                        if (npc.DefaultOutfit.TryResolve<IOutfitGetter>(state.LinkCache, out var NPCoutfit) && NPCoutfit != null)
                        {
                            // pick an item set based on NPC's gender and assignmentClass
                            string chosenItemSet = chooseItemSet(gender, assignmentClass, settings.Assignments);

                            // check if the given outfit + itemset combination has already been assigned
                            FormLink<Outfit> chosenOutfit = getModifiedOutfitKey(gender, chosenItemSet, NPCoutfit.FormKey, OutfitMap);
                            if (chosenOutfit.FormKey == NPCoutfit.FormKey) // getModifiedOutfitKey returns the original outfit formlink if it can't find a modified one in the mapping
                            {
                                Outfit newOutfit = createModifiedOutfit(gender, chosenItemSet, NPCoutfit, settings, OutfitMap);
                                moddedNPC.DefaultOutfit = newOutfit.FormKey;
                            }
                            else
                            {
                                moddedNPC.DefaultOutfit = chosenOutfit.FormKey;
                            }
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
                        Console.WriteLine(label);
                    }
                    Console.WriteLine();
                }
            }

            int debug = 0;
        }

        public static Outfit createModifiedOutfit(string gender, string itemSet, IOutfitGetter origOutfit, UTconfig config, OutfitMapping map)
        {
            Outfit moddedOutfit = (Outfit)origOutfit.DeepCopy();
            List<UTitemset> availableSets = new List<UTitemset>();
            List<string> chosenItems = new List<string>();

            switch(gender)
            {
                case "male":
                    availableSets = config.Sets.Male;
                    break;
                case "female":
                    availableSets = config.Sets.Female;
                    break;
            }
            
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
                        moddedOutfit.Items.Add(uTitem.FormLink);
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
            NOC.FormLink = moddedOutfit;
            
            if (mapping.ContainsKey(origOutfit.FormKey))
            {
                mapping[origOutfit.FormKey].Add(NOC);
            }
            else
            {
                mapping.Add(origOutfit.FormKey, new List<NewOutfitContainer>() { NOC });
            }

            return moddedOutfit;
        }

        public static FormLink<Outfit> getModifiedOutfitKey(string gender, string itemSet, FormKey origFormKey, OutfitMapping map)
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
                        return NOC.FormLink;
                    }
                }
            }

            return new FormLink<Outfit>(origFormKey);
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

        public static void createItems(List<UTitem> Items, ILinkCache lk)
        {
            List<UTitem> toRemove = new List<UTitem>();
            foreach (var item in Items)
            {
                if (item.formID != null && item.formID.Length == 8)
                {
                    item.formID = item.formID.Substring(2, 6);
                }
                string itemKey = item.formID + ":" + item.source;
                if (FormKey.TryFactory(itemKey, out var formKey) && formKey.IsNull == false)
                {
                    // If conversion successful
                    if (!lk.TryResolve<IArmorGetter>(formKey, out var origItem))
                    {
                        Console.WriteLine("Could not find item with formKey " + itemKey);
                        toRemove.Add(item);
                    }
                    else
                    {
                        item.FormKey = formKey;
                        Armor ModdedItem = (Armor)origItem.DeepCopy();
                        ModdedItem.Name = item.Name;
                        item.FormLink = ModdedItem;
                    }                  
                }
                else
                {
                    Console.WriteLine("Could not load item " + item.Name + ". Could not create a formKey from plugin " + item.source + " and formID " + item.formID + ".");
                    toRemove.Add(item);
                }
            }

            foreach(UTitem i in toRemove)
            {
                Items.Remove(i);
            }
        }
    }

    public class UTconfig
    {
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
        public string source { get; set; }
        public string formID { get; set; }
        public FormKey FormKey { get; set; }
        public FormLink<Armor> FormLink { get; set; }

        public UTitem()
        {
            Name = "";
            source = "";
            formID = "";
            FormKey = new FormKey();
            FormLink = new FormLink<Armor>();
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

        public FormLink<Outfit> FormLink { get; set; }

        public NewOutfitContainer()
        {
            itemSet = "";
            FormKey = new FormKey();
            FormLink = new FormLink<Outfit>();
        }
    }

}
