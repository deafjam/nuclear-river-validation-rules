using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace NuClear.ValidationRules.Storage
{
    internal static class SchemaExtensions
    {
        public static MappingSchema RegisterDataTypes(this MappingSchema schema)
        {
            schema.SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, 19, 4));
            schema.SetDataType(typeof(decimal?), new SqlDataType(DataType.Decimal, 19, 4));
            schema.SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, int.MaxValue));
            schema.SetDataType(typeof(byte[]), new SqlDataType(DataType.VarBinary, int.MaxValue));

            // XDocument mapping to nvarchar
            schema.SetDataType(typeof(XDocument), new SqlDataType(DataType.NVarChar, 4000));
            schema.SetConvertExpression<string, XDocument>(x => XDocument.Parse(x));
            schema.SetConvertExpression<XDocument, string>(x => x.ToString(SaveOptions.DisableFormatting));
            schema.SetConvertExpression<XDocument, DataParameter>(x => new DataParameter { DataType = DataType.NVarChar, Value = x.ToString(SaveOptions.DisableFormatting) });

            return schema;
        }

        public static EntityMappingBuilder<T> HasIndex<T>(this EntityMappingBuilder<T> builder, Expression<Func<T, object>> fields)
        {
            var fieldsVisitor = new Visitor();
            fieldsVisitor.Visit(fields);

            builder.HasAttribute(new IndexAttribute { Fields = fieldsVisitor.Members, Include = Array.Empty<MemberInfo>() });

            return builder;
        }

        public static EntityMappingBuilder<T> HasIndex<T>(this EntityMappingBuilder<T> builder, Expression<Func<T, object>> fields, Expression<Func<T, object>> fieldsInclude)
        {
            var fieldsVisitor = new Visitor();
            fieldsVisitor.Visit(fields);

            var fieldsIncludeVisitor = new Visitor();
            fieldsIncludeVisitor.Visit(fieldsInclude);

            builder.HasAttribute(new IndexAttribute { Fields = fieldsVisitor.Members, Include = fieldsIncludeVisitor.Members });

            return builder;
        }

        public sealed class IndexAttribute : Attribute
        {
            public IReadOnlyCollection<MemberInfo> Fields { get; set; }
            public IReadOnlyCollection<MemberInfo> Include { get; set; }
        }

        private sealed class Visitor : ExpressionVisitor
        {
            private readonly HashSet<MemberInfo> _members = new HashSet<MemberInfo>();

            public IReadOnlyCollection<MemberInfo> Members => _members;

            protected override Expression VisitMember(MemberExpression node)
            {
                _members.Add(node.Member);
                return base.VisitMember(node);
            }
        }
    }
}