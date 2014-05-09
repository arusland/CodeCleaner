using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

//////////////////////////////////////////////////
// This namespace declared just for test CodeCleaner
//////////////////////////////////////////////////
namespace DummyName.Test
{
    delegate void DefaultDelegate(object sender, EventArgs e);
    internal delegate void InternalDelegate(object sender, EventArgs e);
    internal delegate void GenericDelegate<T>(object sender, IList<T> list);

    /// <summary>
    /// DummyClass1 comment
    /// </summary>
    public abstract class DummyClass1
    {
        #region Enums

        private enum PrivateEnum
        {
            Do1,
            Do2
        }

        #endregion

        #region Ctors

        public DummyClass1()
        {
            #region Region1

            #region Region1_1

            if (CONST_Int == 12)
            {
                Val_Bin = 444;
            }

            #endregion

            #endregion
            // empty public constructor
        }

        DummyClass1(int int1)
        {
            // empty private constructor
        }

        static DummyClass1()
        {
            // empty static constructor
        }

        #endregion

        #region Constants

        private const int CONST_Int = 99999;
        private const string CONST_string = "Just dummy const string";
        protected string NAME_NotConstant = "Just field declared \"in''' Constants region";
        private const long CONST_Long = 12;
        private bool justForDebug = 12 == CONST_Int;
        static int Val_IntDefault = 12;
        public abstract void MethodAbstract(object sender, EventArgs e);

        static
            private
            int Val_Private = 12;
        private static int Val_PrivateStatic = 12;
        const int Const_Default = 12;
        public static readonly int _ReaonlyStaticInt;
        public
            event EventHandler OnPublicEvent
            ;
        private static void MethodStaticPrivate(object sender, EventArgs e)
        {
            Debug.WriteLine("dummy output" + e.ToString());
        }

        event EventHandler OnDefaultEvent;
        static event EventHandler OnDefaultStaticEvent;
        static public event EventHandler OnPublicStaticEvent;
        int Val_Bin = ~
            (1 + 2);
        internal IList<IList<string>> list;

        #endregion

        public abstract String DefaultAbstractProperty
        {
            get;
            set;
        }
    }

    /// <summary>
    /// DummyClass2 comment
    /// </summary>
    public partial class DummyClass2 : EventArgs
    {
        #region Structures

        public partial struct InnerDummyStruct
        {
            public InnerDummyStruct(int hy)
            {
                //cdsfdfdsf
            }
        }

        #endregion

        public DummyClass2()
            : this(34)
        {
        }

        public DummyClass2(int dig)
        {
        }

        string GetSome<T>(T type, string fg)
        {
            return type.ToString() + fg;
        }

        public String PublicProperty
        {
            get;
            set;
        }

        private static String PrivatePropertyStatic
        {
            get;
            set;
        }

        public virtual String DefaultVirtualProperty
        {
            get;
            set;
        }

    }

    internal struct PureStruct
    {
        public PureStruct(string input)
        {

        }
    }

    public partial interface PublicInterface
    {
        void InterfaceMethod();

        IList<string> InterfaceProperty
        {
            get;
        }

        event EventHandler OnSomeEvent;
    }

    public abstract class SomeGenericClass<T>
    {
        private T _FieldPrivate;

        public abstract string this[T value, int index]
        {
            get;
        }


        public string this[T value, int index, string sparam]
        {
            get
            {
                return string.Empty; 
            }
        }

        public T GetValue()
        {
            return _FieldPrivate;
        }

        public bool GetValue2<T, K>()
        {
            return false;
        }
    }
}

