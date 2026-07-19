using Zenject;

namespace EmpireAtWar.Extentions
{
    public abstract class DependencyBuilder<TInheritor>:IDependencyBuilder where TInheritor: class, IDependencyBuilder
    {
        private string _prefixPath;
        private string _postfixPath;
        
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
            _prefixPath = prefix ?? string.Empty;
            _postfixPath = postfix ?? string.Empty;
            return this as TInheritor;
        }

        protected void ConstructName<T>()
        {
            if (PathToFile == null || PathToFile.Equals(string.Empty))
            {
                PathToFile = $"{_prefixPath}{typeof(T).Name}{_postfixPath}";
            }
        }

    }
}