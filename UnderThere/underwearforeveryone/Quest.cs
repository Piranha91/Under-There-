namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class underwearforeveryone
    {
        public static class Quest
        {
            private readonly static ModKey ModKey = ModKey.FromNameAndExtension("underwearforeveryone.esp");
            public static FormKey UFE_Auto_Quest => ModKey.MakeFormKey(0xa21);
            public static FormKey UFE_Player_Quest => ModKey.MakeFormKey(0xa20);
        }
    }
}
