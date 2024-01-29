using HabitTracker.DynamoDb.Models;

namespace Tests
{
    public class HabitDefinitionEntryTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void CreateWithUserIdAndName()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var name = _faker.Random.AlphaNumeric(10);

            // Act
            var newEntry = HabitDefinitionEntry.Create(userId, name);

            // Assert
            Assert.Equal("HabitDefinition", newEntry.PartitionKey.ItemType);
            Assert.Equal(userId, newEntry.PartitionKey.UserId);
            Assert.Equal(name, newEntry.Name);
            Assert.True(Guid.TryParse(newEntry.HabitId, out _));
        }

        [Fact]
        public void CloneWithNewName()
        {

        }
    }
}