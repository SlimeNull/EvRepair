using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUtils
{
    public static class ConsoleInput
    {
        public delegate bool ValueParser<TValue>(string str, out TValue value);

        public static TValue Any<TValue>(string prompt, string errorMessage, ValueParser<TValue> parser)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (input == null)
                    Environment.Exit(-1);

                if (parser.Invoke(input ?? string.Empty, out TValue value))
                    return value;

                Console.WriteLine(errorMessage);
            }
        }

        public static string String(string prompt)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (input == null)
                Environment.Exit(-1);

            return input ?? throw new InvalidOperationException();
        }

        public static string AnyString(string prompt, string errorMessage, Predicate<string> predicate)
        {
            while (true)
            {
                string input = String(prompt);

                if (predicate.Invoke(input))
                    return input;

                Console.WriteLine(errorMessage);
            }
        }

        public static Int32 Int32(string prompt) => Any<Int32>(prompt, "Invalid format", System.Int32.TryParse);
        public static Int64 Int64(string prompt) => Any<Int64>(prompt, "Invalid format", System.Int64.TryParse);
        public static Single Single(string prompt) => Any<Single>(prompt, "Invalid format", System.Single.TryParse);
        public static Double Double(string prompt) => Any<Double>(prompt, "Invalid format", System.Double.TryParse);

        public static string FilePath(string prompt) => AnyString(prompt, "File not exist", str => File.Exists(str));
        public static string DirectoryPath(string prompt) => AnyString(prompt, "Directory not exist", str => Directory.Exists(str));


        public static bool YesOrNo(string prompt, bool? defaultValue)
        {
            string tail = defaultValue switch
            {
                null => "(y/n)",
                true => "(Y/n)",
                false => "y/N"
            };

            string message = $"{prompt} {tail} ";

            while (true)
            {
                string input = String(message);

                if (input.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    return true;
                else if (input.Equals("N", StringComparison.OrdinalIgnoreCase))
                    return false;
                else if (string.IsNullOrWhiteSpace(input) && defaultValue != null)
                    return defaultValue.Value;
                else
                    Console.WriteLine("Input Y or N");
            }
        }

        public static int SelectIndex(string prompt, int numberStart, params string[] choices)
        {
            if (choices == null)
                throw new ArgumentNullException(nameof(choices));
            if (choices.Length == 0)
                throw new ArgumentException("No choice");

            Console.WriteLine(prompt);

            for (int i = 0; i < choices.Length; i++)
                Console.WriteLine($"  {numberStart + i}. {choices[i]}");

            while (true)
            {
                string? input = Console.ReadLine();

                if (input == null)
                    Environment.Exit(-1);

                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] == input)
                        return i;

                if (int.TryParse(input, out int inputNumber))
                {
                    int inputIndex = inputNumber - numberStart;
                    if (inputIndex >= 0 && inputIndex < choices.Length)
                        return inputIndex;

                    Console.WriteLine("Invalid number");
                }
                else
                {
                    Console.WriteLine("Not a number or choice");
                }
            }
        }

        public static int SelectIndex(string prompt, params string[] choices)
            => SelectIndex(prompt, 1, choices);

        private static TValue SelectValueCore<TValue>(string prompt, int numberStart, Func<TValue, string> choicePicker, IReadOnlyList<TValue> values)
        {
            string[] choices = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                choices[i] = choicePicker.Invoke(values[i]);

            return values[SelectIndex(prompt, numberStart, choices)];
        }

        public static TValue SelectValue<TValue>(string prompt, int numberStart, Func<TValue, string> choicePicker, IReadOnlyList<TValue> values)
            => SelectValueCore(prompt, numberStart, choicePicker, values);

        public static TValue SelectValue<TValue>(string prompt, int numberStart, Func<TValue, string> choicePicker, params TValue[] values)
            => SelectValueCore(prompt, numberStart, choicePicker, values);

        public static TEnum SelectEnum<TEnum>(string prompt, int numberStart, Func<TEnum, string> choicePicker) where TEnum : Enum
        {
            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            return SelectValue(prompt, numberStart, choicePicker, values);
        }

        public static TValue SelectValue<TValue>(string prompt, IReadOnlyList<TValue> values) where TValue : notnull
            => SelectValueCore(prompt, 1, v => v.ToString() ?? string.Empty, values);

        public static TValue SelectValue<TValue>(string prompt, params TValue[] values) where TValue : notnull
            => SelectValueCore(prompt, 1, v => v.ToString() ?? string.Empty, values);

        public static TEnum SelectEnum<TEnum>(string prompt) where TEnum : Enum
            => SelectEnum<TEnum>(prompt, 1, v => v.ToString());

        public static ConsoleKeyInfo Key(string prompt)
        {
            Console.WriteLine(prompt);
            return Console.ReadKey(true);
        }
    }
}
