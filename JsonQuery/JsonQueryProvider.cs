using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace JsonQuery.Contracts
{
    public class JsonQueryProvider : IJsonQueryProvider
    {
        public JsonQueryProvider()
        {
            _queryables = new Dictionary<string, QueryableInfo>();
        }

        public void AddQueryable(IQueryable queryable)
        {
            AddQueryable(
                queryable: queryable,
                name: queryable.ElementType.Name);
        }

        public void AddQueryable(IQueryable queryable, string name)
        {
            _AddQueryableInfo(queryable, name);
        }

        public JsonQueryResult ExecuteQuery(JsonQueryRequest request)
        {
            // From
            var queryable = _GetQueryableByName(request.From.Name);

            // Where
            if (request.Where != null && request.Where.Any())
            {
                queryable = _BuildWhereQueryable(queryable, request.Where);
            }

            // OrderBy
            if (request.OrderBy != null && request.OrderBy.Any())
            {
                queryable = _BuildOrderByQueryable(queryable, request.OrderBy);
            }

            // Limit
            if (request.Limit != null)
            {
                queryable = _BuildLimitQueryable(queryable, request.Limit);
            }

            // Select
            var resultsQueryable = _BuildSelectQueryable(queryable, request.Select) as IQueryable<object[]>;
            var results = resultsQueryable.ToList();

            // Return
            var result = _BuildResult(request, results);
            return result;
        }

        #region Private

        private readonly Dictionary<string, QueryableInfo> _queryables;

        struct QueryableInfo
        {
            public IQueryable Queryable;
            public JsonQuerySchema JsonQuerySchema;
        }

        #region General

        private void _AddQueryableInfo(IQueryable queryable, string name)
        {
            JsonQuerySchema schema = null; // TODO: _GetQueryableSchema(queryable, name);
            _queryables.Add(name, new QueryableInfo()
            {
                Queryable = queryable,
                JsonQuerySchema = schema
            });
        }

        private JsonQuerySchema _GetQueryableSchema(IQueryable queryable, string name)
        {
            // TODO
            throw new NotImplementedException();
        }

        private IQueryable _GetQueryableByName(string name)
        {
            if (_queryables.ContainsKey(name))
            {
                return _queryables[name].Queryable;
            }
            else
            {
                throw new JsonQueryException($"Queryable '{name}' not found");
            }
        }

        private MemberExpression _GetMemberExpression(Expression typeParameter, string propertyName)
        {
            if (propertyName.Contains("."))
            {
                int index = propertyName.IndexOf(".");
                var subParameter = Expression.Property(typeParameter, propertyName.Substring(0, index));
                return _GetMemberExpression(subParameter, propertyName.Substring(index + 1));
            }
            else
            {
                return Expression.Property(typeParameter, propertyName);
            }
        }

        private JsonQueryResult _BuildResult(JsonQueryRequest request, List<object[]> data)
        {
            var result = new JsonQueryResult()
            {
                Columns = request.Select
                    .Select(t => new JsonQueryResultColumn()
                    {
                        Name = t.Name ?? t.Selector
                    })
                    .ToList(),
                Rows = data
                    .Select(t => new JsonQueryResultRow()
                    {
                        Data = t
                    })
                    .ToList()
            };
            return result;
        }

        #endregion General

        #region Select

        private Expression _BuildSelectExpression(Type elementType, List<JsonQuerySelect> jsonQuerySelects)
        {
            var typeParameter = Expression.Parameter(elementType);
            var initializers = new Expression[jsonQuerySelects.Count];
            for (int i = 0; i < jsonQuerySelects.Count; i++)
            {
                initializers[i] = Expression.Convert(_GetMemberExpression(typeParameter, jsonQuerySelects[i].Selector), typeof(object));
            }
            return Expression.Lambda(
                body: Expression.NewArrayInit(
                    type: typeof(object),
                    initializers: initializers),
                parameters: typeParameter);
        }

        private IQueryable _BuildSelectQueryable(IQueryable baseQueryable, Expression selectExpression)
        {
            var method = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select)
                .GetMethodInfo()
                .GetGenericMethodDefinition()
                .MakeGenericMethod(baseQueryable.ElementType, typeof(object[]));

            return baseQueryable.Provider.CreateQuery(
                Expression.Call(
                    instance: null,
                    method: method,
                    arg0: baseQueryable.Expression,
                    arg1: Expression.Quote(selectExpression)));
        }

        private IQueryable _BuildSelectQueryable(IQueryable baseQueryable, List<JsonQuerySelect> jsonQuerySelects)
        {
            var selectExpression = _BuildSelectExpression(baseQueryable.ElementType, jsonQuerySelects);
            return _BuildSelectQueryable(baseQueryable, selectExpression);
        }

        #endregion Select

        #region Where

        private Expression _BuildWhereExpression(Type elementType, List<JsonQueryWhere> jsonQueryWheres)
        {
            var typeParameter = Expression.Parameter(elementType);
            Expression expression = null;
            for (int i = 0; i < jsonQueryWheres.Count; i++)
            {
                var ex = _GetWhereExpression(typeParameter, jsonQueryWheres[i]);
                expression = (expression == null)
                    ? ex
                    : Expression.And(expression, ex);
            }

            return Expression.Lambda(
                body: expression,
                typeParameter);
        }

        private Expression _GetWhereExpression(ParameterExpression typeParameter, JsonQueryWhere jsonQueryWhere)
        {
            Expression left = _GetMemberExpression(typeParameter, jsonQueryWhere.Selector);
            Expression right = null;

            if (!string.IsNullOrEmpty(jsonQueryWhere.Value))
            {
                if (jsonQueryWhere.Value.StartsWith("'") && jsonQueryWhere.Value.EndsWith("'"))
                {
                    var constValueString = jsonQueryWhere.Value.Substring(1, jsonQueryWhere.Value.Length - 2);
                    var constValue = Convert.ChangeType(constValueString, left.Type);
                    right = Expression.Constant(constValue);
                }
                else
                {
                    right = _GetMemberExpression(typeParameter, jsonQueryWhere.Value);
                }
            }
            else if (jsonQueryWhere.Values != null && jsonQueryWhere.Values.Any())
            {
                var listType = typeof(List<>)
                    .MakeGenericType(left.Type);
                var listAddMethod = listType.GetMethod("Add");
                var valueList = Activator.CreateInstance(listType, jsonQueryWhere.Values.Length);
                for (int i = 0; i < jsonQueryWhere.Values.Length; i++)
                {
                    var typedValue = Convert.ChangeType(jsonQueryWhere.Values[i], left.Type);
                    listAddMethod.Invoke(valueList, new object[] { typedValue });
                }
                right = Expression.Constant(valueList);
            }

            switch (jsonQueryWhere.Operator.ToLower())
            {
                case "=":
                    return Expression.Equal(left, right);
                case "!=":
                    return Expression.NotEqual(left, right);
                case ">":
                    return Expression.GreaterThan(left, right);
                case ">=":
                    return Expression.GreaterThanOrEqual(left, right);
                case "<":
                    return Expression.LessThan(left, right);
                case "<=":
                    return Expression.LessThanOrEqual(left, right);
                case "in":
                    var method = new Func<IQueryable<object>, object, bool>(Enumerable.Contains)
                        .GetMethodInfo()
                        .GetGenericMethodDefinition()
                        .MakeGenericMethod(left.Type);
                    return Expression.Call(
                        instance: null,
                        method: method,
                        arg0: right,
                        arg1: left);
                default:
                    throw new JsonQueryException($"Unknown operator '{jsonQueryWhere.Operator}'");
            }
        }

        private IQueryable _BuildWhereQueryable(IQueryable baseQueryable, Expression whereExpression)
        {
            var method = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where)
                .GetMethodInfo()
                .GetGenericMethodDefinition()
                .MakeGenericMethod(baseQueryable.ElementType);

            return baseQueryable.Provider.CreateQuery(
                Expression.Call(
                    instance: null,
                    method: method,
                    arg0: baseQueryable.Expression,
                    arg1: Expression.Quote(whereExpression)));
        }

        private IQueryable _BuildWhereQueryable(IQueryable baseQueryable, List<JsonQueryWhere> jsonQueryWheres)
        {
            var whereExpression = _BuildWhereExpression(baseQueryable.ElementType, jsonQueryWheres);
            return _BuildWhereQueryable(baseQueryable, whereExpression);
        }

        #endregion Where

        #region OrderBy

        private LambdaExpression _GetOrderByExpression(ParameterExpression typeParameter, JsonQueryOrderBy jsonQueryOrderBy)
        {
            var memberExpression = _GetMemberExpression(typeParameter, jsonQueryOrderBy.Selector);
            var lambdaExpression = Expression.Lambda(
                body: memberExpression,
                typeParameter);
            return lambdaExpression;
        }

        private IQueryable _BuildOrderByQueryable(IQueryable baseQueryable, JsonQueryOrderBy jsonQueryOrderBy, bool isFirst)
        {
            var typeParameter = Expression.Parameter(baseQueryable.ElementType);
            var orderByExpression = _GetOrderByExpression(typeParameter, jsonQueryOrderBy);
            MethodInfo methodInfo;

            if (isFirst)
            {
                if (jsonQueryOrderBy.Direction == JsonQueryOrderDirection.Asc)
                {
                    methodInfo = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderBy)
                        .GetMethodInfo();
                }
                else
                {
                    methodInfo = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderByDescending)
                        .GetMethodInfo();
                }
            }
            else
            {
                if (jsonQueryOrderBy.Direction == JsonQueryOrderDirection.Asc)
                {
                    methodInfo = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenBy)
                        .GetMethodInfo();
                }
                else
                {
                    methodInfo = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenByDescending)
                        .GetMethodInfo();
                }
            }

            var method = methodInfo
                .GetGenericMethodDefinition()
                .MakeGenericMethod(baseQueryable.ElementType, orderByExpression.ReturnType);
            return baseQueryable.Provider.CreateQuery(
                Expression.Call(
                    instance: null,
                    method: method,
                    arg0: baseQueryable.Expression,
                    arg1: Expression.Quote(orderByExpression)));
        }

        private IQueryable _BuildOrderByQueryable(IQueryable baseQueryable, List<JsonQueryOrderBy> jsonQueryOrderBies)
        {
            var queryable = baseQueryable;
            bool isFirst = true;
            for (int i = 0; i < jsonQueryOrderBies.Count; i++)
            {
                queryable = _BuildOrderByQueryable(queryable, jsonQueryOrderBies[i], isFirst);
                isFirst = false;
            }
            return queryable;
        }

        #endregion OrderBy

        #region Limit

        private IQueryable _BuildLimitSkipQueryable(IQueryable queryable, int skip)
        {
            var method = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Skip)
                .GetMethodInfo()
                .GetGenericMethodDefinition()
                .MakeGenericMethod(queryable.ElementType);
            var expression = Expression.Constant(skip);

            return queryable.Provider.CreateQuery(
                Expression.Call(
                    instance: null,
                    method: method,
                    arg0: queryable.Expression,
                    arg1: expression));
        }

        private IQueryable _BuildLimitTakeQueryable(IQueryable queryable, int take)
        {
            var method = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Take)
                .GetMethodInfo()
                .GetGenericMethodDefinition()
                .MakeGenericMethod(queryable.ElementType);
            var expression = Expression.Constant(take);

            return queryable.Provider.CreateQuery(
                Expression.Call(
                    instance: null,
                    method: method,
                    arg0: queryable.Expression,
                    arg1: expression));
        }

        private IQueryable _BuildLimitQueryable(IQueryable baseQueryable, JsonQueryLimit jsonQueryLimit)
        {
            var queryable = baseQueryable;
            if (jsonQueryLimit.Skip > 0)
            {
                queryable = _BuildLimitSkipQueryable(queryable, jsonQueryLimit.Skip);
            }
            if (jsonQueryLimit.Take > 0)
            {
                queryable = _BuildLimitTakeQueryable(queryable, jsonQueryLimit.Take);
            }
            return queryable;
        }

        #endregion Limit

        #endregion Private
    }
}
