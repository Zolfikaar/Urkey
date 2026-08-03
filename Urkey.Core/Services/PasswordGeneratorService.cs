using System.Security.Cryptography;

namespace Urkey.Core.Services
{
    public sealed class PasswordGeneratorOptions
    {
        public int Length { get; set; } = 16;
        public bool UseLowercase { get; set; } = true;
        public bool UseUppercase { get; set; } = true;
        public bool UseDigits { get; set; } = true;
        public bool UseSymbols { get; set; } = true;
    }

    public static class PasswordGeneratorService
    {
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?";

        public static string Generate(PasswordGeneratorOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.Length < 4)
                throw new ArgumentOutOfRangeException(nameof(options.Length));

            var pools = new List<string>();
            if (options.UseLowercase) pools.Add(Lower);
            if (options.UseUppercase) pools.Add(Upper);
            if (options.UseDigits) pools.Add(Digits);
            if (options.UseSymbols) pools.Add(Symbols);

            if (pools.Count == 0)
                throw new InvalidOperationException("At least one character set must be selected.");

            var all = string.Concat(pools);
            var chars = new char[options.Length];

            // Guarantee at least one char from each selected set when length allows.
            int i = 0;
            foreach (var pool in pools)
            {
                if (i >= options.Length) break;
                chars[i++] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
            }

            for (; i < options.Length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            // Fisher–Yates shuffle
            for (int n = chars.Length - 1; n > 0; n--)
            {
                int j = RandomNumberGenerator.GetInt32(n + 1);
                (chars[n], chars[j]) = (chars[j], chars[n]);
            }

            return new string(chars);
        }
    }
}
