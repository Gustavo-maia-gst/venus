using Venus.DependencyResolver;

namespace Venus.Classes
{
    public class OperacaoRunner : IRunner, ITransientDependency
    {
        public void Run()
        {
            Console.WriteLine("I'm Running baby");
        }
    }
}
