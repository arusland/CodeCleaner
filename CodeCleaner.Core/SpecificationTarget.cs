using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orygin.Shared.Minimal.Helpers;
using Orygin.Shared.Minimal.Extensions;

namespace CodeCleaner
{
    public class SpecificationTarget : ISpecificationTarget
    {
        #region Ctors

        public SpecificationTarget(BindingType bindingType, SortType sortType, IList<Region> regions, int maxChildrenCount, 
            string nameConvention, bool regionsOnly, Order typesOrder)
        {
            Checker.NotNull(regions, "regions");
            Checker.NotNull(nameConvention, "nameConvention");
            Checker.AreNotEqual<BindingType>(BindingType.None, bindingType);

            SortType = sortType;
            BindingType = bindingType;
            Regions = regions;
            MaxBlocksCount = maxChildrenCount;
            RegionsOnly = regionsOnly;
            NameConvention = nameConvention;
            TypesOrder = typesOrder;
        }
        
        #endregion

        #region ISpecificationTarget

        public SortType SortType
        {
            get;
            private set;
        }

        public BindingType BindingType
        {
            get;
            private set;
        }

        public IList<Region> Regions
        {
            get;
            private set;
        }

        public int MaxBlocksCount
        {
            get;
            private set;
        }

        public bool RegionsOnly
        {
            get;
            private set;
        }

        public string NameConvention
        {
            get;
            private set;
        }

        public Order TypesOrder
        {
            get;
            private set;
        }

        #endregion
    }
}
