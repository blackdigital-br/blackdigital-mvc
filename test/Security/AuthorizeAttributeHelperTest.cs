using BlackDigital.AspNet.Security;
using System.Reflection;
using AuthorizeAttribute = BlackDigital.Domain.Security.AuthorizeAttribute;

namespace BlackDigital.AspNet.Test.Security
{
    public class AuthorizeAttributeHelperTest
    {
        public interface IPublicService
        {
            Task<string> GetAsync();
        }

        public class PublicService : IPublicService
        {
            public Task<string> GetAsync() => Task.FromResult("value");
        }

        public interface IMethodAuthService
        {
            Task<string> OpenAsync();
            Task<string> SecuredAsync();
        }

        public class MethodAuthService : IMethodAuthService
        {
            public Task<string> OpenAsync() => Task.FromResult("open");

            [Authorize]
            public Task<string> SecuredAsync() => Task.FromResult("secured");
        }

        public interface IClassAuthService
        {
            Task<string> GetAllAsync();
            Task<string> GetOneAsync();
        }

        [Authorize(Roles = new[] { "Admin" })]
        public class ClassAuthService : IClassAuthService
        {
            public Task<string> GetAllAsync() => Task.FromResult("all");

            [Authorize(Policy = "Read")]
            public Task<string> GetOneAsync() => Task.FromResult("one");
        }

        public interface IExplicitAuthService
        {
            Task<string> SecuredAsync();
        }

        public class ExplicitAuthService : IExplicitAuthService
        {
            [Authorize(Policy = "Explicit")]
            Task<string> IExplicitAuthService.SecuredAsync() => Task.FromResult("explicit");
        }

        public abstract class AbstractService : IPublicService
        {
            public abstract Task<string> GetAsync();
        }

        public class ConcreteService : AbstractService
        {
            public override Task<string> GetAsync() => Task.FromResult("concrete");
        }

        private static MethodInfo Method<T>(string name) => typeof(T).GetMethod(name)!;

        [Fact]
        public void GetRequirements_WhenNoAttribute_ShouldReturnEmpty()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(PublicService), Method<IPublicService>(nameof(IPublicService.GetAsync)));

            Assert.Empty(requirements);
        }

        [Fact]
        public void GetRequirements_WhenOnlyMethodHasAttribute_ShouldReturnIt()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(MethodAuthService), Method<IMethodAuthService>(nameof(IMethodAuthService.SecuredAsync)));

            var requirement = Assert.Single(requirements);
            Assert.Null(requirement.Policy);
            Assert.Null(requirement.Roles);
        }

        [Fact]
        public void GetRequirements_WhenMethodHasNoAttribute_ShouldReturnEmpty()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(MethodAuthService), Method<IMethodAuthService>(nameof(IMethodAuthService.OpenAsync)));

            Assert.Empty(requirements);
        }

        [Fact]
        public void GetRequirements_WhenClassHasAttribute_ShouldApplyToEveryMethod()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(ClassAuthService), Method<IClassAuthService>(nameof(IClassAuthService.GetAllAsync)));

            var requirement = Assert.Single(requirements);
            Assert.Equal(new[] { "Admin" }, requirement.Roles);
        }

        [Fact]
        public void GetRequirements_WhenClassAndMethodHaveAttributes_ShouldReturnBoth()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(ClassAuthService), Method<IClassAuthService>(nameof(IClassAuthService.GetOneAsync)));

            Assert.Equal(2, requirements.Count);
            Assert.Contains(requirements, r => r.Roles is { Length: 1 } && r.Roles[0] == "Admin");
            Assert.Contains(requirements, r => r.Policy == "Read");
        }

        [Fact]
        public void GetRequirements_WhenExplicitImplementation_ShouldResolveMethodAttribute()
        {
            var requirements = AuthorizeAttributeHelper.GetRequirements(
                typeof(ExplicitAuthService), Method<IExplicitAuthService>(nameof(IExplicitAuthService.SecuredAsync)));

            var requirement = Assert.Single(requirements);
            Assert.Equal("Explicit", requirement.Policy);
        }

        [Fact]
        public void GetRequirements_ShouldReturnCachedInstance()
        {
            var method = Method<IClassAuthService>(nameof(IClassAuthService.GetAllAsync));

            var first = AuthorizeAttributeHelper.GetRequirements(typeof(ClassAuthService), method);
            var second = AuthorizeAttributeHelper.GetRequirements(typeof(ClassAuthService), method);

            Assert.Same(first, second);
        }

        [Fact]
        public void GetRequirements_WhenArgumentsAreNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AuthorizeAttributeHelper.GetRequirements(null!, Method<IPublicService>(nameof(IPublicService.GetAsync))));

            Assert.Throws<ArgumentNullException>(() =>
                AuthorizeAttributeHelper.GetRequirements(typeof(PublicService), null!));
        }

        [Fact]
        public void HasRequirements_WhenImplementationIsNull_ShouldReturnFalse()
        {
            var result = AuthorizeAttributeHelper.HasRequirements(
                null, Method<IMethodAuthService>(nameof(IMethodAuthService.SecuredAsync)));

            Assert.False(result);
        }

        [Fact]
        public void HasRequirements_WhenAttributeDeclared_ShouldReturnTrue()
        {
            var result = AuthorizeAttributeHelper.HasRequirements(
                typeof(MethodAuthService), Method<IMethodAuthService>(nameof(IMethodAuthService.SecuredAsync)));

            Assert.True(result);
        }

        [Fact]
        public void FindImplementation_ShouldReturnConcreteImplementation()
        {
            var candidates = new[] { typeof(AbstractService), typeof(ConcreteService) };

            var result = AuthorizeAttributeHelper.FindImplementation(typeof(IPublicService), candidates);

            Assert.Equal(typeof(ConcreteService), result);
        }

        [Fact]
        public void FindImplementation_WhenNoImplementation_ShouldReturnNull()
        {
            var result = AuthorizeAttributeHelper.FindImplementation(typeof(IPublicService), new[] { typeof(string) });

            Assert.Null(result);
        }

        [Fact]
        public void FindImplementation_WhenArgumentsAreNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AuthorizeAttributeHelper.FindImplementation(null!, Array.Empty<Type>()));

            Assert.Throws<ArgumentNullException>(() =>
                AuthorizeAttributeHelper.FindImplementation(typeof(IPublicService), null!));
        }

        [Fact]
        public void FindImplementationMethod_ShouldReturnMappedMethod()
        {
            var result = AuthorizeAttributeHelper.FindImplementationMethod(
                typeof(MethodAuthService), Method<IMethodAuthService>(nameof(IMethodAuthService.SecuredAsync)));

            Assert.NotNull(result);
            Assert.Equal(nameof(IMethodAuthService.SecuredAsync), result!.Name);
            Assert.Equal(typeof(MethodAuthService), result.DeclaringType);
        }
    }
}
