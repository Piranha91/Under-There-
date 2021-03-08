namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class underwearforeveryone
    {
        public static class MagicEffect
        {
            private readonly static ModKey ModKey = ModKey.FromNameAndExtension("underwearforeveryone.esp");
            public static FormKey UFE_ActorInventory_Effect => ModKey.MakeFormKey(0x800);
            public static FormKey UFE_Actor_Effect => ModKey.MakeFormKey(0x80c);
            public static FormKey UFE_Cloak_Effect => ModKey.MakeFormKey(0xa01);
            public static FormKey UFE_Toggle_Effect => ModKey.MakeFormKey(0xa04);
            public static FormKey UFE_Player_Tracker_Effect => ModKey.MakeFormKey(0xa31);
        }
    }
}
