using Orygin.Shared.Minimal.Helpers;
using Orygin.Shared.Minimal.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeCleaner
{
    public class Region
    {
        #region Ctors

        public Region(string name, IList<RegionType> types, IList<ModificatorType> modificators,
            string nameConvention, Order innerRegionsOrder, Order typesOrder, string regionNameConvention, bool allowFieldAssign, int maxRegionRepeatCount, SortType sort)
        {
            Checker.NotNullOrEmpty(name, "name");

            Name = name;
            Types = types;
            Modificators = modificators;
            NameConvention = nameConvention;
            InnerRegionsOrder = innerRegionsOrder;
            TypesOrder = typesOrder;
            RegionNameConvention = regionNameConvention;
            MaxRegionRepeatCount = maxRegionRepeatCount;
            AllowFieldAssign = allowFieldAssign;
            SortType = sort;
        }

        #endregion

        #region Properties

        #region Public

        public bool AllowFieldAssign
        {
            get;
            private set;
        }

        public bool HasInnerRegionsOrder
        {
            get
            {
                return InnerRegionsOrder.IsNotNull();
            }
        }

        public Order InnerRegionsOrder
        {
            get;
            private set;
        }

        public int MaxRegionRepeatCount
        {
            get;
            private set;
        }

        public IList<ModificatorType> Modificators
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string NameConvention
        {
            get;
            private set;
        }

        public string RegionNameConvention
        {
            get;
            private set;
        }

        public SortType SortType
        {
            get;
            private set;
        }

        public IList<RegionType> Types
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

        #endregion

        #region Methods

        #region Public

        public bool CheckNameConvention(string name)
        {
            Checker.NotNullOrEmpty(name);

            if (NameConvention.IsNullOrEmpty())
            {
                return false;
            }

            return Regex.IsMatch(name, NameConvention, RegexOptions.Singleline);
        }

        public bool CheckRegionNameConvention(string name)
        {
            Checker.NotNullOrEmpty(name);

            if (RegionNameConvention.IsNullOrEmpty())
            {
                return false;
            }

            return Regex.IsMatch(name, RegionNameConvention, RegexOptions.Singleline);
        }

        /// <summary>
        /// Create new Region with inherited properties from another region
        /// </summary>
        /// <returns></returns>
        public Region Synthesize(Region parent)
        {
            if (parent.IsNull())
            {
                return new Region(Name,
                                Types,
                                Modificators,
                                NameConvention,
                                InnerRegionsOrder,
                                TypesOrder,
                                string.Empty,
                                AllowFieldAssign,
                                1, 
                                SortType);
            }

            return new Region(Name,
                Types.Any() ? Types : parent.Types,
                Modificators.Any() ? Modificators : parent.Modificators,
                NameConvention.IsNotNullOrEmpty() ? NameConvention : parent.NameConvention,
                InnerRegionsOrder,
                TypesOrder,
                string.Empty,
                AllowFieldAssign,
                1, 
                SortType != SortType.None ? SortType : parent.SortType);
        }

        public override string ToString()
        {
            return string.Format("Region: {0}", Name);
        }

        #endregion

        #endregion
    }
}
