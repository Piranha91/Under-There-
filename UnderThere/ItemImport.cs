using System;
using System.Collections.Generic;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace UnderThere
{
    class ItemImport
    {
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
    }
}
