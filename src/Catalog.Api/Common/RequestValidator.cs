using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Common;

/// <summary>
/// Validação de entrada com DataAnnotations (BCL, zero dependência externa).
/// No Módulo 2 isto é substituído por FluentValidation, que expressa regras
/// condicionais e compostas sem virar um emaranhado de atributos.
/// </summary>
public static class RequestValidator
{
    public static bool TryValidate<T>(T instance, out Dictionary<string, string[]> errors)
        where T : notnull
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        // validateAllProperties: true — sem isso, só o [Required] é avaliado. Pegadinha clássica.
        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            errors = [];
            return true;
        }

        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results)
        {
            var members = result.MemberNames.ToArray();
            if (members.Length == 0)
            {
                members = ["request"];
            }

            foreach (var member in members)
            {
                if (!grouped.TryGetValue(member, out var messages))
                {
                    messages = [];
                    grouped[member] = messages;
                }

                messages.Add(result.ErrorMessage ?? "Valor inválido.");
            }
        }

        errors = grouped.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        return false;
    }
}
