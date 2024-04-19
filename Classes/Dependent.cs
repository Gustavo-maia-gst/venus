using Venus.DependencyResolver;

namespace Venus.Classes
{
    public class Dependent
    {
        private readonly IRunner _runner;

        public Dependent(IRunner runner)
        {
            _runner = runner;
        }

        public void RunRunner()
        {
            _runner.Run();
        }
    }
}
