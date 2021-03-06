﻿// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using OrmMock.DataGenerator;

namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using OrmMock;

    [TestFixture]
    public class ForContextTest
    {
        private DataGenerator dataGenerator;

        [SetUp]
        public void Setup()
        {
            this.dataGenerator = new DataGenerator();
        }

        public class ClassWithConstructor
        {
            private int id;

            public int Id => this.id;

            public ClassWithConstructor(int id)
            {
                this.id = id;
            }
        }

        public class TestClass1
        {
            public int Id { get; set; }

            public virtual TestClass2 Class2 { get; set; }

            public int Class2Id { get; set; }

            public string Name { get; set; }
        }

        public class TestClass2
        {
            public int Id { get; set; }

            public virtual ICollection<TestClass1> Class1 { get; set; }

            public string Name { get; set; }
        }

        public class TestClass3
        {
            public TestClass1 TestClass1 { get; }

            public Guid Id { get; }

            public TestClass3(TestClass1 tc1, Guid id)
            {
                this.TestClass1 = tc1;
                this.Id = id;
            }
        }

        public class TestClass4
        {
            public int Id { get; set; }

            public TestClass5 Class5 { get; set; }

            public int? Class5Id { get; set; }
        }


        public class TestClass5
        {
            public int Id { get; set; }

            public virtual ICollection<TestClass4> Class4 { get; set; }
        }

        public class TestClass6
        {
            public Guid Id { get; set; }

            public Guid Class7Id { get; set; }

            public Guid Class8Id { get; set; }

            public TestClass7 Class7 { get; set; }

            public TestClass8 Class8 { get; set; }
        }

        public class TestClass7
        {
            public Guid Id { get; set; }

            public Guid Class8Id { get; set; }

            public TestClass8 Class8 { get; set; }

            public ICollection<TestClass6> Class6 { get; set; }
        }

        public class TestClass8
        {
            public Guid Id { get; set; }
            public ICollection<TestClass7> Class7 { get; set; }

            public ICollection<TestClass6> Class6 { get; set; }
        }

        public class TestClass9
        {
            public Guid Id { get; set; }

            public TestClass10 Class10 { get; set; }
        }

        public class TestClass10
        {
            public Guid Id { get; set; }

            public TestClass9 Class9 { get; set; }
        }

        public class TestNullable1
        {

            public Guid Id { get; set; }

            public Guid? Nullable2Id { get; set; }

            public TestNullable2 Nullable2 { get; set; }
        }

        public class TestNullable2
        {
            public Guid Id { get; set; }

            public ICollection<TestNullable1> Nullable1 { get; set; }
        }

        public class TestCircularClass
        {
            public int Id { get; set; }

            public TestCircularClass2 Circular { get; set; }

            public int CircularId { get; set; }
        }

        public class TestCircularClass2
        {
            public int Id { get; set; }

            public ICollection<TestCircularClass> Circulars { get; set; }

            public int CircularId { get; set; }


            public TestCircularClass3 Circular { get; set; }
        }

        public class TestCircularClass3
        {
            public int Id { get; set; }

            public ICollection<TestCircularClass2> Circulars { get; set; }

            public int CircularId { get; set; }


            public TestCircularClass Circular { get; set; }
        }

        public class SimpleClass
        {
            public string Prop1 { get; set; }

            public string Prop2 { get; set; }
        }

        public class SimpleClass2
        {
            public string Foo { get; set; }

            public SimpleClass SimpleClass { get; set; }
        }

        [Test]
        public void TestReferences()
        {
            var result = this.dataGenerator.Create<TestClass2>();
            Assert.AreEqual(3, result.Class1.Count);
            Assert.IsTrue(result.Class1.All(t => t.Class2Id == t.Class2.Id));
            Assert.IsTrue(result.Class1.All(t => object.ReferenceEquals(t.Class2, result)));
        }

        [Test]
        public void TestReferences2()
        {
            var result = this.dataGenerator.Create<TestClass1>();
            Assert.AreEqual(result.Class2.Class1.Single().Id, result.Id);
        }

        [Test]
        public void TestReferences3()
        {
            var result = this.dataGenerator.CreateMany<TestClass4>(10).ToList();
        }

        [Test]
        public void TestReferences4()
        {
            var result = this.dataGenerator.Create<TestClass6>();
            var tc6 = this.dataGenerator.GetObjects<TestClass6>().Single();
            var tc7 = this.dataGenerator.GetObjects<TestClass7>().Single();
            // var tc8 = this.ctx.GetObjects<TestClass8>().Single();
        }

        [Test]
        public void TestNullable()
        {
            this.dataGenerator.ObjectLimit = 9999;
            var result = this.dataGenerator.CreateMany<TestNullable1>(1000).ToList();

            var nonNullCount = result.Count(t => t.Nullable2 != null);
            var nullCount = result.Count(t => t.Nullable2 == null);

            Assert.NotZero(nonNullCount);
            Assert.NotZero(nullCount);
        }

        [Test]
        public void TestNullableInclude()
        {
            this.dataGenerator.For<TestNullable1>().With(e => e.Nullable2);
            var result = this.dataGenerator.Create<TestNullable1>();

            Assert.IsNotNull(result.Nullable2);
            Assert.AreEqual(result.Nullable2Id.Value, result.Nullable2.Id);
        }

        [Test]
        public void TestNullableExclude()
        {
            this.dataGenerator.For<TestNullable1>().Without(e => e.Nullable2);
            var result = this.dataGenerator.Create<TestNullable1>();

            Assert.IsNull(result.Nullable2);
            Assert.IsNull(result.Nullable2Id);
        }

        [Test]
        public void TestSingleton()
        {
            this.dataGenerator.For<TestClass2>()
                .RegisterSingleton();

            var result = this.dataGenerator.CreateMany<TestClass1>(10).ToList();

            var singleton = this.dataGenerator.GetSingleton<TestClass2>();

            Assert.IsTrue(result.All(t => object.ReferenceEquals(t.Class2, singleton)));
        }

        [Test]
        public void TestSingleton2()
        {
            var singleton = new TestClass2
            {
                Id = 123
            };
            this.dataGenerator.For<TestClass2>().With(singleton);

            var result = this.dataGenerator.CreateMany<TestClass1>(10).ToList();

            Assert.IsTrue(result.All(t => object.ReferenceEquals(t.Class2, singleton)));
        }

        [Test]
        public void TestSingletonReferenceMismatch()
        {
            this.dataGenerator.For<TestClass2>()
                .Include(tc => tc.Class1, 1);

            var obj1 = this.dataGenerator.Create<TestClass2>();
            this.dataGenerator.Singleton(this.dataGenerator.GetObject<TestClass1>());
            Assert.Throws<InvalidOperationException>(() => this.dataGenerator.Create<TestClass2>());
        }

        [Test]
        public void TestCircular()
        {
            this.dataGenerator.DefaultLookback = 10;
            var objs = this.dataGenerator.Create<TestCircularClass>();
            Assert.AreEqual(3, this.dataGenerator.GetObjects().Count());
        }

        [Test]
        public void TestLimit()
        {
            this.dataGenerator.CreateMany<SimpleClass>(this.dataGenerator.ObjectLimit).ToList();
            Assert.Throws<InvalidOperationException>(() => this.dataGenerator.Create<SimpleClass>());
        }

        [Test]
        public void TestCreateMany()
        {
            var objs = this.dataGenerator.For<SimpleClass>().CreateMany().ToList();
            Assert.AreEqual(3, objs.Count);
        }

        [Test]
        public void TestCustomSetter()
        {
            this.dataGenerator.For<string>()
                .With(() => "str");

            var obj = this.dataGenerator.Create<SimpleClass>();
            Assert.AreEqual("str", obj.Prop1);
            Assert.AreEqual("str", obj.Prop2);
        }

        [Test]
        public void TestCustomProperty()
        {
            this.dataGenerator.For<string>()
                .With(() => "str");

            this.dataGenerator.For<SimpleClass>()
                .With(sc => sc.Prop1, () => "str1")
                .With(sc => sc.Prop2, "str2");

            var obj = this.dataGenerator.Create<SimpleClass>();
            Assert.AreEqual("str1", obj.Prop1);
            Assert.AreEqual("str2", obj.Prop2);
        }

        [Test]
        public void TestCustomProperty2()
        {
            var obj = this.dataGenerator.Build<SimpleClass2>()
                .With(sc => sc.SimpleClass.Prop1, "str1")
                .With(sc => sc.SimpleClass.Prop2, "xxx")
                .Create();

            Assert.AreEqual("str1", obj.SimpleClass.Prop1);
            Assert.AreEqual("xxx", obj.SimpleClass.Prop2);
        }

        [Test]
        public void TestCustomProperty3()
        {
            var obj = this.dataGenerator.Build<SimpleClass2>()
                .With(sc => sc.SimpleClass.Prop1, sc => sc.SimpleClass.Prop2)
                .Create();

            Assert.AreEqual(obj.SimpleClass.Prop2, obj.SimpleClass.Prop1);
        }

        [Test]
        public void TestWithoutType()
        {
            this.dataGenerator.For<string>()
                .Without();

            var obj = this.dataGenerator.Create<SimpleClass>();
            Assert.IsNull(obj.Prop1);
            Assert.IsNull(obj.Prop2);
        }

        [Test]
        public void TestWithoutProperty()
        {
            this.dataGenerator.For<SimpleClass>()
                .Without(sc => sc.Prop2);

            var obj = this.dataGenerator.Create<SimpleClass>();
            Assert.IsNotEmpty(obj.Prop1);
            Assert.IsNull(obj.Prop2);
        }

        [Test]
        public void TestConstructorParametersFail()
        {
            Assert.Throws<InvalidOperationException>(() => this.dataGenerator.Create<TestClass3>());
        }

        [Test]
        public void TestInclude()
        {
            this.dataGenerator.For<TestClass2>()
                .Include(tc => tc.Class1, 2);

            var obj = this.dataGenerator.Create<TestClass2>();
            Assert.AreEqual(2, obj.Class1.Count);
        }

        [Test]
        public void Test11Relation()
        {
            this.dataGenerator.Relations.Register11Relation<TestClass9, TestClass10>(tc9 => tc9.Id, tc10 => tc10.Id);

            var obj = this.dataGenerator.Create<TestClass9>();
            Assert.AreEqual(2, this.dataGenerator.GetObjects().Count());
            Assert.AreNotEqual(Guid.Empty, obj.Id);
            Assert.AreNotEqual(Guid.Empty, obj.Class10.Id);
            Assert.AreEqual(obj.Id, obj.Class10.Id);
        }

        [Test]
        public void TestBuild()
        {
            this.dataGenerator.For<SimpleClass>()
                .With(x => x.Prop2, "bar");

            var item = this.dataGenerator.Build<SimpleClass>()
                .With(x => x.Prop1, "foo")
                .Create();

            Assert.AreEqual("foo", item.Prop1);
            Assert.AreEqual("bar", item.Prop2);

            var item2 = this.dataGenerator
                .Create<SimpleClass>();

            Assert.AreNotEqual("foo", item2.Prop1);
            Assert.AreEqual("bar", item.Prop2);
        }

        [Test]
        public void TestWithoutKeys()
        {
            this.dataGenerator.WithoutRelations();

            var a = this.dataGenerator.Create<SimpleClass2>();
        }

        [Test]
        public void TestPostCreate()
        {
            this.dataGenerator.For<SimpleClass>().AddPostCreate(s => s.Prop2 = "hello");

            var sc = this.dataGenerator.Create<SimpleClass>();

            Assert.AreEqual("hello", sc.Prop2);
        }

        [Test]
        public void TestPostCreateProperty()
        {
            var count = 0;
            this.dataGenerator.For<SimpleClass>().PostCreate(s => new { s.Prop1, s.Prop2 }, _ => ++count);

            var sc = this.dataGenerator.Create<SimpleClass>();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TestWithClassProperty()
        {
            this.dataGenerator.For<SimpleClass2>().With(s => s.SimpleClass, () => new SimpleClass
            {
                Prop1 = "foo",
                Prop2 = "bar"
            });

            var sc = this.dataGenerator.Create<SimpleClass2>();

            Assert.AreEqual("foo", sc.SimpleClass.Prop1);
            Assert.AreEqual("bar", sc.SimpleClass.Prop2);
        }

        [Test]
        public void TestWithClassType()
        {
            this.dataGenerator.For<SimpleClass>().With(() => new SimpleClass
            {
                Prop1 = "foo",
                Prop2 = "bar"
            });

            var sc = this.dataGenerator.Create<SimpleClass2>();

            Assert.AreEqual("foo", sc.SimpleClass.Prop1);
            Assert.AreEqual("bar", sc.SimpleClass.Prop2);
        }

        [Test]
        public void TestMultipleFor()
        {
            this.dataGenerator.For<TestClass1>()
                .With(t => t.Name, "apa")
                .For<TestClass2>()
                .With(t => t.Name, "bepa");

            var obj = this.dataGenerator.Create<TestClass1>();
            Assert.AreEqual("apa", obj.Name);
            Assert.AreEqual("bepa", obj.Class2.Name);
        }
    }
}
