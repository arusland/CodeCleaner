using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ISpecificationTarget
    {
        SortType SortType
        {
            get;
        }

        BindingType BindingType
        {
            get;
        }

        IList<Region> Regions
        {
            get;
        }

        string NameConvention
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

        /// <summary>
        /// 
        /// </summary>
        bool RegionsOnly
        {
            get;
        }

        Order TypesOrder
        {
            get;
        }
    }
}
