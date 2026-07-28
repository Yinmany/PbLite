using PbLite;

namespace PbLite.Tests
{
    public class RegistryTests
    {
        [Fact]
        public void ForEach_VisitsAllSerializers()
        {
            var visited = new System.Collections.Generic.List<IProtoSerializer>();
            TestProtoRegistry.Register(visited.Add);

            Assert.NotEmpty(visited);

            // Should contain at least TestMessageSerializer and InnerMessageSerializer
            var typeNames = visited.Select(s => s.Type.Name).ToList();
            Assert.Contains("TestMessage", typeNames);
            Assert.Contains("InnerMessage", typeNames);

            // Each should be a singleton
            Assert.All(visited, s => Assert.NotNull(s));
        }

        [Fact]
        public void ForEach_AllImplementIProtoSerializer()
        {
            TestProtoRegistry.Register(s =>
            {
                Assert.IsAssignableFrom<IProtoSerializer>(s);
            });
        }

        [Fact]
        public void ForEach_DistinctTypes()
        {
            var types = new System.Collections.Generic.HashSet<System.Type>();
            TestProtoRegistry.Register(s => types.Add(s.Type));

            Assert.True(types.Count >= 2);
        }
    }
}
