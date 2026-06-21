using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class UserAdminServiceTests
    {
        [Fact]
        public async Task GetAllAsync_MapsUsersWithRolesAndBanStatus()
        {
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.GetAllAsync()).ReturnsAsync(new List<ClashUserDomain>
            {
                new() { Id = "1", Email = "a@x.pl", UserName = "alpha", IsBanned = false },
                new() { Id = "2", Email = "b@x.pl", UserName = "beta", IsBanned = true }
            });
            users.Setup(u => u.GetRolesAsync("1")).ReturnsAsync(new List<string> { "Admin" });
            users.Setup(u => u.GetRolesAsync("2")).ReturnsAsync(new List<string>());

            var result = await new UserAdminService(users.Object).GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("alpha", result[0].UserName);
            Assert.Contains("Admin", result[0].Roles);
            Assert.True(result[1].IsBanned);
        }

        [Fact]
        public async Task BanAsync_DelegatesToRepository()
        {
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.BanAsync("1")).ReturnsAsync(true);

            Assert.True(await new UserAdminService(users.Object).BanAsync("1"));
            users.Verify(u => u.BanAsync("1"), Times.Once);
        }

        [Fact]
        public async Task AddToRoleAsync_DelegatesToRepository()
        {
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.AddToRoleAsync("1", "Organizer")).ReturnsAsync(true);

            Assert.True(await new UserAdminService(users.Object).AddToRoleAsync("1", "Organizer"));
            users.Verify(u => u.AddToRoleAsync("1", "Organizer"), Times.Once);
        }
    }
}
