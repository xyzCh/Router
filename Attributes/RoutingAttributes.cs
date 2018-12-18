using System;
namespace Router.Attributes
{
    public sealed class RoutingMethodAttribute:Attribute
    {
        public RoutingMethodAttribute() {
            UseHttpGet = true;
            JSONSerializeString = true;
        }

        public bool UseHttpGet { get; set; }
        public bool JSONSerializeString { get; set; }
    }
}
