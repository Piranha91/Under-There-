using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nifly;

namespace UnderThere
{
    internal class NifHandler
    {
        public static void SetSlots(string path, List<int> slots)
        {
            using var nif = new NifFile(true);
            nif.Load(path);

            var blockCache = new niflycpp.BlockCache(nif.GetHeader());
            var header = blockCache.Header;

            for (uint blockId = 0; blockId < header.GetNumBlocks(); ++blockId)
            {
                NiShape shape = blockCache.EditableBlockById<NiShape>(blockId);
                if (shape == null || (shape is not BSTriShape && shape is not NiTriShape && shape is not NiTriStrips))
                    continue;

                // Remove skin from BSTriShape
                if (shape.HasSkinInstance())
                {
                    //niShape.SetSkinned(false);
                    using var skinRef = shape.SkinInstanceRef();
                    BSDismemberSkinInstance dskinInstance = blockCache.EditableBlockById<BSDismemberSkinInstance>(skinRef.index);

                    var x = dskinInstance.partitions;
                    var y = x.begin();


                }
            }
        }
    }
}
