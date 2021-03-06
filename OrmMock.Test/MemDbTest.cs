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

namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using OrmMock.MemDb;
    using OrmMock.Shared;

    [TestFixture]
    public class MemDbTest
    {
        public class TestClass1
        {
            public Guid Id { get; set; }
        }

        public class TestClass1B
        {
            public Guid Id { get; set; }
        }

        public class TestClass2
        {
            public int Key1 { get; set; }

            public int Key2 { get; set; }
        }

        public class TestClass3
        {
            public TestClass3()
            {
                this.List = new List<TestClass3>();
                this.List2 = new List<TestClass4>();
            }

            public int Id { get; set; }

            public IList<TestClass3> List { get; }

            public IList<TestClass4> List2 { get; }

            public TestClass3 Ref { get; set; }

            public TestClass4 Ref2 { get; set; }
        }

        public class TestClass4
        {
            public TestClass4()
            {
                this.List = new List<TestClass3>();
                this.List2 = new List<TestClass4>();
            }

            public int Id { get; set; }

            public List<TestClass3> List { get; }

            public IList<TestClass4> List2 { get; }

            public TestClass3 Ref { get; set; }

            public TestClass4 Ref2 { get; set; }
        }

        public class TestClass5
        {
            public long Id { get; set; }

            public int Auto { get; set; }
        }


        public class TestClass6
        {
            public virtual TestClass7 Class7 { get; set; }

            public int Class7Id { get; set; }
        }

        public class TestClass7
        {
            public int Id { get; set; }

            public ICollection<TestClass6> Class6 { get; set; }
        }

        public class ManyToMany1
        {
            public int Id { get; set; }

            public ICollection<ManyToMany2> ManyToMany2 { get; set; }
        }

        public class ManyToMany2
        {
            public int Id { get; set; }

            public ICollection<ManyToMany1> ManyToMany1 { get; set; }
        }

        private MemDb db;

        [SetUp]
        public void Setup()
        {
            this.db = new MemDb();
            this.db.Relations.RegisterNullForeignKeys<TestClass4, TestClass3>();
            this.db.Relations.RegisterNullForeignKeys<TestClass3, TestClass4>();
            this.db.Relations.RegisterNullForeignKeys<TestClass3, TestClass3>();
            this.db.Relations.RegisterNullForeignKeys<TestClass4, TestClass4>();
        }

        [Test]
        public void TestAdd()
        {
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            db.Commit();
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [Test]
        public void TestAddTwiceSameKey()
        {
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            obj = new TestClass1 { Id = obj.Id };
            db.Add(obj);
            db.Commit();
        }

        [Test]
        public void TestCount()
        {
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });

            db.Commit();

            Assert.AreEqual(2, db.Count<TestClass1>());
            Assert.AreEqual(3, db.Count<TestClass1B>());
            Assert.AreEqual(5, db.Count());
        }

        [Test]
        public void TestAddUnknownKey()
        {
            db.Add(new TestClass2());
            db.Commit();
            Assert.AreEqual(1, db.Count());
        }

        [Test]
        public void TestRegisterKey()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1);
        }

        [Test]
        public void TestRegisterKeyFail()
        {
            Assert.Throws<InvalidOperationException>(() => db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1 + 1));
        }

        [Test]
        public void TestRegisterKeyGet()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1);
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new Keys(23));
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyComposite()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
        }

        [Test]
        public void TestRegisterKeyCompositeFail()
        {
            Assert.Throws<InvalidOperationException>(() => db.Relations.RegisterPrimaryKey<TestClass2>(i => new { foo = i.Key1 + 1 }));
        }

        [Test]
        public void TestRegisterKeyGetComposite()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new Keys(45, 23));
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyGetCompositeFail()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new Keys(23, 45));
            Assert.IsNull(fetched);
        }

        [Test]
        public void TestGet()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            var fetched = db.Get<TestClass1>(new Keys(stored.Id));
            Assert.AreSame(stored, fetched);
        }

        [Test]
        public void TestGetEnumerable()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            db.Get<TestClass1>().Single(s => s.Id == stored.Id);
        }

        [Test]
        public void TestGetEnumerableEmpty()
        {
            Assert.AreEqual(0, db.Get<TestClass1>().Count());
        }

        [Test]
        public void TestRemove()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            var remove = new TestClass1 { Id = stored.Id };
            Assert.IsTrue(db.Remove(remove));
            db.Commit();
            Assert.AreEqual(0, db.Count());
        }

        [Test]
        public void TestRemoveMissing()
        {
            Assert.IsFalse(db.Remove(new TestClass1 { Id = Guid.NewGuid() }));
        }

        [Test]
        public void TestAddChildren()
        {
            var stored = new TestClass4
            {
                Id = 1,
            };
            stored.List.Add(new TestClass3
            {
                Id = 2
            });
            stored.List2.Add(new TestClass4
            {
                Id = 3
            });

            db.Add(stored);
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new Keys(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new Keys(2)));
            Assert.IsNotNull(db.Get<TestClass4>(new Keys(3)));
        }

        [Test]
        public void TestAddChildrenTwice()
        {
            var stored = new TestClass4
            {
                Id = 1,
            };
            var added = new TestClass3 { Id = 2 };
            stored.List.Add(added);
            stored.List.Add(added);

            db.Add(stored);
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new Keys(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new Keys(2)));
            Assert.AreEqual(2, db.Count());
        }

        [Test]
        public void TestAddReference()
        {
            var stored = new TestClass4
            {
                Id = 1,
                Ref = new TestClass3
                {
                    Id = 2,
                    Ref2 = new TestClass4
                    {
                        Id = 3
                    }
                }
            };

            db.Add(stored);
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new Keys(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new Keys(2)));
            Assert.IsNotNull(db.Get<TestClass4>(new Keys(3)));
            Assert.AreEqual(3, db.Count());
        }

        [Test]
        public void TestAutoIncrement()
        {
            db.RegisterAutoIncrement<TestClass5>(i => i.Auto);
            db.Add(new TestClass5
            {
                Id = 123
            });
            db.Add(new TestClass5
            {
                Id = 1234
            });
            db.Commit();

            var s1 = db.Get<TestClass5>(new Keys((long)123));
            var s2 = db.Get<TestClass5>(new Keys((long)1234));

            Assert.AreEqual(1, s1.Auto);
            Assert.AreEqual(2, s2.Auto);
        }

        [Test]
        public void TestCreate()
        {
            var obj = db.Create<TestClass2>();

            Assert.IsNotNull(obj);
            Assert.AreEqual(typeof(TestClass2), obj.GetType());
        }

        [Test]
        public void TestAddMany()
        {
            db.RegisterAutoIncrement<TestClass5>(i => i.Id);
            db.AddMany(Enumerable.Range(0, 10).Select(_ => new TestClass5()));
            db.Commit();

            Assert.AreEqual(10, db.Count());
        }

        [Test]
        public void TestTraverseObjectGraph()
        {
            var objects = db.TraverseObjectGraph(new TestClass7
            {
                Class6 = new List<TestClass6>
                {
                    new TestClass6(),
                    new TestClass6(),
                    new TestClass6(),
                    new TestClass6()
                }
            }).ToList();

            Assert.AreEqual(5, objects.Count);
        }

        [Test]
        public void TestRemoveByKey()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            Assert.IsTrue(db.Remove<TestClass1>(new Keys(stored.Id)));
            db.Commit();
            Assert.AreEqual(0, db.Count());
        }

        [Test]
        public void TestDisconnected()
        {
            db.AddMany(new object[]
            {
                new TestClass6
                {
                    Class7Id = 123
                },
                new TestClass7
                {
                    Id = 123
                }
            });

            db.Commit();
            var obj = db.Get<TestClass6>().Single();

            Assert.AreSame(obj, obj.Class7.Class6.Single());
        }

        [Test]
        public void TestManyToMany()
        {
            var mm1 = new ManyToMany1 { ManyToMany2 = new List<ManyToMany2> { new ManyToMany2(), new ManyToMany2(), new ManyToMany2() } };

            db.Add(mm1);
            db.Commit();
            var mm = db.Get<ManyToMany1>().Single();
            VerifyMM(mm);
        }

        [Test]
        public void TestManyToManyB()
        {
            var mm1 = new ManyToMany1();
            var mm2 = Enumerable.Range(0, 3).Select(_ => new ManyToMany2 { ManyToMany1 = new List<ManyToMany1> { mm1 } }).ToList();

            db.AddMany(mm2);
            db.Commit();
            var mm = db.Get<ManyToMany1>().Single();
            VerifyMM(mm);
        }

        [Test]
        public void TestManyToManyC()
        {
            var mm1 = new ManyToMany1();
            var mm2 = Enumerable.Range(0, 3).Select(_ => new ManyToMany2 { ManyToMany1 = new List<ManyToMany1> { mm1 } }).ToList();
            mm1.ManyToMany2 = new List<ManyToMany2>(mm2);
            db.AddMany(mm2);
            db.Commit();
            var mm = db.Get<ManyToMany1>().Single();
            VerifyMM(mm);
        }

        public void VerifyMM(ManyToMany1 root)
        {
            Assert.AreEqual(3, root.ManyToMany2.Count);
            Assert.IsTrue(root.ManyToMany2.All(mm => object.ReferenceEquals(mm.ManyToMany1.Single(), root)));
        }
    }
}
