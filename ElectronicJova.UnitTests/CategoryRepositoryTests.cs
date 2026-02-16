using ElectronicJova.Data;
using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using System.Threading.Tasks;

namespace ElectronicJova.UnitTests
{
    public class CategoryRepositoryTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Add_ShouldAddCategorySuccessfully()
        {
            // Arrange
            var dbContext = GetDbContext("Add_Category_Db");
            var repository = new Repository<Category>(dbContext);
            var category = new Category { Id = 1, Name = "Test Category", DisplayOrder = 1 };

            // Act
            repository.Add(category);
            await dbContext.SaveChangesAsync();

            // Assert
            var result = await dbContext.Categories.FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Test Category", result.Name);
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllCategories()
        {
            // Arrange
            var dbContext = GetDbContext("GetAll_Category_Db");
            dbContext.Categories.AddRange(
                new Category { Id = 1, Name = "Cat1", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Cat2", DisplayOrder = 2 }
            );
            await dbContext.SaveChangesAsync();

            var repository = new Repository<Category>(dbContext);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetFirstOrDefault_ShouldReturnCorrectCategory()
        {
            // Arrange
            var dbContext = GetDbContext("GetFirstOrDefault_Category_Db");
            dbContext.Categories.AddRange(
                new Category { Id = 1, Name = "Cat1", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Cat2", DisplayOrder = 2 }
            );
            await dbContext.SaveChangesAsync();

            var repository = new Repository<Category>(dbContext);

            // Act
            var result = await repository.GetFirstOrDefaultAsync(u => u.Name == "Cat2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cat2", result.Name);
        }

        [Fact]
        public async Task Update_ShouldUpdateCategorySuccessfully()
        {
            // Arrange
            var dbContext = GetDbContext("Update_Category_Db");
            dbContext.Categories.Add(new Category { Id = 1, Name = "Old Name", DisplayOrder = 1 });
            await dbContext.SaveChangesAsync();

            var repository = new Repository<Category>(dbContext);
            var categoryToUpdate = await dbContext.Categories.FindAsync(1);
            categoryToUpdate.Name = "New Name";

            // Act
            repository.Update(categoryToUpdate);
            await dbContext.SaveChangesAsync();

            // Assert
            var result = await dbContext.Categories.FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
        }

        [Fact]
        public async Task Remove_ShouldRemoveCategorySuccessfully()
        {
            // Arrange
            var dbContext = GetDbContext("Remove_Category_Db");
            dbContext.Categories.Add(new Category { Id = 1, Name = "To Be Removed", DisplayOrder = 1 });
            await dbContext.SaveChangesAsync();

            var repository = new Repository<Category>(dbContext);
            var categoryToRemove = await dbContext.Categories.FindAsync(1);

            // Act
            repository.Remove(categoryToRemove);
            await dbContext.SaveChangesAsync();

            // Assert
            var result = await dbContext.Categories.FindAsync(1);
            Assert.Null(result);
        }
    }
}
