using System;
using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public class ConsoleReader
    {
        public ConsoleReader()
        {
            Console.Clear();
        }

        public int GetInt(string prompt)
        {
            return GetValue(prompt, int.Parse);
        }

        public string GetString(string prompt)
        {
            Console.Out.Write("Enter {0}: ", prompt);
            var line = Console.ReadLine();
            return line;
        }

        public T GetValueOf<T>(string prompt, IEnumerable<T> list, Func<T, string> formatter)
        {
            var data = list.ToList();
            return GetValueOf(prompt, data, data.Count, formatter);
        }

        public T GetValueOf<T>(string prompt, IEnumerable<T> list, int pageSize, Func<T, string> formatter)
        {
            Console.Out.WriteLine("{0}: ", prompt);
            int i = 0;
            int page = -1;
            bool selected = false;
            var data = list.ToList();
            var count = data.Count;

            T value = default(T);

            do
            {
                ++page;

                var enumerable = data.Skip(page * pageSize).Take(pageSize);

                foreach (T item in enumerable)
                {
                    Console.Out.WriteLine("\t{0,-4:0000}: {1}", ++i, formatter(item));
                }
                string line = Console.ReadLine();

                int selectedIndex;
                if (int.TryParse(line, out selectedIndex) && selectedIndex > 0 && selectedIndex <= count)
                {
                    value = data.ElementAt(--selectedIndex);
                    selected = true;
                }

            } while (!selected);

            return value;
        }

        public T GetValue<T>(string prompt, Func<string, T> convertor)
        {
            Console.Out.Write("Enter {0}: ", prompt);
            string line = Console.ReadLine();

            try
            {
                var value = convertor(line);

                return value;
            }
            catch
            {
                throw new Exception(string.Format("Invalid {0}", prompt));
            }
        }

        public List<T> GetList<T>(Func<string, T> convert)
        {
            string line = Console.ReadLine();
            var items = new List<T>();

            if (line != null)
            {
                string[] strings = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                items.AddRange(strings.Select(convert));
            }

            return items;
        }

        public bool Confirm(string message)
        {
            Console.Out.Write("{0} [Y/n]: ", message);

            var c = Console.ReadKey().KeyChar;

            if (c.Equals('y') || c.Equals('Y') || c.Equals('\r'))
            {
                Console.Out.WriteLine("");
                return true;
            }

            if (c.Equals('n') || c.Equals('N'))
            {
                Console.Out.WriteLine("");
                return false;
            }

            Console.Out.WriteLine("invalid choice...");
            return Confirm(message);
        }
    }
}