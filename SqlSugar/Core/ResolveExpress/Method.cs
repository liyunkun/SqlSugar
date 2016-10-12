﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;

namespace SqlSugar
{
    //局部类：解析函数
    internal partial class ResolveExpress
    {
        /// <summary>
        /// 是否相等
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <returns></returns>
        private string Equals(string methodName, MethodCallExpression mce) {
            MemberType leftType = MemberType.None;
            MemberType rightType = MemberType.None;
            var left = CreateSqlElements(mce.Object, ref leftType,true);
            var right = CreateSqlElements(mce.Arguments[0], ref rightType,true);
            var oldLeft = AddParas(ref left,right);
            return string.Format("({0} = " + SqlSugarTool.ParSymbol + "{1})", oldLeft, left);
        }

        /// <summary>
        /// 拉姆达StartsWith函数处理
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="isTure"></param>
        /// <returns></returns>
        private string StartsWith(string methodName, MethodCallExpression mce, bool isTure)
        {
            MemberType leftType = MemberType.None;
            MemberType rightType = MemberType.None;
            var left = CreateSqlElements(mce.Object, ref leftType,true);
            var right = CreateSqlElements(mce.Arguments[0], ref rightType,true);
            var oldLeft = AddParas(ref left, right + '%');
            return string.Format("({0} {1} LIKE " + SqlSugarTool.ParSymbol + "{2})", oldLeft, null, left);
        }

        /// <summary>
        /// 拉姆达EndWith函数处理
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="isTure"></param>
        /// <returns></returns>
        private string EndWith(string methodName, MethodCallExpression mce, bool isTure)
        {
            MemberType leftType = MemberType.None;
            MemberType rightType = MemberType.None;
            var left = CreateSqlElements(mce.Object, ref leftType,true);
            var right = CreateSqlElements(mce.Arguments[0], ref rightType,true);
            var oldLeft = AddParas(ref left, '%' + right);
            return string.Format("({0} {1} LIKE " + SqlSugarTool.ParSymbol + "{2})", oldLeft, null, left);
        }

        /// <summary>
        /// 拉姆达Contains函数处理
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="isTure"></param>
        /// <returns></returns>
        private string Contains(string methodName, MethodCallExpression mce, bool isTure)
        {
            MemberType leftType = MemberType.None;
            MemberType rightType = MemberType.None;
            var left = CreateSqlElements(mce.Object, ref leftType,true);
            var right = CreateSqlElements(mce.Arguments[0], ref rightType,true);
            if (left.IsCollectionsList() || right.IsStringArray() || right.IsEnumerable())
            {
                object containsValue = null;
                string fieldName = "";
                if (left.IsCollectionsList())
                {
                    fieldName = right;
                    MemberExpression mbx = ((MemberExpression)mce.Object);
                    Expression exp = mce.Object;
                    SetMemberValueToDynInv(ref exp, mbx, ref containsValue);

                }
                else
                {
                    MemberExpression mbx = ((MemberExpression)mce.Arguments[0]);
                    Expression exp = mce.Arguments[0];
                    SetMemberValueToDynInv(ref exp, mbx, ref containsValue);
                    fieldName = CreateSqlElements(mce.Arguments[1], ref rightType,true);
                }
                List<string> inArray = new List<string>();
                foreach (var item in (IEnumerable)containsValue)
                {
                    inArray.Add(item.ObjToString());
                }
                if (inArray.Count == 0)
                {
                    return (" (1=2) ");
                }
                var inValue = inArray.ToArray().ToJoinSqlInVal();
                return string.Format("({0} IN ({1}))", fieldName, inValue);
            }
            else if (mce.Arguments.Count == 2) { //两个值
                //object containsValue = null;
                //MemberExpression mbx = ((MemberExpression)mce.Arguments[0]);
                //Expression exp = mce.Arguments[0];
                //SetMemberValueToDynInv(ref exp, mbx, ref containsValue);
                //var fieldName = CreateSqlElements(mce.Arguments[1], ref rightType);
                //return null;
                throw new SqlSugarException("请将数组提取成变量，不能直接写在表达式中。");
            }
            else
            {
                var oldLeft = AddParas(ref left, '%' + right + '%');
                return string.Format("({0} {1} LIKE " + SqlSugarTool.ParSymbol + "{2})", oldLeft, null, left);
            }
        }

        /// <summary>
        /// 非空验证
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="isTure"></param>
        /// <returns></returns>
        private string IsNullOrEmpty(string methodName, MethodCallExpression mce, bool isTure)
        {
            MemberType leftType = MemberType.None;
            MemberType rightType = MemberType.None;
            var isConstant = mce.Arguments.First().NodeType == ExpressionType.Constant;
            var left = CreateSqlElements(mce.Object, ref leftType,true);
            var right = CreateSqlElements(mce.Arguments[0], ref rightType,true);
            if (right == "null")
            {
                right = "";
            }
            if (isConstant)
            {
                var oldLeft = AddParas(ref left, right);
                if (isTure)
                {
                    return string.Format("(" + SqlSugarTool.ParSymbol + "{0} is null OR " + SqlSugarTool.ParSymbol + "{0}='' )", left);
                }
                else
                {
                    return string.Format("(" + SqlSugarTool.ParSymbol + "{0} is not null AND " + SqlSugarTool.ParSymbol + "{0}<>'' )", left);
                }
            }
            else
            {
                if (isTure)
                {
                    if (rightType == MemberType.Key)
                    {
                        return string.Format("({0} is null OR {0}='' )", right.ToSqlFilter());
                    }
                    else
                    {
                        return string.Format("('{0}' is null OR '{0}'='' )", right.ToSqlFilter());
                    }
                }
                else
                {
                    if (rightType == MemberType.Key)
                    {
                        return string.Format("({0} is not null AND {0}<>'' )", right.ToSqlFilter());
                    }
                    else
                    {

                        return string.Format("('{0}' is not null AND '{0}'<>'' )", right.ToSqlFilter());
                    }
                }

            }
        }

        /// <summary>
        /// 拉姆达函数处理
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private string MethodTo(string methodName, MethodCallExpression mce, ref MemberType type)
        {
            string value = string.Empty;
            if (mce.Arguments.IsValuable())
            {
                value = CreateSqlElements(mce.Arguments.FirstOrDefault(), ref type,true);
            }
            else
            {
                value = MethodToString(methodName, mce, ref type); ;
            }
            if (methodName == "ToDateTime" || methodName == "ObjToDate")
            {
                return Convert.ToDateTime(value).ToString();
            }
            else if (methodName.StartsWith("ToInt"))
            {
                return Convert.ToInt32(value).ToString();
            }
            else if (methodName.StartsWith("Trim"))
            {
                return (value.ObjToString()).Trim();
            }
            else if (methodName.StartsWith("ObjTo"))
            {
                return value;
            }
            else if (methodName == "ToLower") {
                if (value == null) return value;
                else
                    return value.ToLower();
            }
            else if (methodName == "ToUpper")
            {
                if (value == null) return value;
                else
                    return value.ToUpper();
            }
            else
            {
                throw new SqlSugarException("不支持当前函数：" + methodName + "\r\n" + ResolveExpress.ExpToSqlError);
            }
        }

        /// <summary>
        /// 拉姆达ToString函数处理
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="mce"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private string MethodToString(string methodName, MethodCallExpression mce, ref MemberType type)
        {
            return CreateSqlElements(mce.Object, ref type,true);
        }
    }
}
