using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AutoMapper;
using System.Linq;

namespace EFAutomapperBug
{
    public class TestItemChild
    {
        public int Id { get; set; }
    }

    public class TestItem
    {
        public int Id { get; set; }

        public int TestItemChildId { get; set; }
        public TestItemChild TestItemChild { get; set; }
    }

    public class TestItemChildDTO
    {
        public int Id { get; set; }
    }

    public class TestItemDTO
    {
        public int TestItemChildId { get; set; }
        public TestItemChildDTO TestItemChild { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestItemChild> TestItemChildren { get; set; }
        public DbSet<TestItem> TestItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType))
            {
                modelBuilder.Entity(entityType)
                    .HasKey("Id");

                modelBuilder.Entity(entityType)
                    .Property<int>("Id")
                    .ValueGeneratedNever();
            }
        }
    }

    public class UnitTest
    {
        [Fact]
        public async Task Test()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            // Create tables in database
            using (var dbContext = CreateDbContext())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Insert test value
            using(var dbContext = CreateDbContext())
            {
                dbContext.TestItems.Add(new TestItem
                {
                    Id = 1,
                    TestItemChild = new TestItemChild
                    {
                        Id = 1,
                    }
                });

                await dbContext.SaveChangesAsync();
            }

            // Update test value
            using(var dbContext = CreateDbContext())
            {
                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstAsync();
                
                var mapper =  new Mapper(new MapperConfiguration(config =>
                {
                    config.CreateMap<TestItemDTO, TestItem>();
                }));

                mapper.Map(
                    new TestItemDTO
                    {
                        TestItemChildId = 1,
                    },
                    item);

                await dbContext.SaveChangesAsync();
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.Equal(1, count);
            }

            connection.Dispose();

            TestDbContext CreateDbContext()
                => new TestDbContext(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options);
        }
    }
}
