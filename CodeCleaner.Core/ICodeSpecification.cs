using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ICodeSpecification
    {
        IList<Order> Orders
        {
            get;
        }

        IList<ISpecificationTarget> Targets
        {
            get;
        }
    }
}
