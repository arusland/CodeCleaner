using System;

namespace CodeCleaner.Test.Test2
{
    internal class TestAccessModifiers
    {
        #region Methods

        #region Protected

        protected internal void FooMethod()
        {
            throw new NotImplementedException("JUST FOR TEST!");
        }
        
        internal protected void FooMethod2()
        {
            throw new NotImplementedException("JUST FOR TEST!");
        }

        #endregion
        
        #endregion
    }
}
