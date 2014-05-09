using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public class CodeCleanerException : Exception
    {
        #region Ctors

        public CodeCleanerException(ExceptionCode code)
            : base(GetMessageFromCode(code))
        {
            Code = code;
        }

        public CodeCleanerException(ExceptionCode code, int line, string fileName)
            :this(code)
        {
            Line = line;
            Filename = fileName;
        }

        public CodeCleanerException(Exception exception)
            : base(exception.Message, exception)
        {
            Code = ExceptionCode.UnknownException;
        }
        
        #endregion

        #region Properties
        
        #region Public

        /// <summary>
        /// Exception code
        /// </summary>
        public ExceptionCode Code
        {
            get;
            private set;
        }

        /// <summary>
        /// Line number in file.
        /// </summary>
        public int Line
        {
            get;
            private set;
        }

        /// <summary>
        /// Full path to file.
        /// </summary>
        public string Filename
        {
            get;
            set;
        }
        
        #endregion
        
        #endregion
        
        #region Methods

        public static string GetMessageFromCode(ExceptionCode code)
        {
            switch(code)
            {
                case ExceptionCode.DoubleNamespaceDeclaration:
                    return "File should not contain two or more namespace declaration";
                case ExceptionCode.NamespaceNotFound:
                    return "Namespace not found in file";
                case ExceptionCode.InvalidFieldFound:
                    return "Invalid field found";
                case ExceptionCode.InvalidMethodFound:
                    return "Invalid method found.";
                case ExceptionCode.InvalidOperatorFound:
                    return "Invalid operator found.";
                case ExceptionCode.InvalidConstructorFound:
                    return "Invalid constructor found.";
                case ExceptionCode.InvalidDestructorFound:
                    return "Invalid destructor found.";
                case ExceptionCode.InvalidPropertyFound:
                    return "Invalid property found.";
                case ExceptionCode.InvalidClassFound:
                    return "Invalid class found.";
                case ExceptionCode.InvalidStructFound:
                    return "Invalid structure found.";
                case ExceptionCode.InvalidInterfaceFound:
                    return "Invalid interface found.";
                case ExceptionCode.InvalidEnumFound:
                    return "Invalid enum found.";
                default:
                    throw new InvalidOperationException("Unsupported ExceptionCode: " + code.ToString());
            }
        }
        
        #endregion
    }
}
