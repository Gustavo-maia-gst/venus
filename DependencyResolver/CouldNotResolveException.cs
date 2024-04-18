namespace Venus.DependencyResolver
{
    public class CouldNotResolveException : Exception
    {
        private static readonly string template = "Could not resolve the dependency of type {0}";
        public CouldNotResolveException(Type type) : base(string.Format(template, type.FullName)) { }
        public CouldNotResolveException(Type type, string msg) : base(string.Format(template, type.FullName) + ": " + msg) { }
    }
}
