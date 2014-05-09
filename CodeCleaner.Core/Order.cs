using Orygin.Shared.Minimal.Helpers;
using System.Collections.Generic;

namespace CodeCleaner
{
    public class Order
    {
        #region Ctors

        public Order(string name, IList<Region> regions)
        {
            Checker.NotNullOrEmpty(name, "name");
            Checker.NotNull(regions, "regions");

            Name = name;
            Regions = regions;
        }

        #endregion

        #region Properties

        public string Name
        {
            get;
            private set;
        }

        public IList<Region> Regions
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        #region Public

        public override string ToString()
        {
            return string.Format("Order: {0}", Name);
        }

        #endregion

        #endregion
    }
}
