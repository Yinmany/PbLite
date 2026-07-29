namespace PbLite.Tests
{
    public class RegistryTests
    {
        [Fact]
        public void Register_RegistersAllSerializers()
        {
            PbLiteGeneratedSerializers.Register();

            Assert.NotNull(SerializerRegistry.Get(typeof(TestMessage)));
            Assert.NotNull(SerializerRegistry.Get(typeof(InnerMessage)));
        }

        [Fact]
        public void Register_AllImplementIProtoSerializer()
        {
            PbLiteGeneratedSerializers.Register();

            var s1 = SerializerRegistry.Get(typeof(TestMessage));
            var s2 = SerializerRegistry.Get(typeof(InnerMessage));

            Assert.IsAssignableFrom<IProtoSerializer>(s1);
            Assert.IsAssignableFrom<IProtoSerializer>(s2);
        }

        [Fact]
        public void Register_DistinctTypes()
        {
            PbLiteGeneratedSerializers.Register();

            Assert.NotSame(
                SerializerRegistry.Get(typeof(TestMessage)),
                SerializerRegistry.Get(typeof(InnerMessage)));
        }
    }
}
