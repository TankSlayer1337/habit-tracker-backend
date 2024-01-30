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
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var name = _faker.Random.AlphaNumeric(10);
            var entry = HabitDefinitionEntry.Create(userId, name);
            var newName = _faker.Random.AlphaNumeric(10);

            // Act
            var clonedEntry = entry.CloneWithNewName(newName);

            // Assert
            Assert.Equal(userId, clonedEntry.PartitionKey.UserId);
            Assert.Equal(newName, clonedEntry.Name);
            Assert.Equal(name, entry.Name);
        }

        [Fact]
        public void ConvertToDefinition()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var name = _faker.Random.AlphaNumeric(10);
            var entry = HabitDefinitionEntry.Create(userId, name);

            // Act
            var definition = entry.Convert();

            // Assert
            Assert.Equal(entry.HabitId, definition.HabitId);
            Assert.Equal(entry.Name, definition.Name);
        }
    }
}