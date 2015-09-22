﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ronin.Common;
using Stashbox.Attributes;
using Stashbox.ContainerExtensions.MethodInjection;
using Stashbox.ContainerExtensions.PropertyInjection;
using System;
using System.Threading.Tasks;

namespace Stashbox.Tests
{
    [TestClass]
    public class AttributeTest
    {
        [TestMethod]
        public void AttributeTests_Resolve()
        {
            var container = new StashboxContainer();
            container.RegisterExtension(new PropertyInjectionExtension());
            container.RegisterExtension(new MethodInjectionExtension());
            container.RegisterType<ITest1, Test1>("test1");
            container.RegisterType<ITest1, Test11>("test11");
            container.RegisterType<ITest1, Test12>("test12");
            container.RegisterType<ITest2, Test2>("test2");
            container.RegisterType<ITest2, Test22>("test22");
            container.RegisterType<ITest3, Test3>();
            container.RegisterType<ITest4, Test4>();
            container.RegisterType<Test33>();

            var test1 = container.Resolve<ITest1>();
            var test2 = container.Resolve<ITest2>("test2");
            var test3 = container.Resolve<ITest3>();
            var test4 = container.Resolve<Lazy<ITest4>>();
            var test33 = container.Resolve<Test33>();

            Assert.IsNotNull(test1);
            Assert.IsNotNull(test2);
            Assert.IsNotNull(test3);
            Assert.IsNotNull(test4.Value);

            Assert.IsTrue(test3.MethodInvoked);

            Assert.IsInstanceOfType(test2.test1, typeof(Test1));
            Assert.IsInstanceOfType(test3.test1, typeof(Test11));
            Assert.IsInstanceOfType(test3.test2, typeof(Test22));
        }

        [TestMethod]
        public void AttributeTests_Parallel_Resolve()
        {
            var container = new StashboxContainer();
            container.RegisterExtension(new PropertyInjectionExtension());
            container.RegisterExtension(new MethodInjectionExtension());
            container.RegisterType<ITest1, Test1>("test1");
            container.RegisterType<ITest1, Test11>("test11");
            container.RegisterType<ITest1, Test12>("test12");
            container.RegisterType<ITest2, Test2>("test2");
            container.RegisterType<ITest2, Test22>("test22");
            container.RegisterType<ITest3, Test3>();
            container.RegisterType<Test33>();

            Parallel.For(0, 50000, (i) =>
            {
                var test1 = container.Resolve<ITest1>();
                var test2 = container.Resolve<ITest2>("test2");
                var test3 = container.Resolve<ITest3>();
                var test33 = container.Resolve<Test33>();

                Assert.IsNotNull(test1);
                Assert.IsNotNull(test2);
                Assert.IsNotNull(test3);

                Assert.IsTrue(test3.MethodInvoked);

                Assert.IsInstanceOfType(test2.test1, typeof(Test1));
                Assert.IsInstanceOfType(test3.test1, typeof(Test11));
                Assert.IsInstanceOfType(test3.test2, typeof(Test22));
            });
        }

        [TestMethod]
        public void AttributeTests_Parallel_Lazy_Resolve()
        {
            var container = new StashboxContainer();
            container.RegisterExtension(new PropertyInjectionExtension());
            container.RegisterExtension(new MethodInjectionExtension());
            container.RegisterType<ITest1, Test1>("test1");
            container.RegisterType<ITest1, Test11>("test11");
            container.RegisterType<ITest1, Test12>("test12");
            container.RegisterType<ITest2, Test2>("test2");
            container.RegisterType<ITest2, Test22>("test22");
            container.RegisterType<ITest3, Test3>();
            container.RegisterType<ITest4, Test4>();
            container.RegisterType<Test33>();

            Parallel.For(0, 50000, (i) =>
            {
                var test1 = container.Resolve<Lazy<ITest1>>();
                var test2 = container.Resolve<Lazy<ITest2>>("test2");
                var test3 = container.Resolve<Lazy<ITest3>>();
                var test4 = container.Resolve<Lazy<ITest4>>();
                var test33 = container.Resolve<Lazy<Test33>>();

                Assert.IsNotNull(test1.Value);
                Assert.IsNotNull(test2.Value);
                Assert.IsNotNull(test3.Value);
                Assert.IsNotNull(test4.Value);

                Assert.IsTrue(test3.Value.MethodInvoked);
                Assert.IsTrue(test4.Value.MethodInvoked);

                Assert.IsInstanceOfType(test2.Value.test1, typeof(Test1));
                Assert.IsInstanceOfType(test3.Value.test1, typeof(Test11));
                Assert.IsInstanceOfType(test3.Value.test2, typeof(Test22));

                Assert.IsInstanceOfType(test4.Value.test1.Value, typeof(Test11));
                Assert.IsInstanceOfType(test4.Value.test2.Value, typeof(Test22));
            });
        }

        public interface ITest1 { }

        public interface ITest2 { ITest1 test1 { get; set; } }

        public interface ITest3 { ITest2 test2 { get; set; } ITest1 test1 { get; set; } bool MethodInvoked { get; set; } }

        public interface ITest4 { Lazy<ITest2> test2 { get; set; } Lazy<ITest1> test1 { get; set; } bool MethodInvoked { get; set; } }

        public class Test1 : ITest1
        { }

        public class Test11 : ITest1
        { }

        public class Test12 : ITest1
        { }

        public class Test22 : ITest2 { public ITest1 test1 { get; set; } }

        public class Test2 : ITest2
        {
            [InjectionProperty]
            public ITest1 test1 { get; set; }

            public Test2([Dependency("test11")]ITest1 test1)
            {
                Shield.EnsureNotNull(test1);
                Shield.EnsureTypeOf<Test11>(test1);
            }
        }

        public class Test3 : ITest3
        {
            [InjectionProperty("test11")]
            public ITest1 test1 { get; set; }

            [InjectionProperty("test22")]
            public ITest2 test2 { get; set; }

            public Test3()
            { }

            public Test3(ITest1 test1)
            { }

            [InjectionMethod]
            public void MethodTest([Dependency("test22")]ITest2 test2)
            {
                Shield.EnsureNotNull(test2);
                MethodInvoked = true;
            }

            [InjectionConstructor]
            public Test3([Dependency("test12")]ITest1 test1, [Dependency("test2")]ITest2 test2)
            {
                Shield.EnsureNotNull(test1);
                Shield.EnsureNotNull(test2);

                Shield.EnsureTypeOf<Test12>(test1);
                Shield.EnsureTypeOf<Test2>(test2);
            }

            public bool MethodInvoked { get; set; }
        }

        public class Test4 : ITest4
        {
            [InjectionProperty("test11")]
            public Lazy<ITest1> test1 { get; set; }

            [InjectionProperty("test22")]
            public Lazy<ITest2> test2 { get; set; }

            public Test4()
            { }

            public Test4(Lazy<ITest1> test1)
            { }

            [InjectionMethod]
            public void MethodTest([Dependency("test22")]Lazy<ITest2> test2)
            {
                Shield.EnsureNotNull(test2.Value);
                MethodInvoked = true;
            }

            [InjectionConstructor]
            public Test4([Dependency("test12")]Lazy<ITest1> test1, [Dependency("test2")]Lazy<ITest2> test2)
            {
                Shield.EnsureNotNull(test1.Value);
                Shield.EnsureNotNull(test2.Value);

                Shield.EnsureTypeOf<Test12>(test1.Value);
                Shield.EnsureTypeOf<Test2>(test2.Value);
            }

            public bool MethodInvoked { get; set; }
        }

        public class Test33
        {
            public Test33()
            {
                throw new Exception();
            }

            [InjectionConstructor]
            public Test33(ITest1 test1)
            {
                Shield.EnsureNotNull(test1);
            }
        }
    }
}
