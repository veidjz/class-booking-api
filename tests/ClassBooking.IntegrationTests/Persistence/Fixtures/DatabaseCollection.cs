namespace ClassBooking.IntegrationTests.Persistence.Fixtures;

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<ContainersFixture>;
