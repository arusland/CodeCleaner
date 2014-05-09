using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CodeCleaner.Test.Test
{
    [Obsolete]
    [ComVisible(true)]
    internal class AttributesTest
    {
        [Flags]
        public enum FlagEnum
        {
            FlagOne = 1,
            FlagTwo = 2,
            FlagTree = 4
        }
        
        [Serializable]
        private struct StructPrivate
        {
            [Obsolete]
            int int1;
        }        

        [Obsolete("Some method", false)]
        public string[,] Method1()
        {
            return null;
        }

        [ComVisible(true)]
        [Obsolete("Common property")]
        public int Property1
        {
            get;
            private set;
        }
    }

    [Obsolete]class SmallClass
    {
        [Obsolete]
        private static string[,] ALL_Features;

        [Obsolete]
        private static string[,] FEATURES = new string[,] {{"1", "Saving items to db"},
                {"2", "Allows updating of software"}, 
                {"3", "Orygin Service Control"},
                {"4", "Allows using plugins"}};
    }

    [Obsolete]
    [ComVisible(true)]
    internal abstract class AttributesAbstractTest : IList<int>
    {
        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Index property")]
        public int this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    [ComVisible(false)]
    public interface SomeInterface : ICloneable
    {
    }
}
