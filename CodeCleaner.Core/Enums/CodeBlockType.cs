using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public enum CodeBlockType
    {
        None = 0,
        Namespace,
        Class,
        Interface,
        Structure,
        Enum,
        Region,
        Comment,
        SingleLineDirective,
        Using,
        Field,
        Property,
        IndexProperty,
        DependencyProperty,
        RoutedEvent,
        Const,
        Event,
        Constructor,
        Destructor,
        Method,
        Operator,
        Delegate,
        Attribute,
        PlainText
    }
}
