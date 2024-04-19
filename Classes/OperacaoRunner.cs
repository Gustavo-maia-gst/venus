using Venus.DependencyResolver;

namespace Venus.Classes
{
    public class OperacaoRunner : IRunner, ITransientDependency
    {
        private readonly Dependent dependent;

        public OperacaoRunner(Dependent dependent)
        {
            this.dependent = dependent;
        }

        public void Run()
        {
            Console.WriteLine("I'm Running baby");
        }
    }
}
