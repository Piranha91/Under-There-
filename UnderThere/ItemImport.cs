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
            deepCopyItems(settings.Sets, UWsourcePlugins, state); // copy all armor records along with their linked subrecords into PatchMod to get rid of dependencies on the original plugins. Sets[i].FormKeyObject will now point to the new FormKey in PatchMod

            // create a leveled list entry for each set
            foreach (var set in settings.Sets)
            {
                var currentItems = state.PatchMod.LeveledItems.AddNew();
                currentItems.EditorID = "LItems_" + set.Name;
                currentItems.Flags |= LeveledItem.Flag.UseAll;
                currentItems.Entries = new ExtendedList<LeveledItemEntry>();

                editAndStoreUTitems(set.Items, currentItems, settings.MakeItemsEquippable, settings.PatchableRaces, state);

                set.LeveledList = currentItems.FormKey;
            }
        }

        public static void deepCopyItems(List<UTSet> Sets, HashSet<ModKey> UWsourcePlugins, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var recordsToDup = new HashSet<FormLinkInformation>();

            foreach (var set in Sets)
            {
                getFormLinksToDuplicate(set.Items, recordsToDup, state.LinkCache);
            }

            // store the original source mod names to notify user that they can be disabled.
            UWsourcePlugins.Add(recordsToDup.Select(x => x.FormKey.ModKey));

            //var deleteMeEventually = (ILinkCache<ISkyrimMod>)lk; // will be moved to lk directly in next Mutagen version.
            var duplicated = recordsToDup
                .Select(toDup =>
                {
                    if (!state.LinkCache.TryResolveContext(toDup.FormKey, toDup.Type, out var existingContext))
                    {
                        throw new ArgumentException($"Couldn't find {toDup.FormKey}?");
                    }
                    return (OldFormKey: toDup.FormKey, Duplicate: existingContext.DuplicateIntoAsNewRecord(state.PatchMod));
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
                remapSetItemList(set.Items, remap);
            }
        }

        public static void getFormLinksToDuplicate(List<UTitem> UTitemList, HashSet<FormLinkInformation> recordsToDup, ILinkCache lk)
        {
            foreach (var item in UTitemList)
            {
                if (!item.Record.TryResolve(lk, out var origItem))
                {
                    throw new Exception("Could not find item with formKey " + item.Record.FormKey + ". Please make sure that " + item.Record.FormKey.ModKey.ToString() + " is active in your load order.");
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

        public static void remapSetItemList(List<UTitem> UTitemList, Dictionary<FormKey, FormKey> remap)
        {
            foreach (var item in UTitemList)
            {
                item.Record = new FormLink<IArmorGetter>(remap[item.Record.FormKey]);
            }
        }

        public static void editAndStoreUTitems(List<UTitem> items, LeveledItem currentItems, bool bMakeItemsEquipable, IReadOnlyCollection<FormLink<IRaceGetter>> patchableRaceFormLinks, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (currentItems.Entries == null) return;
            foreach (UTitem item in items)
            {
                if (item.Record.TryResolve<IArmor>(state.LinkCache, out var moddedItem))
                {
                    moddedItem.Name = item.DispName;
                    moddedItem.EditorID = "UT_" + moddedItem.EditorID;
                    if (item.Weight >= 0) // if not defined in config file, keep the original item's weight
                    {
                        moddedItem.Weight = item.Weight;
                    }
                    if (item.Value != 4294967295) // if not defined in config file, keep the original item's value
                    {
                        moddedItem.Value = item.Value;
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
                    data.Reference = moddedItem.FormKey;
                    data.Level = 1;
                    data.Count = 1;
                    entry.Data = data;
                    currentItems.Entries.Add(entry);
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
