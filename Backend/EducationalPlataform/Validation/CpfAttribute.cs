using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class CpfAttribute : ValidationAttribute
    {
        public bool AllowEmpty { get; set; }

        public CpfAttribute()
        {
            ErrorMessage = "CPF inválido.";
        }

        public override bool IsValid(object? value)
        {
            if (value is not string cpf || string.IsNullOrWhiteSpace(cpf))
            {
                return AllowEmpty;
            }

            return CpfValidator.IsValid(cpf);
        }
    }
}
