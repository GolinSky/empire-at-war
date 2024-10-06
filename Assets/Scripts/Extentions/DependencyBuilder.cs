using Zenject;

namespace EmpireAtWar.Extentions
{
    public abstract class DependencyBuilder<TInheritor>:IDependencyBuilder where TInheritor: class, IDependencyBuilder
    {
        private string prefixPath;
        private string postfixPath;
        
        protected DiContainer Container { get; private set; }
        protected string PathToFile { get; private set; }


        protected DependencyBuilder(DiContainer container)
        {
            Container = container;
        }
        
        public TInheritor BuildPathToFile(string customName) 
        {
            PathToFile = customName;
            return this as TInheritor;
        }

        public TInheritor AppendToPath(string prefix, string postfix)
        {
            prefixPath = prefix ?? string.Empty;
            postfixPath = postfix ?? string.Empty;
            return this as TInheritor;
        }

        protected void ConstructName<T>()
        {
            if (PathToFile == null || PathToFile.Equals(string.Empty))
            {
                PathToFile = $"{prefixPath}{typeof(T).Name}{postfixPath}";
            }
        }

    }
}