namespace DeliverySystem.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection definition that shares a single <see cref="DeliverySystemFactory"/>
/// instance across all test classes in the collection, avoiding redundant server startups.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<DeliverySystemFactory>
{
    /// <summary>The collection name referenced by test classes via <c>[Collection]</c>.</summary>
    public const string Name = "Integration";
}
