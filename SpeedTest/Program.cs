using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XpressiveCode.Injection;
using System.Diagnostics;

namespace SpeedTest {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Injector Speed Test");
            Console.WriteLine("Test the creation of 100,000 cars that includes 2 dependency injections. This creates a total of 300,000 objects");
            Console.WriteLine("Running Test...");

            Injector.Mappings.Add<IEntity>(() => {
                return new Person {
                    Age = 21,
                    Name = "John"
                };
            });

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100000; i++) {
                Injector.Build<Car>();
            }

            sw.Stop();

            Console.WriteLine("Created 100,000 Cars in " + sw.ElapsedMilliseconds.ToString() + "ms");

            Console.WriteLine("");
            Console.WriteLine("Creating 100,000 IEntity objects, which is a 1 to 1 ratio, no dependency required, but it will still use the map to return an actual object");

            sw.Restart();
            for (int i = 0; i < 100000; i++) {
                Injector.Build<IEntity>();
            }
            sw.Stop();

            Console.WriteLine("Created 100,000 IEntity objects in " + sw.ElapsedMilliseconds.ToString() + "ms");
            
            Console.Read();
        }
    }

    class Person : IEntity {
        public int Age { get; set; }
        public string Name { get; set; }
    }

    class Car {
        public IEntity Owner { get; private set; }
        public IEntity Insurer { get; private set; }

        public Car(Person owner, IEntity insurer) {
            Owner = owner;
            Insurer = insurer;
        }
    }

    interface IEntity {
        int Age { get; set; }
        string Name { get; set; }
    }
}
