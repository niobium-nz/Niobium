using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Cod
{
    public static class ValidationHelper
    {
        public static bool ValidateChineseMobileNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (input.StartsWith("0086"))
            {
                input = input[4..];
            }

            if (input.StartsWith("+86"))
            {
                input = input[3..];
            }

            return input.Length == 11 && input.All(char.IsDigit) && input[0] == '1';
        }


        public static bool TryValidate<TEntity>(this TEntity model, out ValidationState result)
        {
            return TryValidate<TEntity, string>(model, null, out result);
        }

        public static bool TryValidate<TEntity, TProperty>(this TEntity model, Expression<Func<TEntity, TProperty>>? exp, out ValidationState result)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Model cannot be null.");
            }

            if (model is IUserInput formatable)
            {
                formatable.Sanitize();
            }

            result = new ValidationState();
            ValidationContext context = new(model);
            List<ValidationResult> validationResults = [];

            bool isValid;
            if (exp == null)
            {
                isValid = Validator.TryValidateObject(model, context, validationResults, true);
            }
            else
            {
                context.MemberName = exp.Body is MemberExpression mexp && mexp.NodeType == ExpressionType.MemberAccess
                    ? mexp.Member.Name
                    : throw new NotSupportedException($"Validation not supported on: {exp}");

                Func<TEntity, TProperty> func = exp.Compile();
                TProperty value = func(model);
                isValid = Validator.TryValidateProperty(value, context, validationResults);
            }

            if (!isValid)
            {
                foreach (ValidationResult item in validationResults)
                {
                    foreach (string member in item.MemberNames)
                    {
                        if (item.ErrorMessage != null)
                        {
                            result.AddError(member, item.ErrorMessage);
                        }
                    }
                }
            }

            return isValid;
        }
    }
}
