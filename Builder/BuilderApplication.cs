using System.Linq.Expressions;
using Venus.DependencyResolver;

namespace Venus.Builder
{
    public class BuilderApplication
    {
        public readonly WebApplication App;
        public readonly IResolver Resolver;

        private readonly WebApplicationBuilder _builder;

        public BuilderApplication(string[] args, Action<WebApplicationBuilder> config)
        {
            Resolver = new Resolver();

            _builder = WebApplication.CreateBuilder(args);
            config(_builder);
            App = _builder.Build();
        }

        public void UseDependencyInjection()
        {
            Resolver.RegisterDefault();
        }
    }
}
