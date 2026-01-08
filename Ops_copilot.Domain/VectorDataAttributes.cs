using System;

namespace Microsoft.Extensions.VectorData
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class VectorStoreRecordKeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class VectorStoreRecordDataAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class VectorStoreRecordVectorAttribute : Attribute
    {
        public int Dimensions { get; set; }

        public VectorStoreRecordVectorAttribute() { }
    }
}
