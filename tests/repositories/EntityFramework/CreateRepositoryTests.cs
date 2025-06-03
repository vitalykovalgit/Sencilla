
namespace Sencilla.Repository.EntityFramework.Tests
{

    public class CreateRepositoryTests
    {
        User CurrentUser = new User { Id = 1, Roles = new List<UserRole> { new UserRole { Id = 1, Role = "user" } } };

        Mock<IResolver> Resolver = new ();
        Mock<ICurrentUserProvider> CurrentUserProvider = new ();

        DbContextOptions<DynamicDbContext> DbOptions;
        RepositoryRegistrator RepoRegistrator = new();

        RepositoryDependency? RepositoryDependency;

        [SetUp]
        public void Setup()
        {
            /*
             * Setup DBs
             **/
            DbOptions = new DbContextOptionsBuilder<DynamicDbContext>()
                 .UseInMemoryDatabase(databaseName: "Picassa")
                 .Options;

            // Mock DB context and setup it
            RepoRegistrator.Entities.Add(typeof(Entity));
            // Insert seed data into the database using one instance of the context
            //using (var context = new DynamicDbContext(options, registrator))
            //{
            //    context.Movies.Add(new Movie { Id = 1, Title = "Movie 1", YearOfRelease = 2018, Genre = "Action" });
            //    context.Movies.Add(new Movie { Id = 2, Title = "Movie 2", YearOfRelease = 2018, Genre = "Action" });
            //    context.Movies.Add(new Movie { Id = 3, Title = "Movie 3", YearOfRelease = 2019, Genre = "Action" });
            //    context.SaveChanges();
            //}


            /*
             * Setup Security 
             **/
            // setup current user 
            var currentUser = new User { Id = 1, Roles = new List<UserRole> { new UserRole { Id = 1, Role = "user" } } };
            CurrentUserProvider.Setup(o => o.CurrentUser).Returns(currentUser);

            // setup matrix 
            var matrix = new Mock<IReadRepository<Matrix>>();
            matrix.Setup(m => m.GetAll(null, CancellationToken.None).Result).Returns(new List<Matrix> {
                new Matrix { Role = "owner" /**/, Action = (int)Component.Security.Action.Create, Resource = nameof(Entity), Constraint = "" }
            });
            Resolver.Setup(r => r.Resolve<IReadRepository<Matrix>>()).Returns(matrix.Object);

            // setup security constarint 
            //var secureConstraint = new SecurityConstraint(CurrentUserProvider.Object, Resolver.Object);
            //Resolver.Setup(r => r.Resolve<IEnumerable<IReadConstraint>>()).Returns(new[] { secureConstraint });

            // setup repo dependency 
            var events = new Mock<IEventDispatcher>();
            var commands = new Mock<ICommandDispatcher>();
            //RepositoryDependency = new RepositoryDependency(Resolver.Object, events.Object, commands.Object);
        }

        [Test]
        public async Task EntityCreateWithConstraints_ShouldBeCreated()
        {
            // Test repo 
            var repo = new CreateRepository<Entity, DynamicDbContext>(RepositoryDependency, new DynamicDbContext(DbOptions, RepoRegistrator));
            var created = await repo.Create(new Entity 
            {
                Id = 1,
                Name = "Jonh",
            });

            // Test result
        }
    }
}