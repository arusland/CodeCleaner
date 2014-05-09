using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeSpecification : ICodeSpecification
    {
        #region Constants

        private const char SEPARATOR_Value = ',';

        #endregion

        #region Fields

        private IList<Order> _Orders;

        #endregion

        #region Properties

        #region Public

        public IList<Order> Orders
        {
            get
            {
                return _Orders;
            }
            private set
            {
                _Orders = value;
            }
        }

        public IList<ISpecificationTarget> Targets
        {
            get;
            private set;
        }

        #endregion

        #endregion

        #region Methods

        #region Private

        private static string GetAttribute(XmlNode node, string name, string defaultValue)
        {
            string value = GetAttribute(node, name);

            if (value.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return value;
        }

        private static string GetAttribute(XmlNode node, string name)
        {
            XmlAttribute attr = node.Attributes[name];

            return attr.IsNotNull() ? attr.Value : string.Empty;
        }

        private static T GetAttributeEnum<T>(XmlNode node, string name, string defaultValue)
        {
            return GetAttribute(node, name, defaultValue).ParseEnum<T>();
        }

        private static T GetAttributeEnum<T>(XmlNode node, string name)
        {
            return GetAttribute(node, name).ParseEnum<T>();
        }

        private static int GetMaxRegionRepeatCount(string value)
        {
            if (value.Equals("*"))
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch (System.Exception ex)
            {
            	throw new InvalidOperationException("Invalid attribute value presented as integer." + ex.Message);
            }
        }

        private static IList<ModificatorType> GetModificators(string modificators)
        {
            string[] splited = modificators.Split(new char[] { SEPARATOR_Value });
            return splited.Where(p => p.IsNotNullOrEmpty()).Select(p => p.Trim())
                .Select(p => p.UpFirstChar().ParseEnum<ModificatorType>()).ToList();
        }

        private Order GetRegionOrder(XmlNode node, string orderRefName, Order defValue)
        {
            string orderName = GetAttribute(node, orderRefName);

            if (orderName.IsNotNullOrEmpty())
            {
                Order order = Orders.FirstOrDefault(p => p.Name == orderName);

                if (order.IsNull())
                {
                    throw new InvalidOperationException("Order not found: " + orderName);
                }

                return order;
            }
            else
            {
                return defValue;
            }
        }

        private static IList<RegionType> GetTypes(string types)
        {
            string[] splited = types.Split(new char[] { SEPARATOR_Value });
            return splited.Where(p => p.IsNotNullOrEmpty()).Select(p => p.Trim()).Select(p => p.ParseEnum<RegionType>()).ToList();
        }

        private Order LoadOrder(XmlNode node)
        {
            string orderName = GetAttribute(node, "Name");
            SortType sort = GetAttributeEnum<SortType>(node, "Sort", SortType.None.ToString());

            return new Order(orderName, LoadRegions(node.SelectNodes("Region"), sort, null));
        }

        private IList<Order> LoadOrders(XmlNodeList nodes)
        {
            List<Order> orders = new List<Order>();

            foreach (XmlNode node in nodes)
            {
                orders.Add(LoadOrder(node));
            }

            return orders;
        }

        private Region LoadRegion(XmlNode node, SortType sort, Order typesOrder)
        {
            return new Region(GetAttribute(node, "Name").Trim(),
                GetTypes(GetAttribute(node, "Types")),
                GetModificators(GetAttribute(node, "Modificators")),
                GetAttribute(node, "NameConvention"),
                GetRegionOrder(node, "InnerRegionsOrderRef", null),
                GetRegionOrder(node, "TypeOrderRef", typesOrder),
                GetAttribute(node, "RegionNameConvention"),
                bool.Parse(GetAttribute(node, "AllowFieldAssign", false.ToString())),
                GetMaxRegionRepeatCount(GetAttribute(node, "MaxRepeatCount", "1")),
                GetAttributeEnum<SortType>(node, "Sort", sort.ToString()));
        }

        private IList<Region> LoadRegions(XmlNodeList nodes, SortType sort, Order typesOrder)
        {
            List<Region> regions = new List<Region>();

            foreach (XmlNode node in nodes)
            {
                regions.Add(LoadRegion(node, sort, typesOrder));
            }

            return regions;
        }

        private ISpecificationTarget LoadTarget(XmlNode node)
        {
            SortType sort = GetAttributeEnum<SortType>(node, "Sort", SortType.None.ToString());
            Order typesOrder = GetRegionOrder(node, "TypeOrderRef", null);

            IList<Region> regions = LoadRegions(node.SelectNodes("Region"), sort, typesOrder);
            SpecificationTarget result = new SpecificationTarget(GetAttribute(node, "BindTo").UpFirstChar().ParseEnum<BindingType>(),
                sort, regions, Convert.ToInt32(GetAttribute(node, "MaxBlocksCount", "0")),
                GetAttribute(node, "NameConvention", string.Empty),
                bool.Parse(GetAttribute(node, "RegionsOnly", false.ToString())), 
                typesOrder);

            return result;
        }

        private IList<ISpecificationTarget> LoadTargets(XmlNodeList nodes)
        {
            List<ISpecificationTarget> targets = new List<ISpecificationTarget>();

            foreach (XmlNode node in nodes)
            {
                targets.Add(LoadTarget(node));
            }

            return targets;
        }

        #endregion

        #region Public

        public void Load(string configFilename)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(configFilename);
            Orders = LoadOrders(doc.DocumentElement.SelectNodes("Orders/Order"));
            Targets = LoadTargets(doc.DocumentElement.SelectNodes("Targets/Target"));
        }

        #endregion

        #endregion
    }
}
