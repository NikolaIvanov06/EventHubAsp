using System;
using System.Threading.Tasks;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class IdentitySeederTests : IDisposable
    {
        private Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
        private Mock<UserManager<User>> _mockUserManager;
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            _mockRoleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
                new Mock<IRoleStore<IdentityRole<Guid>>>().Object, null, null, null, null);
            _mockUserManager = GetMockUserManager();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_mockRoleManager.Object);
            serviceCollection.AddSingleton(_mockUserManager.Object);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private static Mock<UserManager<User>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }


        
        [Test]
        public async Task SeedRolesAndUsers_ShouldCreateUsers_WhenUsersDoNotExist()
        {
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            await IdentitySeeder.SeedRolesAndUsers(_serviceProvider);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Exactly(3));
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Exactly(3));
        }



        [Test]
        public async Task SeedRolesAndUsers_ShouldNotCreateUsers_WhenUsersAlreadyExist()
        {
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User());

            await IdentitySeeder.SeedRolesAndUsers(_serviceProvider);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [TearDown]
        public void Teardown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            Teardown();
        }
    }
}