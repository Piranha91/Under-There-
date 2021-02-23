using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderThere.Settings
{
    public enum Quality
    {
        Poor,
        Medium,
        Rich
    }

    public enum AssignmentQuality
    {
        Default,
        Poor,
        Medium,
        Rich
    }

    public static class QualityExt
    {
        public static AssignmentQuality ToAssignmentQuality(this Quality q)
        {
            return q switch
            {
                Quality.Poor => AssignmentQuality.Poor,
                Quality.Medium => AssignmentQuality.Medium,
                Quality.Rich => AssignmentQuality.Rich,
                _ => throw new NotImplementedException()
            };
        }
    }
}
