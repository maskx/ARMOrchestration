using maskx.Expression;
using maskx.Expression.Expressions;
using maskx.Expression.Visitors;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.Functions
{
    internal class FunctionVisitor : EvaluationVisitor
    {
        public FunctionVisitor(EvaluateOptions options) : base(options)
        {
        }

        public List<FunctionExpression> ReferenceFunction { get; set; } = new List<FunctionExpression>();

        //public override void Visit(TernaryExpression expression)
        //{
        //    expression.LeftExpression.Accept(this, null);
        //    expression.RightExpression.Accept(this, null);
        //    expression.MiddleExpression.Accept(this, null);
        //}

        //public override void Visit(BinaryExpression expression)
        //{
        //    expression.LeftExpression.Accept(this, null);
        //    expression.RightExpression.Accept(this, null);
        //}

        //public override void Visit(UnaryExpression expression)
        //{
        //    expression.Expression.Accept(this);
        //}

        //public override void Visit(ValueExpression expression)
        //{
        //}

        public override void Visit(FunctionExpression function, Dictionary<string, object> context = null)
        {
            if (function.Identifier.Name.Equals("reference", StringComparison.OrdinalIgnoreCase))
            {
                ReferenceFunction.Add(function);
                foreach (var item in function.Expressions)
                {
                    item.Accept(this, context);
                }
            }
            else
            {
                base.Visit(function, context);
            }
        }

        //public override void Visit(IdentifierExpression function)
        //{
        //}

        public override void Visit(MemberExpression expression, Dictionary<string, object> context = null)
        {
            if (expression.LeftExpression is FunctionExpression function)
            {
                if (function.Identifier.Name.Equals("reference", StringComparison.OrdinalIgnoreCase))
                {
                    ReferenceFunction.Add(function);
                    foreach (var item in function.Expressions)
                    {
                        item.Accept(this, context);
                    }
                    return;
                }
            }
            base.Visit(expression, context);
        }
    }
}