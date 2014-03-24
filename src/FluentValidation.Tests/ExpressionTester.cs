using FluentValidation.Validators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FluentValidation.Tests {
  [TestFixture]
  public class ExpressionTester  {
    public class Model {
      public int MyTestProp { get; set; }
      public string MyTestProp2 { get; set; }
    }

    public class TestValidator : AbstractValidator<Model> {
      public TestValidator() {
        When(x => x.MyTestProp == 1, () => {
          RuleFor(y => y.MyTestProp2).NotNull();
        });
      }
    }

    [Test]
    public void testc() {
      var validator = new TestValidator();
      foreach(var rule in validator) {
        var visitor = new Visitor();
        var stringval = visitor.visit(((DelegatingValidator)rule.Validators.First()).Expression);
      }
    }
 
    public class Visitor {
      public string visit(Expression expression) {
        switch (expression.NodeType) {
          case ExpressionType.Equal:
            return VisitBinary("==", ((BinaryExpression)expression).Left, ((BinaryExpression)expression).Right);
          case ExpressionType.Constant:
            return VisitConstant((ConstantExpression)expression);
          case ExpressionType.MemberAccess:
            return VisitMemberAccess((MemberExpression)expression);
          case ExpressionType.Convert:
            return VisitConvert((UnaryExpression)expression);
          case ExpressionType.Lambda:
            return VisitLambda((LambdaExpression)expression);
        }

        throw new Exception();
      }

      public string VisitBinary(string op, Expression lhs, Expression rhs) {
        return visit(lhs) + op + visit(rhs);
      }

      public string VisitConstant(ConstantExpression conexp) {
        if (conexp.Type == typeof(string)) {
          return "'" + conexp.Value + "'";
        }

        return conexp.Value.ToString();
      }

      public string VisitMemberAccess(MemberExpression memexp) {
        return memexp.Member.Name;
      }

      public string VisitConvert(UnaryExpression unexp) {
        return visit(unexp.Operand);
      }

      public string VisitLambda(LambdaExpression lamexp) {
        return visit(lamexp.Body);
      }
    }
  }
}
