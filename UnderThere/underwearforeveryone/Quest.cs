// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class underwearforeveryone
    {
        public static class Quest
        {
            private static FormLink<IQuestGetter> Construct(uint id) => new FormLink<IQuestGetter>(ModKey.MakeFormKey(id));
            public static FormLink<IQuestGetter> UFE_Auto_Quest => Construct(0xa21);
            public static FormLink<IQuestGetter> UFE_Player_Quest => Construct(0xa20);
        }
    }
}
