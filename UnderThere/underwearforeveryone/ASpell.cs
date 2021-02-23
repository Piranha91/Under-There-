namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class underwearforeveryone
    {
        public static class ASpell
        {
            private readonly static ModKey ModKey = ModKey.FromNameAndExtension("underwearforeveryone.esp");
            public static FormKey UFE_ActorInventory_Spell => ModKey.MakeFormKey(0x80a);
            public static FormKey UFE_Actor_Spell => ModKey.MakeFormKey(0x80d);
            public static FormKey UFE_Cloak_Spell => ModKey.MakeFormKey(0xa02);
            public static FormKey UFE_Toggle_Spell => ModKey.MakeFormKey(0xa03);
            public static FormKey UFE_Player_Tracker_Spell => ModKey.MakeFormKey(0xa32);
        }
    }
}
