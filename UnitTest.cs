using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AutoMapper;
using System.Linq;
using System.Data.Common;

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
        private readonly DbConnection connection;

        public UnitTest()
        {
            this.connection = new SqliteConnection("Filename=:memory:");
            this.connection.Open();
        }

        [Fact]
        public async Task AutoMapper_ShouldNotDelete()
        {
            using (var dbContext = CreateDbContext())
            {
                await this.SeedDbContext(dbContext);
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 before update");
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

                Assert.True(count == 1, "Expected count to be 1 after update");
            }
        }

        [Fact]
        public async Task ManualMapping_ShouldNotDelete()
        {
            using (var dbContext = CreateDbContext())
            {
                await this.SeedDbContext(dbContext);
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 before update");
            }

            // Update test value
            using(var dbContext = CreateDbContext())
            {
                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstAsync();

                item.TestItemChild = null;
                item.TestItemChildId = 1;

                await dbContext.SaveChangesAsync();
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 after update");
            }
        }

        [Fact]
        public async Task ManualMapping_IngoreNavProp_NoChange_ShouldNotDelete()
        {
            using (var dbContext = CreateDbContext())
            {
                await this.SeedDbContext(dbContext);
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 before update");
            }

            // Update test value
            using(var dbContext = CreateDbContext())
            {
                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstAsync();
                
                item.TestItemChildId = 1;

                await dbContext.SaveChangesAsync();
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 after update");

                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstOrDefaultAsync();

                Assert.NotNull(item);
                Assert.True(item.TestItemChildId == 1, "Expected TestItemChildId to be 1");
                Assert.True(item.TestItemChild.Id == 1, "Expected TestItemChild.Id to be 1");
            }
        }
        
        [Fact]
        public async Task ManualMapping_IngoreNavProp_Change_ShouldNotDelete()
        {
            using (var dbContext = CreateDbContext())
            {
                await this.SeedDbContext(dbContext);
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 before update");
            }

            // Update test value
            using(var dbContext = CreateDbContext())
            {
                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstAsync();

                item.TestItemChildId = 2;

                await dbContext.SaveChangesAsync();
            }

            // There should be one item in the TestItems table
            using(var dbContext = CreateDbContext())
            {
                var count = await dbContext.TestItems.CountAsync();

                Assert.True(count == 1, "Expected count to be 1 after update");

                var item = await dbContext.TestItems
                    .Include(e => e.TestItemChild)
                    .FirstOrDefaultAsync();

                Assert.NotNull(item);
                Assert.True(item.TestItemChildId == 2, "Expected TestItemChildId to be 2");
                Assert.True(item.TestItemChild.Id == 2, "Expected TestItemChild.Id to be 2");
            }
        }

        private TestDbContext CreateDbContext()
            => new TestDbContext(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(this.connection)
                    .Options);
        
        private async Task SeedDbContext(TestDbContext dbContext)
        {
            // Create tables in database
            await dbContext.Database.EnsureCreatedAsync();

            // Insert test value
            dbContext.TestItems.Add(new TestItem
            {
                Id = 1,
                TestItemChild = new TestItemChild
                {
                    Id = 1,
                }
            });
           
            dbContext.TestItemChildren.Add(new TestItemChild
            {
                Id = 2,
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
