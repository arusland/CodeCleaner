using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeCleaner.Test
{
    [TestClass]
    public class CodeCleanerTest
    {
        #region Fields (1)

        private string _templatesPath;

        #endregion Fields

        #region Methods

        [TestMethod]
        public void DoubleNamespaceTest()
        {
            CheckTemplate("TestDoubleNameSpace.cs", ExceptionCode.DoubleNamespaceDeclaration);
        }


        [TestInitialize]
        public void Init()
        {
            _templatesPath = @"..\..\..\CodeCleaner.Test\Test\";
        }

        [TestMethod]
        public void NamespaceNotFoundTest()
        {
            CheckTemplate("TestNameSpaceNotFound.cs", ExceptionCode.NamespaceNotFound);
        }

        private void CheckTemplate(string templateName, ExceptionCode expectedCode)
        {
            CodeParser parser = new CodeParser();

            try
            {
                parser.Parse(_templatesPath + templateName);
            }
            catch (CodeCleanerException ex)
            {
                Assert.AreEqual(expectedCode, ex.Code);
            }
        }

        #endregion Methods
    }
}
