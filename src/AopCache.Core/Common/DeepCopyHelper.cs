//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;

//namespace AopCache.Core.Common
//{
//    /// <summary>
//    /// 深拷贝
//    /// </summary>
//    public class DeepCopyHelper
//    {
//        public static T Copy<T>(T data)
//        {
//            if (data == null) return default;
//            return TransExp<T, T>.Trans(data);
//        }
//    }

//    public static class TransExp<TIn, TOut>
//    {
//        private static readonly Func<TIn, TOut> Cache = GetFunc();
//        private static Func<TIn, TOut> GetFunc()
//        {
//            var parameterExpression = Expression.Parameter(typeof(TIn), "p");
//            var memberBindingList = new List<MemberBinding>();

//            foreach (var item in typeof(TOut).GetProperties())
//            {
//                if (!item.CanWrite) continue;
//                MemberExpression property = Expression.Property(parameterExpression, typeof(TIn).GetProperty(item.Name));
//                MemberBinding memberBinding = Expression.Bind(item, property);
//                memberBindingList.Add(memberBinding);
//            }

//            var memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), memberBindingList.ToArray());
//            var lambda = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression, new ParameterExpression[] { parameterExpression });

//            return lambda.Compile();
//        }

//        public static TOut Trans(TIn tIn)
//        {
//            return Cache(tIn);
//        }
//    }

//    public static class TransExp
//    {
//        private static readonly Func<dynamic, dynamic> Cache = GetFunc();
//        private static Func<dynamic, dynamic> GetFunc(Type typeIn,Type typeOut)
//        {
//            var parameterExpression = Expression.Parameter(typeIn, "p");
//            var memberBindingList = new List<MemberBinding>();

//            foreach (var item in typeOut.GetProperties())
//            {
//                if (!item.CanWrite) continue;
//                MemberExpression property = Expression.Property(parameterExpression, typeIn.GetProperty(item.Name));
//                MemberBinding memberBinding = Expression.Bind(item, property);
//                memberBindingList.Add(memberBinding);
//            }

//            var memberInitExpression = Expression.MemberInit(Expression.New(typeOut), memberBindingList.ToArray());
//            var lambda = Expression.Lambda<Func<dynamic, dynamic>>(memberInitExpression, new ParameterExpression[] { parameterExpression });

//            return lambda.Compile();
//        }

//        public static TOut Trans(dynamic tIn,Type typeIn,Type typeOut)
//        {
//            return Cache(typeIn,typeOut,tIn);
//        }
//    }
//}