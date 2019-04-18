using JsonQuery.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonQuery.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void Simple_Query_Over_List_Of_Objects()
        {
            // Arrange
            IJsonQueryProvider jsonQuery = new JsonQueryProvider();
            var data = _BuildSampleData();
            var request = new JsonQueryRequest()
            {
                Select = new List<JsonQuerySelect>()
                {
                    new JsonQuerySelect() { Selector = nameof(Foo.IntProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.StringProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DecimalProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DateTimeProperty) }
                },
                From = new JsonQueryFrom()
                {
                    Name = nameof(Foo)
                }
            };

            // Act
            jsonQuery.AddQueryable(data.AsQueryable());
            var result = jsonQuery.ExecuteQuery(request);

            // Assert
            Assert.AreEqual(
                expected: data.Count,
                actual: result.Rows.Count);
            for (int i = 0; i < request.Select.Count; i++)
            {
                Assert.AreEqual(
                    expected: request.Select[i].Selector,
                    actual: result.Columns[i].Name);
            }
            for (int i = 0; i < result.Rows.Count; i++)
            {
                Assert.AreEqual(
                    expected: request.Select.Count,
                    actual: result.Rows[i].Data.Length);

                Assert.AreEqual(
                    expected: data[i].IntProperty,
                    actual: result.Rows[i].Data[0]);
                Assert.AreEqual(
                    expected: data[i].StringProperty,
                    actual: result.Rows[i].Data[1]);
                Assert.AreEqual(
                    expected: data[i].DecimalProperty,
                    actual: result.Rows[i].Data[2]);
                Assert.AreEqual(
                    expected: data[i].DateTimeProperty,
                    actual: result.Rows[i].Data[3]);
            }
        }

        [TestMethod]
        public void Simple_Filtering()
        {
            // Arrange
            IJsonQueryProvider jsonQuery = new JsonQueryProvider();
            var data = _BuildSampleData();
            var request = new JsonQueryRequest()
            {
                Select = new List<JsonQuerySelect>()
                {
                    new JsonQuerySelect() { Selector = nameof(Foo.IntProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.StringProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DecimalProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DateTimeProperty) }
                },
                From = new JsonQueryFrom()
                {
                    Name = nameof(Foo)
                },
                Where = new List<JsonQueryWhere>()
                {
                    new JsonQueryWhere()
                    {
                        Selector = nameof(Foo.IntProperty),
                        Operator = "<=",
                        Value = "'20'"
                    }
                }
            };

            // Act
            jsonQuery.AddQueryable(data.AsQueryable());
            var result = jsonQuery.ExecuteQuery(request);

            // Assert
            Assert.AreEqual(
                expected: 20,
                actual: result.Rows.Count);
            for (int i = 0; i < result.Rows.Count; i++)
            {
                Assert.IsTrue((int)result.Rows[i].Data[0] <= 20);
            }
        }

        [TestMethod]
        public void Simple_Where_In_Collection()
        {
            // Arrange
            IJsonQueryProvider jsonQuery = new JsonQueryProvider();
            var data = _BuildSampleData();
            var request = new JsonQueryRequest()
            {
                Select = new List<JsonQuerySelect>()
                {
                    new JsonQuerySelect() { Selector = nameof(Foo.IntProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.StringProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DecimalProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DateTimeProperty) }
                },
                From = new JsonQueryFrom()
                {
                    Name = nameof(Foo)
                },
                Where = new List<JsonQueryWhere>()
                {
                    new JsonQueryWhere()
                    {
                        Selector = nameof(Foo.IntProperty),
                        Operator = "IN",
                        Values = new string[] { "10", "20" }
                    }
                }
            };

            // Act
            jsonQuery.AddQueryable(data.AsQueryable());
            var result = jsonQuery.ExecuteQuery(request);

            // Assert
            Assert.AreEqual(
                expected: 2,
                actual: result.Rows.Count);
            Assert.AreEqual(
                expected: 10,
                actual: result.Rows[0].Data[0]);
            Assert.AreEqual(
                expected: 20,
                actual: result.Rows[1].Data[0]);
        }

        [TestMethod]
        public void Simple_Ordering()
        {
            // Arrange
            IJsonQueryProvider jsonQuery = new JsonQueryProvider();
            var data = _BuildSampleData();
            var request = new JsonQueryRequest()
            {
                Select = new List<JsonQuerySelect>()
                {
                    new JsonQuerySelect() { Selector = nameof(Foo.IntProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.StringProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DecimalProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DateTimeProperty) }
                },
                From = new JsonQueryFrom()
                {
                    Name = nameof(Foo)
                },
                OrderBy = new List<JsonQueryOrderBy>()
                {
                    new JsonQueryOrderBy()
                    {
                        Selector = nameof(Foo.IntProperty),
                        Direction = JsonQueryOrderDirection.Desc
                    },
                    new JsonQueryOrderBy()
                    {
                        Selector = nameof(Foo.DecimalProperty),
                        Direction = JsonQueryOrderDirection.Asc
                    }
                }
            };

            // Act
            jsonQuery.AddQueryable(data.AsQueryable());
            var result = jsonQuery.ExecuteQuery(request);

            // Assert
            Assert.AreEqual(
                expected: data.Count,
                actual: result.Rows.Count);
            var max = int.MaxValue;
            for (int i = 0; i < result.Rows.Count; i++)
            {
                Assert.IsTrue((int)result.Rows[i].Data[0] <= max);
                max = (int)result.Rows[i].Data[0];
            }
        }

        [TestMethod]
        public void Simple_Limits()
        {
            // Arrange
            int skip = 10, take = 20;
            IJsonQueryProvider jsonQuery = new JsonQueryProvider();
            var data = _BuildSampleData();
            var request = new JsonQueryRequest()
            {
                Select = new List<JsonQuerySelect>()
                {
                    new JsonQuerySelect() { Selector = nameof(Foo.IntProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.StringProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DecimalProperty) },
                    new JsonQuerySelect() { Selector = nameof(Foo.DateTimeProperty) }
                },
                From = new JsonQueryFrom()
                {
                    Name = nameof(Foo)
                },
                Limit = new JsonQueryLimit()
                {
                    Skip = skip,
                    Take = take
                }
            };

            // Act
            jsonQuery.AddQueryable(data.AsQueryable());
            var result = jsonQuery.ExecuteQuery(request);

            // Assert
            Assert.AreEqual(
                expected: take,
                actual: result.Rows.Count);
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var id = (int)result.Rows[i].Data[0];
                Assert.IsTrue(id > skip);
                Assert.IsTrue(id <= skip + take);
            }
        }

        #region Private

        class Foo
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public decimal DecimalProperty { get; set; }
            public DateTime DateTimeProperty { get; set; }
        }

        List<Foo> _BuildSampleData(int count = 100)
        {
            var result = new List<Foo>(capacity: count);
            var rnd = new Random();
            var date = DateTime.Now;

            for (int i = 1; i <= count; i++)
            {
                result.Add(new Foo()
                {
                    IntProperty = i,
                    StringProperty = $"Object {i}",
                    DecimalProperty = (decimal)(rnd.NextDouble() * 100.0),
                    DateTimeProperty = (date = date.AddMinutes(1))
                });
            }

            return result;
        }

        #endregion Private
    }
}
