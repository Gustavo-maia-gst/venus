using System.Linq.Expressions;
using Venus.DependencyResolver;

namespace Venus.Builder
{
    public class BuilderApplication
    {
        public readonly WebApplication App;
        private readonly IResolver _resolver;

        private readonly WebApplicationBuilder _builder;

        public BuilderApplication(string[] args, Action<WebApplicationBuilder> config)
        {
            _resolver = new Resolver();

            _builder = WebApplication.CreateBuilder(args);
            config(_builder);
            App = _builder.Build();
        }

        public void UseDependencyInjection()
        {
            _resolver.RegisterDefault();
        }

        public object GetInstance(Type type)
        {
            return _resolver.Resolve(type);
        }
    }
}
