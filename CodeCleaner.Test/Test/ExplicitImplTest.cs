using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner.Test.Test
{
    public interface ISomeInterface<T>
    {
        void CreateBoo<T>();

        T GetBoo(int index);

        IList<IList<string>> this[string name]
        {
            get;
            set;
        }

        string[,]SimpleProperty
        {
            get;
        }

        Dictionary<T, TG> GetList<T, TG>();

        event EventHandler<EventArgs> SimpleEvent;
    }

    public class ExplicitImplTest : ISomeInterface<string>
    {
        void ISomeInterface<string>.CreateBoo<T>()
        {
            throw new NotImplementedException();
        }

        string ISomeInterface<string>.GetBoo(int index)
        {
            throw new NotImplementedException();
        }

        

        Dictionary<T, TG>ISomeInterface<string>.GetList<T, TG>()
        {
            throw new NotImplementedException();
        }

        ISomeInterface<string>GetList2<Z>()
        {
            throw new NotImplementedException();
        }

        string[,] GetList3<Z>()
        {
            throw new NotImplementedException();
        }

        string[,]ISomeInterface<string>.SimpleProperty
        {
            get { throw new NotImplementedException(); }
        }

        ISomeInterface<string>SimpleProperty2
        {
            get { throw new NotImplementedException(); }
        }

        IList<IList<string>>ISomeInterface<string>.this[string name]
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


        event EventHandler<EventArgs>ISomeInterface<string>.SimpleEvent
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }
    }
}
