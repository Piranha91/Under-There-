using System;
using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Noggog;
using UnderThere.Settings;

namespace UnderThere
{
    class ItemImport
    {
        public static void createItems(UTconfig settings, HashSet<ModKey> UWsourcePlugins, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            getSourcePlugins(settings, UWsourcePlugins, state);

            var lItemsByGenderAndWealth = initializeGenderedCategoryLeveledLists(settings, state);

            // create a leveled list entry for each set
            foreach (var set in settings.AllSets)
            {
                var EditorID = "LItems_" + set.Name;
                var currentItems = state.PatchMod.LeveledItems.AddNew(EditorID);
                currentItems.EditorID = EditorID;
                currentItems.Flags |= LeveledItem.Flag.UseAll;
                currentItems.Entries = new ExtendedList<LeveledItemEntry>();
                
                editAndStoreUTitems(set, currentItems, settings.MakeItemsEquippable, settings.PatchableRaces, lItemsByGenderAndWealth, state);

                set.LeveledList = currentItems.FormKey;
            }

            Validator.validateGenderedSets(settings, lItemsByGenderAndWealth);
        }

        public static Dictionary<GenderTarget, Dictionary<string, LeveledItem>> initializeGenderedCategoryLeveledLists(UTconfig settings, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var lItemsByGenderAndWealth = new Dictionary<GenderTarget, Dictionary<string, LeveledItem>>();
            lItemsByGenderAndWealth[GenderTarget.Male] = new Dictionary<string, LeveledItem>();
            lItemsByGenderAndWealth[GenderTarget.Female] = new Dictionary<string, LeveledItem>();

            HashSet<string> categories = new HashSet<string>();
            foreach (UTSet set in settings.AllSets)
            {
                //change with Noggog's optimized code later instead of Try Catch
                string category = set is UTCategorySet categorySet ? categorySet.Category : Program.Default;
                categories.Add(category);
            }

            foreach (string cat in categories)
            {
                var editorID = "LItems_M_" + cat;
                var catItemsM = state.PatchMod.LeveledItems.AddNew(editorID);
                catItemsM.EditorID = editorID;
                catItemsM.Entries = new ExtendedList<LeveledItemEntry>();
                lItemsByGenderAndWealth[GenderTarget.Male].Add(cat, catItemsM);

                editorID = "LItems_F_" + cat;
                var catItemsF = state.PatchMod.LeveledItems.AddNew(editorID);
                catItemsF.EditorID = editorID;
                catItemsF.Entries = new ExtendedList<LeveledItemEntry>();
                lItemsByGenderAndWealth[GenderTarget.Female].Add(cat, catItemsF);
            }

            return lItemsByGenderAndWealth;
        }

        public static void getSourcePlugins(UTconfig settings, HashSet<ModKey> UWsourcePlugins, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (UTSet set in settings.Sets)
            {
                foreach (UTitem item in set.Items)
                {
                    if (!item.Record.TryResolve(state.LinkCache, out var origItem))
                    {
                        throw new Exception("Could not find item with formKey " + item.Record.FormKey + ". Please make sure that " + item.Record.FormKey.ModKey.ToString() + " is active in your load order.");
                    }
                    if (!UWsourcePlugins.Contains(origItem.FormKey.ModKey))
                    {
                        UWsourcePlugins.Add(origItem.FormKey.ModKey);
                    }
                }
            }
        }

        public static void editAndStoreUTitems(UTSet set, LeveledItem currentItems, bool bMakeItemsEquipable, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaceFormLinks, Dictionary<GenderTarget, Dictionary<string, LeveledItem>> lItemsByGenderAndWealth,  IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (currentItems.Entries == null) return;

            string category = set is UTCategorySet categorySet ? categorySet.Category : Program.Default;

            HashSet<Armor?> maleSetItems = new HashSet<Armor?>();
            HashSet<Armor?> femaleSetItems = new HashSet<Armor?>();

            foreach (UTitem item in set.Items)
            {
                if (state.LinkCache.TryResolve<IArmorGetter>(item.Record.FormKey, out var linkedItem))
                {
                    var moddedItem = state.PatchMod.Armors.GetOrAddAsOverride(linkedItem);
                    moddedItem.Name = item.DispName;
                    moddedItem.EditorID = "UT_" + moddedItem.EditorID;
                    if (item.Weight != "") // if not defined in config file, keep the original item's weight
                    {
                        try
                        {
                            moddedItem.Weight = float.Parse(item.Weight);
                        }
                        catch
                        {
                            throw new Exception("Could not convert weight \"" + item.Weight + "\" to a number for item: " + item.DispName);
                        }
                    }
                    if (item.Value != "") // if not defined in config file, keep the original item's value
                    {
                        try
                        {
                            moddedItem.Value = Convert.ToUInt32(item.Value);
                        }
                        catch
                        {
                            throw new Exception("Could not convert value \"" + item.Value + "\" to a number for item: " + item.DispName);
                        }
                    }
                
                    if (item.Slots.Count > 0 && moddedItem.BodyTemplate != null) // if not defined in config file, keep the original item's slots
                    {
                        moddedItem.BodyTemplate.FirstPersonFlags = new BipedObjectFlag();
                        foreach (int modSlot in item.Slots)
                        {
                            moddedItem.BodyTemplate.FirstPersonFlags |= Auxil.mapIntToSlot(modSlot);
                        }
                    }
                    switch (bMakeItemsEquipable)
                    {
                        case true: moddedItem.MajorFlags &= Armor.MajorFlag.NonPlayable; break;
                        case false: moddedItem.MajorFlags |= Armor.MajorFlag.NonPlayable; break;
                    }

                    modifyArmature(moddedItem, item.Slots, patchableRaceFormLinks, state); // sets slots and additional races for armature if necessary

                    LeveledItemEntry entry = new LeveledItemEntry();
                    LeveledItemEntryData data = new LeveledItemEntryData();
                    data.Reference.SetTo(moddedItem);
                    data.Level = 1;
                    data.Count = 1;
                    entry.Data = data;
                    currentItems.Entries.Add(entry);

                    // store gendered items
                    switch (item.Gender)
                    {
                        case GenderTarget.Male:
                            maleSetItems.Add(moddedItem);
                            break;
                        case GenderTarget.Female:
                            femaleSetItems.Add(moddedItem);
                            break;
                        case GenderTarget.Mutual:
                            maleSetItems.Add(moddedItem);
                            femaleSetItems.Add(moddedItem);
                            break;
                    }
                }
                else
                {
                    throw new Exception("Could not resolve record " + item.Record.FormKey.ToString());
                }
            }

            // fill in missing gendered items

            if (maleSetItems.Any())
            {
                createGenderedLeveledSet(GenderTarget.Male, category, set, maleSetItems, lItemsByGenderAndWealth, state);
            }
            else if (lItemsByGenderAndWealth[GenderTarget.Male][category] != null)
            {
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference.SetTo(lItemsByGenderAndWealth[GenderTarget.Male][category]);
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                currentItems.Entries.Add(entry);
            }

            if (femaleSetItems.Any())
            {
                createGenderedLeveledSet(GenderTarget.Female, category, set, femaleSetItems, lItemsByGenderAndWealth, state);
            }
            else if (lItemsByGenderAndWealth[GenderTarget.Female][category] != null)
            {
                LeveledItemEntry entry = new LeveledItemEntry();
                LeveledItemEntryData data = new LeveledItemEntryData();
                data.Reference.SetTo(lItemsByGenderAndWealth[GenderTarget.Female][category]);
                data.Level = 1;
                data.Count = 1;
                entry.Data = data;
                currentItems.Entries.Add(entry);
            }
        }

        public static void createGenderedLeveledSet(GenderTarget gender, string category, UTSet set, HashSet<Armor?> genderedSetItems, Dictionary<GenderTarget, Dictionary<string, LeveledItem>> lItemsByGenderAndWealth, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // create leveled list for the gendered items in the current set
            var editorID = "LItems_" + set.Name + "_" + gender.ToString();
            LeveledItem currentItems_gender = state.PatchMod.LeveledItems.AddNew(editorID);
            currentItems_gender.EditorID = editorID;
            currentItems_gender.Flags |= LeveledItem.Flag.UseAll;
            currentItems_gender.Entries = new ExtendedList<LeveledItemEntry>();

            // add the gendered items for the current set into the new leveled list
            foreach (var item in genderedSetItems)
            {
                LeveledItemEntry entry_gender = new LeveledItemEntry();
                LeveledItemEntryData data_gender = new LeveledItemEntryData();
                data_gender.Reference.SetTo(item);
                data_gender.Level = 1;
                data_gender.Count = 1;
                entry_gender.Data = data_gender;
                currentItems_gender.Entries.Add(entry_gender);
            }

            // add the new leveled list into the existing leveled list containing all gendered outfits in this category
            LeveledItemEntry entry = new LeveledItemEntry();
            LeveledItemEntryData data = new LeveledItemEntryData();
            data.Reference.SetTo(currentItems_gender);
            data.Level = 1;
            data.Count = 1;
            entry.Data = data;

            var currentLLentries = lItemsByGenderAndWealth[gender][category].Entries;
            if (lItemsByGenderAndWealth != null && lItemsByGenderAndWealth[gender] != null && lItemsByGenderAndWealth[gender][category] != null && currentLLentries != null)
            {
                currentLLentries.Add(entry);
            }
        }

        public static void modifyArmature (IArmor moddedItem, List<int> slots, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaceFormLinks, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var aa in moddedItem.Armature)
            {
                if (state.LinkCache.TryResolve<IArmorAddonGetter>(aa.FormKey, out var moddedAA))
                {
                    var moddedAA_override = state.PatchMod.ArmorAddons.GetOrAddAsOverride(moddedAA);

                    setAdditionalRaces(moddedAA_override, patchableRaceFormLinks);
                    editARMAslots(moddedAA_override, slots);
                }
            }
        }

        public static void editARMAslots(IArmorAddon moddedAA, List<int> slots)
        {
            if (slots.Count > 0 && moddedAA.BodyTemplate != null)
            {
                moddedAA.BodyTemplate.FirstPersonFlags = new BipedObjectFlag();
                foreach (int modSlot in slots)
                {
                    moddedAA.BodyTemplate.FirstPersonFlags |= Auxil.mapIntToSlot(modSlot);
                }
            }
        }

        public static void setAdditionalRaces(IArmorAddon moddedAA, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaceFormLinks)
        {
            // get missing PatchableRaces
            List<IFormLink<IRaceGetter>> addedRaces = new List<IFormLink<IRaceGetter>>();
            foreach (var neededRace in patchableRaceFormLinks)
            {
                bool keyFound = false;
                foreach (var additionalRace in moddedAA.AdditionalRaces)
                {
                    if (additionalRace.FormKey == neededRace.FormKey)
                    {
                        keyFound = true;
                        break;
                    }
                }
                if (!keyFound)
                {
                    addedRaces.Add(neededRace);
                }
            }
            // add missing PatchableRaces
            foreach (var additionalRace in addedRaces)
            {
                moddedAA.AdditionalRaces.Add(additionalRace);
            }
        }
    }
}
