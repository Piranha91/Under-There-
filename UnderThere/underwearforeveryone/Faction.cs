namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class underwearforeveryone
    {
        public static class Faction
        {
            private readonly static ModKey ModKey = ModKey.FromNameAndExtension("underwearforeveryone.esp");
            public static FormKey UFE_Include_Faction => ModKey.MakeFormKey(0x80b);
            public static FormKey UFE_Exclude_Faction => ModKey.MakeFormKey(0xa34);
        }
    }
}
