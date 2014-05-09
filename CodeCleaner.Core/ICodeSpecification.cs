using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ICodeSpecification
    {
        IList<ISpecificationTarget> Targets
        {
            get;
        }

        IList<Order> Orders
        {
            get;
        }
    }
}
