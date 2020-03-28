using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Cod
{
    public static class ValidationHelper
    {
        public static bool TryValidate<TEntity>(this TEntity model, out ValidationState result)
               => TryValidate<TEntity, string>(model, null, out result);

        public static bool TryValidate<TEntity, TProperty>(this TEntity model, Expression<Func<TEntity, TProperty>> exp, out ValidationState result)
        {
            result = new ValidationState();
            var context = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();

            bool isValid;
            if (exp == null)
            {
                isValid = Validator.TryValidateObject(model, context, validationResults, true);
            }
            else
            {
                if (exp.Body is MemberExpression mexp && mexp.NodeType == ExpressionType.MemberAccess)
                {
                    context.MemberName = mexp.Member.Name;
                }
                else
                {
                    throw new NotSupportedException($"Validation not supported on: {exp.ToString()}");
                }

                var func = exp.Compile();
                var value = func(model);
                isValid = Validator.TryValidateProperty(value, context, validationResults);
            }

            if (!isValid)
            {
                foreach (var item in validationResults)
                {
                    foreach (var member in item.MemberNames)
                    {
                        result.AddError(member, item.ErrorMessage);
                    }
                }
            }

            return isValid;
        }
    }
}
