using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;

namespace NuClear.ValidationRules.Replication.Specifications
{
    public static partial class Specs
    {
        public static class Join
        {
            public static class Aggs
            {
                private const int NoDependency = 2;
                private const int BindingObjectMatch = 1;
                private const int Different = 3;

                public static Expression<Func<Firm.FirmPosition, AssociatedPositionDto>> WithPrincipalPositions(IQueryable<Firm.FirmAssociatedPosition> requirements, IQueryable<Firm.FirmPosition> principals)
                {
                    Expression<Func<Firm.FirmPosition, AssociatedPositionDto>> expression =
                        associated => new AssociatedPositionDto
                        {
                            Associated = associated,
                            RequirePrincipal = requirements.Any(requirement => requirement.OrderPositionId == associated.OrderPositionId && requirement.ItemPositionId == associated.ItemPositionId),
                            Principals = PrincipalPositions().Compile().Invoke(associated, requirements, principals)
                        };
                    return (Expression<Func<Firm.FirmPosition, AssociatedPositionDto>>)new ExpandMethodCallVisitor().Visit(expression);
                }

                public static Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>> PrincipalPositions(IQueryable<Firm.FirmAssociatedPosition> requirements, IQueryable<Firm.FirmPosition> principals)
                {
                    Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>> expression =
                        associated => PrincipalPositions().Compile().Invoke(associated, requirements, principals);
                    return (Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>>)new ExpandMethodCallVisitor().Visit(expression);
                }

                private static Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmAssociatedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>> PrincipalPositions()
                {
                    Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmAssociatedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>> expression =
                        (associated, requirements, principals) =>
                            from requirement in requirements.Where(x => x.OrderPositionId == associated.OrderPositionId && x.ItemPositionId == associated.ItemPositionId)
                            from principal in principals
                                .Where(x => x.ItemPositionId == requirement.PrincipalPositionId && x.FirmId == requirement.FirmId)
                                .Where(x => x.Start == associated.Start && x.FirmId == associated.FirmId)
                                .Where(x => Scope.CanSee(associated.Scope, x.Scope))
                            select new RelatedPositionDto
                            {
                                Position = principal,
                                RequiredMatch = requirement.BindingType == BindingObjectMatch,
                                RequiredDifferent = requirement.BindingType == Different,
                                IsBindingObjectConditionSatisfied = requirement.BindingType == NoDependency || (BindingObjectEquals().Compile().Invoke(principal, associated) ? requirement.BindingType == BindingObjectMatch : requirement.BindingType == Different)
                            };
                    return (Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmAssociatedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>>)new ExpandMethodCallVisitor().Visit(expression);
                }

                public static Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>> DeniedPositions(IQueryable<Firm.FirmDeniedPosition> requirements, IQueryable<Firm.FirmPosition> principals)
                {
                    Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>> expression =
                        associated => DeniedPositions().Compile().Invoke(associated, requirements, principals);
                    return (Expression<Func<Firm.FirmPosition, IEnumerable<RelatedPositionDto>>>)new ExpandMethodCallVisitor().Visit(expression);
                }

                private static Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmDeniedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>> DeniedPositions()
                {
                    Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmDeniedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>> expression =
                        (associated, requirements, principals) =>
                            from requirement in requirements.Where(x => x.OrderPositionId == associated.OrderPositionId && x.ItemPositionId == associated.ItemPositionId)
                            from principal in principals
                                .Where(x => x.ItemPositionId == requirement.DeniedPositionId && x.FirmId == requirement.FirmId)
                                .Where(x => requirement.Begin <= x.Start && x.Start < requirement.End)
                                .Where(x => x.Start == associated.Start && x.FirmId == associated.FirmId)
                                .Where(x => Scope.CanSee(associated.Scope, x.Scope))
                            select new RelatedPositionDto
                            {
                                Position = principal,
                                IsBindingObjectConditionSatisfied = requirement.BindingType == NoDependency || (BindingObjectEquals().Compile().Invoke(principal, associated) ? requirement.BindingType == BindingObjectMatch : requirement.BindingType == Different)
                            };
                    return (Expression<Func<Firm.FirmPosition, IQueryable<Firm.FirmDeniedPosition>, IQueryable<Firm.FirmPosition>, IEnumerable<RelatedPositionDto>>>)new ExpandMethodCallVisitor().Visit(expression);
                }

                /// <summary>
                /// Возвращает выражение для сравнения объектов привязки.
                /// Выражение пытается реализивать таблицу соответствий, описанную в документации:
                /// см. /docs/ru/validation-rules/compare-linking-objects.md
                /// Выражение достаточно не тривиальное и используется многократно, поэтому и создан <see cref="ExpandMethodCallVisitor"/>
                /// 
                /// Внимание: эта проверка не рассчитана на случай, когда заполнена C3 и не заполнена C1, поскольку такого не встречается.
                /// </summary>
                public static Expression<Func<Firm.IBindingObject, Firm.IBindingObject, bool>> BindingObjectEquals()
                {
                    return (x, y) => x.HasNoBinding == y.HasNoBinding &&
                      (
                        x.Category3Id != null && x.Category3Id == y.Category3Id &&
                        (x.FirmAddressId == null || y.FirmAddressId == null)
                        ||
                        x.FirmAddressId == y.FirmAddressId &&
                          (
                              x.Category1Id == null ||
                              y.Category1Id == null ||
                              x.Category3Id != null && y.Category3Id != null && x.Category3Id == y.Category3Id ||
                              x.Category3Id == null && y.Category3Id == null && x.Category1Id == y.Category1Id
                          )
                       );
                }

                /// <summary>
                /// Позволяет заменить вызов метода, возвращающего выражение (с последующей компиляцией и вызовом, как того требует синтаксис)
                /// на собственно это результат вызова, что позволяет переиспользовать куски выражений при построении запросов.
                /// </summary>
                private sealed class ExpandMethodCallVisitor : ExpressionVisitor
                {
                    protected override Expression VisitMethodCall(MethodCallExpression node)
                    {
                        if (node.Method.Name == nameof(Func<object>.Invoke))
                        {
                            var compileInvocation = (MethodCallExpression)node.Object;
                            var expressionFactoryInvocation = (MethodCallExpression)compileInvocation.Object;
                            return ExpandExpression(expressionFactoryInvocation.Method, node.Arguments);
                        }

                        return base.VisitMethodCall(node);
                    }

                    private Expression ExpandExpression(MethodInfo method, ReadOnlyCollection<Expression> arguments)
                    {
                        if (!method.IsStatic || method.GetParameters().Any())
                        {
                            throw new ArgumentException($"Method {method.Name} has to be static parameterless to be expanded", nameof(method));
                        }

                        var constructedExpression = (LambdaExpression)method.Invoke(null, new object[0]);
                        var replacementDictionary = constructedExpression.Parameters.Zip(arguments, (parameter, argument) => new { parameter, argument })
                            .ToDictionary(x => x.parameter, x => x.argument);
                        return new ReplaceParameterVisitor(replacementDictionary).Visit(constructedExpression.Body);
                    }

                    /// <summary>
                    /// Позволяет заменить обращения к параметрам в выражении на некие другие обращения.
                    /// </summary>
                    private class ReplaceParameterVisitor : ExpressionVisitor
                    {
                        private readonly IDictionary<ParameterExpression, Expression> _dictionary;

                        public ReplaceParameterVisitor(IDictionary<ParameterExpression, Expression> dictionary) =>
                            _dictionary = dictionary;

                        protected override Expression VisitParameter(ParameterExpression node) =>
                            _dictionary.TryGetValue(node, out var replacement) ? replacement : base.VisitParameter(node);
                    }
                }

                public sealed class AssociatedPositionDto
                {
                    public Firm.FirmPosition Associated { get; set; }
                    public IEnumerable<RelatedPositionDto> Principals { get; set; }
                    public bool RequirePrincipal { get; set; }
                }

                public sealed class RelatedPositionDto
                {
                    public Firm.FirmPosition Position { get; set; }
                    public bool RequiredMatch { get; set; }
                    public bool RequiredDifferent { get; set; }
                    public bool IsBindingObjectConditionSatisfied { get; set; }
                }
            }
        }
    }
}