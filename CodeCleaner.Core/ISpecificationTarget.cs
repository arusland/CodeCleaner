using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ISpecificationTarget
    {
        BindingType BindingType
        {
            get;
        }

        /// <summary>
        /// Maximum inner blocks count; default 0 - without limit
        /// </summary>
        int MaxBlocksCount
        {
            get;
        }

        string NameConvention
        {
            get;
        }

        IList<Region> Regions
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        bool RegionsOnly
        {
            get;
        }

        SortType SortType
        {
            get;
        }

        Order TypesOrder
        {
            get;
        }
    }
}
