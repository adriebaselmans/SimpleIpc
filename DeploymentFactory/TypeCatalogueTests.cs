using System;
using NUnit.Framework;

namespace DeploymentFactory
{
    [TestFixture]
    public class TypeCatalogueTests
    {
        public interface IFoo
        {
            void Bar();
        }

        public class Foo : IFoo
        {
            public void Bar()
            {

            }
        }

        [TearDown]
        public void TearDown()
        {
            TypeCatalogue.Clear();
        }
        
        [Test]
        public void GivenInterfaceAndImpl_WhenRegisteringWithContainer_ThenInstanceCanBeRetrievedByType()
        {
            TypeCatalogue.Register<IFoo>(new Foo());
            var instance = TypeCatalogue.Resolve<IFoo>();

            Assert.NotNull(instance);
            Assert.IsTrue(instance is IFoo);
        }

        [Test]
        public void GivenInterfaceAndImpl_WhenRegisteringTwiceWithContainer_ThenExceptionIsThrown()
        {
            TypeCatalogue.Register<IFoo>(new Foo());
            Assert.Catch<ArgumentException>(() => { TypeCatalogue.Register<IFoo>(new Foo()); });            
        }

        [Test]
        public void GivenInterfaceAnDifferentImpl_WhenRegisteringTwiceWithContainer_ThenExceptionIsThrown()
        {
            Assert.Catch<ArgumentException>(() => { TypeCatalogue.Register<IFoo>(new Object()); });
        }
    }
}