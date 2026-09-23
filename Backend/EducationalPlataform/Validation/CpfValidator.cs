namespace EducationalPlataform.Validation
{
    public static class CpfValidator
    {
        public static string DigitsOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        public static bool IsValid(string? value)
        {
            var cpf = DigitsOnly(value);

            if (cpf.Length != 11)
            {
                return false;
            }

            if (cpf.Distinct().Count() == 1)
            {
                return false;
            }

            return CheckDigit(cpf, 9) && CheckDigit(cpf, 10);
        }

        public static string Format(string? value)
        {
            var digits = DigitsOnly(value);

            if (digits.Length != 11)
            {
                return value?.Trim() ?? string.Empty;
            }

            return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
        }

        public static bool SameCpf(string? left, string? right)
        {
            var leftDigits = DigitsOnly(left);
            var rightDigits = DigitsOnly(right);

            return leftDigits.Length == 11
                && leftDigits == rightDigits;
        }

        private static bool CheckDigit(string cpf, int length)
        {
            var sum = 0;
            var weight = length + 1;

            for (var i = 0; i < length; i++)
            {
                sum += (cpf[i] - '0') * (weight - i);
            }

            var remainder = sum % 11;
            var digit = remainder < 2 ? 0 : 11 - remainder;

            return cpf[length] - '0' == digit;
        }
    }
}
