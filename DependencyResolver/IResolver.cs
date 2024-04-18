namespace Venus.DependencyResolver
{
    public interface IResolver
    {
        public void RegisterDefault();
        public object Resolve(Type type);
    }
}
