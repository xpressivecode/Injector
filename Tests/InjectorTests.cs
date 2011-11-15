using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XpressiveCode.Injection;

namespace Tests {
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class InjectorTests {
        public InjectorTests() {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestInitialize]
        public void Setup() {
            Injector.Mappings.Clear();

            Injector.Mappings.Add<IEntity>(() => {
                return new Person {
                    Age = 21,
                    Name = "John"
                };
            });
        }

        [TestMethod]
        public void SeperateInstancesShouldRetainDistinctValues() {
            Person p1 = (Person)Injector.Build<IEntity>();
            Person p2 = (Person)Injector.Build<IEntity>();

            p2.Age = 22;
            p2.Name = "Bill";

            Assert.AreNotSame(p1, p2);
            Assert.AreNotEqual(p1.Name, p2.Name);
            Assert.AreNotEqual(p1.Age, p2.Age);
        }

        [TestMethod]
        public void SingletonShouldReturnSameValues() {
            Person p = new Person {
                Age = 21,
                Name = "John"
            };

            Injector.Mappings.Add<IEntity>(() => {
                return p;
            });

            Person p1 = (Person)Injector.Build<IEntity>();
            Person p2 = (Person)Injector.Build<IEntity>();

            p2.Age = 22;
            p2.Name = "Bill";

            Assert.AreSame(p1, p2);
        }

        [TestMethod]
        public void BuildShouldConstructDependentObjects() {
            Injector.Mappings.Add<Person>(() => {
                return new Person {
                    Age = 21,
                    Name = "John"
                };
            });

            Car c = Injector.Build<Car>();
            Assert.IsNotNull(c.Owner);
            Assert.AreEqual("John", c.Owner.Name);
            Assert.AreEqual(21, c.Owner.Age);
        }

        [TestMethod]
        public void BuildShouldConstructUsingMostParametersMatched() {
            Car c = Injector.Build<Car>();
            Assert.IsNotNull(c.Owner);
            Assert.IsNotNull(c.Insurer);

            Assert.AreEqual("John", c.Owner.Name);
            Assert.AreEqual(21, c.Owner.Age);

            Assert.AreEqual("John", c.Insurer.Name);
            Assert.AreEqual(21, c.Insurer.Age);
        }

        [TestMethod]
        public void ClearShouldRemoveAllMappings() {
            Assert.AreEqual(1, Injector.Mappings.Count);

            Injector.Mappings.Clear();
            Assert.AreEqual(0, Injector.Mappings.Count);
        }

        [TestMethod]
        public void UpdateShouldRetainNewValues() {
            Assert.AreEqual("John", Injector.Build<IEntity>().Name);

            var stats = Injector.Mappings.Update<IEntity>(() => {
                return new Person {
                    Age = 21,
                    Name = "Bill"
                };
            });

            Assert.AreEqual("Bill", Injector.Build<IEntity>().Name);
        }

        [TestMethod]
        public void ShouldTellIfTypeIsMapped() {
            Assert.IsTrue(Injector.Mappings.IsMapped<IEntity>());
            Assert.IsFalse(Injector.Mappings.IsMapped<Car>());
        }

        [TestMethod]
        public void ShouldReturnMappedCount() {
            Assert.AreEqual(1, Injector.Mappings.Count);

            Injector.Mappings.Clear();
            Assert.AreEqual(0, Injector.Mappings.Count);
        }

        [TestMethod]
        public void ShouldReturnMappedTypes() {
            IList<Type> types = Injector.Mappings.Types();

            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(typeof(IEntity), types[0]);
        }

        [TestMethod]
        public void RemoveShouldRemoveMap() {
            Assert.AreEqual(true, Injector.Mappings.IsMapped<IEntity>());

            Injector.Mappings.Remove<IEntity>();
            Assert.AreEqual(false, Injector.Mappings.IsMapped<IEntity>());
            Assert.AreEqual(0, Injector.Mappings.Count);
        }

        [TestMethod]
        public void AddShouldAddMap() {
            Injector.Mappings.Clear();

            Assert.AreEqual(0, Injector.Mappings.Count);
            Assert.AreEqual(false, Injector.Mappings.IsMapped<IEntity>());

            Injector.Mappings.Add<IEntity>(() => {
                return new Person {
                    Age = 21, Name = "John"
                };
            });

            Assert.AreEqual(1, Injector.Mappings.Count);
            Assert.AreEqual(true, Injector.Mappings.IsMapped<IEntity>());
        }

        class Person : IEntity {
            public int Age { get; set; }
            public string Name { get; set; }
        }

        class Car {
            public IEntity Owner { get; private set; }
            public IEntity Insurer { get; private set; }

            public Car(IEntity owner, IEntity insurer) {
                Owner = owner;
                Insurer = insurer;
            }
        }

        interface IEntity {
            int Age { get; set; }
            string Name { get; set; }
        }
    }
}
