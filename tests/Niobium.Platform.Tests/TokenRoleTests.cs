using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Niobium.Platform.Identity;

namespace Niobium.Platform.Tests
{
    [TestClass]
    public sealed class TokenRoleTests
    {
        private static readonly ServiceProvider Services = CreateServices();

        private static IdentityServiceOptions Configuration() => new()
        {
            AccessTokenSecret = new string('s', 64),
            IDTokenPublicKey = "synthetic-unused-public-key",
            AccessTokenIssuer = "https://identity.example.invalid",
            AccessTokenAudience = "synthetic-role-tests",
        };

        private static ServiceProvider CreateServices()
        {
            ServiceCollection services = new();
            services.AddIdentity(options =>
            {
                IdentityServiceOptions configured = Configuration();
                options.AccessTokenSecret = configured.AccessTokenSecret;
                options.IDTokenPublicKey = configured.IDTokenPublicKey;
                options.AccessTokenIssuer = configured.AccessTokenIssuer;
                options.AccessTokenAudience = configured.AccessTokenAudience;
            });
            return services.BuildServiceProvider();
        }

        [ClassCleanup]
        public static void Cleanup() => Services.Dispose();

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task Assigned_roles_round_trip_without_adding_other_authority(int count)
        {
            string[] all = ["Default", "NOTIFICATION-OPERATOR", "BILLER"];
            string[] assigned = all[..count];
            Guid tenant = Guid.NewGuid();
            string actor = Guid.NewGuid().ToString();
            string token = await Services.GetRequiredService<ITokenBuilder>().BuildAsync(tenant, actor, roles: assigned);
            PrincipalParser parser = new(Options.Create(Configuration()));
            ClaimsPrincipal principal = await parser.ParseAsync(token);
            CollectionAssert.AreEqual(assigned, principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());
            Assert.AreEqual(actor, principal.FindFirst(ClaimTypes.Sid)!.Value);
            Assert.AreEqual(tenant.ToString(), principal.FindFirst(ClaimTypes.GroupSid)!.Value);
            foreach (string role in assigned)
                Assert.IsTrue(principal.IsInRole(role));
            Assert.IsFalse(principal.IsInRole("UNASSIGNED-ROLE"));
            Assert.IsFalse(principal.IsInRole("notification-operator"));

            using JsonDocument payload = JsonDocument.Parse(Base64UrlEncoder.Decode(token.Split('.')[1]));
            bool present = payload.RootElement.TryGetProperty(ClaimTypes.Role, out JsonElement wireRoles);
            Assert.AreEqual(count > 0, present);
            if (count == 1)
            {
                Assert.AreEqual(JsonValueKind.String, wireRoles.ValueKind);
                Assert.AreEqual(assigned[0], wireRoles.GetString());
            }
            else if (count > 1)
            {
                Assert.AreEqual(JsonValueKind.Array, wireRoles.ValueKind);
                CollectionAssert.AreEqual(assigned, wireRoles.EnumerateArray().Select(r => r.GetString()!).ToArray());
            }
        }

        [TestMethod]
        public async Task Omitted_roles_do_not_create_a_role_claim()
        {
            string token = await Services.GetRequiredService<ITokenBuilder>().BuildAsync(Guid.NewGuid(), Guid.NewGuid().ToString());
            ClaimsPrincipal principal = await new PrincipalParser(Options.Create(Configuration())).ParseAsync(token);
            Assert.IsFalse(principal.Claims.Any(c => c.Type == ClaimTypes.Role));
        }

        [TestMethod]
        [DataRow("issuer")]
        [DataRow("audience")]
        [DataRow("key")]
        public async Task Multiple_roles_do_not_bypass_token_validation(string mismatch)
        {
            string token = await Services.GetRequiredService<ITokenBuilder>().BuildAsync(Guid.NewGuid(), Guid.NewGuid().ToString(),
                roles: ["Default", "NOTIFICATION-OPERATOR"]);
            IdentityServiceOptions configured = Configuration();
            if (mismatch == "issuer")
                configured.AccessTokenIssuer = "https://other.example.invalid";
            else if (mismatch == "audience")
                configured.AccessTokenAudience = "another-application";
            else
                configured.AccessTokenSecret = new string('x', 64);
            PrincipalParser parser = new(Options.Create(configured));
            Niobium.ApplicationException failure = await Assert.ThrowsExactlyAsync<Niobium.ApplicationException>(
                async () => await parser.ParseAsync(token));
            Assert.IsNotNull(failure);
        }
    }
}
