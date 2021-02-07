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
            
            createItems(settings.Sets, settings.bMakeItemsEquippable, settings.Slots, state.LinkCache, state.PatchMod);

            Dictionary<string, List<string>> ClassLookupFailures = new Dictionary<string, List<string>>();
            string gender = "";
            /*
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                // Use Traits: determines race and sex
                // Use Inventory: determines outfit


                //1: Check if NPC has a template
                //  If no, process the NPC directly
                //  If yes, 
                //      check if the NPC inherits traits
                //          If no, check if the NPC's race is patchable
                //              If yes, handle outfit distribution directly via patcher
                //              If no, don't patch this NPC
                //          If yes, check if the template's race is patchable and continue if not (this may possibly be fallible if the template is a levelled list that contains both patchable and non-patchable races. Current strategy is to patch if ANY of the templates are patchable).
                //              Handle outfit distribution via SPID
                //      check if the NPC inherits inventory
                //          If no, patch the current NPC's outfit
                
                //check if NPC has a template
                if (npc.Template.TryResolve<INpcSpawnGetter>(state.LinkCache, out var npcTemplate) && npcTemplate != null)
                {
                    List<NewOutfitContainer> Assignments = new List<NewOutfitContainer>();
                    getAssignmentForTemplatedNPC(npcTemplate, Assignments, state.LinkCache, ClassLookupFailures, settings);
                }

                // check if NPC race is patchable
                else if (npc.Race.TryResolve<IRaceGetter>(state.LinkCache, out var NPCrace) && npc != null && NPCrace.EditorID != null && settings.PatchableRaces.Contains(NPCrace.EditorID))
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

                            //Console.WriteLine($"{moddedNPC.Name} ({moddedNPC.EditorID}) was assigned outfit {chosenOutfit.ToString()}");
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
                        editedAA.BodyTemplate.FirstPersonFlags |= mapIntToSlot(settings.Slots.Bottom);
                        editedAA.BodyTemplate.FirstPersonFlags |= mapIntToSlot(settings.Slots.Top);
                    }
                }
            }

            */

            Console.WriteLine("\nEnjoy the underwear. Goodbye.");
        }

        public static void getAssignmentForTemplatedNPC(INpcSpawnGetter template, List<NewOutfitContainer> assignments, ILinkCache lk, Dictionary<string, List<string>> ClassLookupFailures, UTconfig settings)
        {
            // if leveled list entry is an NPC
            if (lk.TryResolve<INpcGetter>(template.FormKey, out var npcTemplate) && npcTemplate != null && (npcTemplate.Race.TryResolve<IRaceGetter>(lk, out var NPCrace) && NPCrace != null && NPCrace.EditorID != null && settings.PatchableRaces.Contains(NPCrace.EditorID)))
            {
                // check if NPC inherits inventory

                if (npcTemplate.Class.TryResolve<IClassGetter>(lk, out var NPCclass) && NPCclass != null && NPCclass.EditorID != null)
                {
                    NewOutfitContainer NOC = new NewOutfitContainer();
                    string assignmentClass = GetClassDef(NPCclass.EditorID, settings.ClassDefinitions, settings.bAssignByClass, npcTemplate, ClassLookupFailures);

                }
                int debug1 = 0;
            }
            // if leveled list entry is another leveled list
            else if (lk.TryResolve<ILeveledNpcGetter>(template.FormKey, out var lvlListTemplate) && lvlListTemplate != null && lvlListTemplate.Entries != null)
            {
                foreach (ILeveledNpcEntryGetter lvn in lvlListTemplate.Entries)
                {
                    if (lvn.Data != null && lk.TryResolve<INpcSpawnGetter>(lvn.Data.Reference.FormKey, out var subRef) && subRef != null)
                    {
                        getAssignmentForTemplatedNPC(subRef, assignments, lk, ClassLookupFailures, settings);
                    }
                }
            }

            return;
        }

        /*
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
        }*/

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

            return true;
        }

        public static void deepCopyItems(List<UTSet> Sets, ILinkCache lk, ISkyrimMod PatchMod)
        {
            List<UTSet> toRemove = new List<UTSet>();
            var recordsToDup = new HashSet<FormLinkInformation>();

            List<FormKey> extraArmors = new List<FormKey>(); // these extra armors will be removed at the end of the function when their armor addons have been duplicated. 

            foreach (var Set in Sets)
            {
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

                        recordsToDup.Add(origItem.ToFormLinkInformation());
                        
                        if (i > 0)
                        {
                            extraArmors.Add(origFormKey);
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
                for (int i = 1; i < set.Items.Count; i++)
                {
                    string additionalArmorFormKeyString = set.Items[i];
                    if (FormKey.TryFactory(additionalArmorFormKeyString, out var origArmorFormKey) && !origArmorFormKey.IsNull)
                    {
                        if (remap.ContainsKey(origArmorFormKey) && lk.TryResolve<IArmorGetter>(remap[origArmorFormKey], out var additionalArmor)) // should always be true but checking just in case
                        {
                            foreach (IFormLink<IArmorAddonGetter> additionalARMA_FL in additionalArmor.Armature)
                            {
                                set.AdditionalAAs.Add(additionalARMA_FL);
                            }
                        }
                    }
                }
            }

            foreach (FormKey extra in extraArmors)
            {
                if (remap != null && remap.ContainsKey(extra))
                {
                    PatchMod.Remove(remap[extra]);
                }
            }

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

                        /*
                        if (Set.Items.Count > 1)
                        {
                            for (int i = 1; i < Set.Items.Count; i++)
                            {
                                if (FormKey.TryFactory(Set.Items[i], out var additionalFormKey) && !additionalFormKey.IsNull)
                                {
                                    if (!lk.TryResolve<IArmorGetter>(additionalFormKey, out var additionalItem))
                                    {
                                        Console.WriteLine("Could not find item with formKey " + additionalFormKey);
                                        toRemove.Add(Set); // remove this set from the list
                                        break;
                                    }

                                    foreach (IFormLink<IArmorAddonGetter> additionalARMAtemplateGetter in additionalItem.Armature)
                                    {
                                        moddedItem.Armature.Add(additionalARMAtemplateGetter);
                                    }
                                }
                            }
                        }*/

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
        public bool bPatchMales { get; set; }
        public bool bPatchFemales { get; set; }
        public bool bMakeItemsEquippable { get; set; }
        public bool bAssignByClass {get; set;}
        public List<int> Slots { get; set; }
        public List<string> PatchableRaces { get; set; }
        public UTclassDef ClassDefinitions { get; set; }
        public UTassignmentContainer Assignments { get; set; }
        public List<UTSet> Sets { get; set; }

        public UTconfig()
        {
            bAssignByClass = false;
            Slots = new List<int>();
            PatchableRaces = new List<string>();
            ClassDefinitions = new UTclassDef();
            Assignments = new UTassignmentContainer();
            Sets = new List<UTSet>();
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
