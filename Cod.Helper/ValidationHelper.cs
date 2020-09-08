using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace Cod
{
    public static class ValidationHelper
    {
        public static bool ValidateChineseMobileNumber(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (input.StartsWith("0086"))
            {
                input = input.Substring(4, input.Length - 4);
            }

            if (input.StartsWith("+86"))
            {
                input = input.Substring(3, input.Length - 3);
            }

            if (input.Length != 11 || !input.All(char.IsDigit) || input[0] != '1')
            {
                return false;
            }

            return true;
        }


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
                    throw new NotSupportedException($"Validation not supported on: {exp}");
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

            if (isValid && model is IFormatable formatable)
            {
                formatable.Format();
            }

            return isValid;
        }
    }
}
