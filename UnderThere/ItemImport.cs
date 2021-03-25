using System;
using System.Collections.Generic;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Linq;
using Noggog;
using UnderThere.Settings;

namespace UnderThere
{
    class ItemImport
    {
        public static void createItems(UTconfig settings, HashSet<ModKey> UWsourcePlugins, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            getSourcePlugins(settings, UWsourcePlugins, state);

            // create a leveled list entry for each set
            foreach (var set in settings.AllSets)
            {
                var currentItems = state.PatchMod.LeveledItems.AddNew();
                currentItems.EditorID = "LItems_" + set.Name;
                currentItems.Flags |= LeveledItem.Flag.UseAll;
                currentItems.Entries = new ExtendedList<LeveledItemEntry>();

                editAndStoreUTitems(set.Items, currentItems, settings.MakeItemsEquippable, settings.PatchableRaces, state);

                set.LeveledList = currentItems.FormKey;
            }
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

        public static void editAndStoreUTitems(List<UTitem> items, LeveledItem currentItems, bool bMakeItemsEquipable, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaceFormLinks, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (currentItems.Entries == null) return;
            foreach (UTitem item in items)
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
                }
                else
                {
                    throw new Exception("Could not resolve record " + item.Record.FormKey.ToString());
                }
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

        /*
        public static void setAdditionalRaces(List<UTitem> items, IReadOnlyCollection<IFormLink<IRaceGetter>> patchableRaceFormLinks, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (UTitem item in items)
            {
                if (state.LinkCache.TryResolve<IArmorGetter>(item.formKey, out var moddedItem))
                {
                    foreach (var aa in moddedItem.Armature)
                    {
                        if (state.LinkCache.TryResolve<IArmorAddonGetter>(aa.FormKey, out var moddedAA))
                        {
                            var moddedAA_override = state.PatchMod.ArmorAddons.GetOrAddAsOverride(moddedAA);
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
                                moddedAA_override.AdditionalRaces.Add(additionalRace);
                            }
                        }
                    }
                }
            }
        }*/
    }
}
