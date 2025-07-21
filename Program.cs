using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace TextTranslator
{
    internal class Program
    {
        private static int notFoundCounter = 0;
        private static List<string> errors = new List<string>();
        static void Main(string[] args)
        {
            Console.Title = "TextTranslator v1.02";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string translationsPath = @"en.txt";

            //PrepareWords(args[0]);

            if (args.Length == 0 || !File.Exists(args[0]))
            {
                Console.WriteLine("[Fail] Please drop 'index-xxxxxxx.js' for translations. Accept any 'js' file");
                Console.ReadLine();
                return;
            }

            if (!File.Exists(translationsPath))
            {
                Console.WriteLine("[Fail] Translation dictionary file not found. (require 'en.txt')");
                Console.ReadLine();
                return;
            }

            string inputPath = args[0];
            bool isOrigFile = inputPath.EndsWith(".js.orig");
            bool isJsFile = inputPath.EndsWith(".js");

            if (File.Exists($"{args[0]}.orig") && isJsFile)
            {
                Console.WriteLine("[Fail] There is already .js backup created, please use it!");
                Console.WriteLine("----------------------------------------------------------\n");
                Console.WriteLine("Use '.js.orig' file or remove it!");
                Console.ReadLine();
                return;
            }

            if (!isJsFile && !isOrigFile)
            {
                Console.WriteLine("Please drop 'index-c0043f83.js' for translations. Accept any 'js' file");
                Console.ReadLine();
                return;
            }

            string content = File.ReadAllText(args[0]);
            var translations = new Dictionary<string, string>();

            foreach (var line in File.ReadLines(translationsPath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
                    continue;

                var parts = line.Split(new[] { '|' }, 2);
                if (!translations.ContainsKey(parts[0]))
                    translations[parts[0]] = parts[1];
            }

            var sorted = translations
                .OrderByDescending(kv => kv.Key.Length)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var pair in sorted)
            {
                var zzz = content;
                content = content.Replace(pair.Key, pair.Value);
                if (content != zzz)
                {
                    Console.WriteLine($"Translated [{pair.Value}]");
                }
                else
                {
                    notFoundCounter++;
                    Console.WriteLine($"Missing Translation {pair.Value}");
                    errors.Add($"{pair.Key} - {pair.Value}");
                }
            }
            watch.Stop();
            Console.WriteLine($"Execution time:{watch.ElapsedMilliseconds}ms");

            var proMode = @"static versionType(){var t;return((t=window.electron)==null?void 0:t.versionType)??""normal""}static isProfessional(){var t;return((t=window.electron)==null?void 0:t.versionType)==""professional""}";
            if (content.IndexOf(proMode) > 0)
            {
                content = content.Replace(
                    proMode,
                    @"static versionType(){return""professional""}static isProfessional(){return true}"
                );
                Console.WriteLine($"******************************************");
                Console.WriteLine($"[Success] PRO version patch applied!");
            }
            else
            {
                Console.WriteLine($"******************************************");
                Console.WriteLine($"[Fail] Faild to apply PRO version!");
            }

            Console.WriteLine($"");

            if (isOrigFile)
            {
                var fileNoExt = args[0].Substring(0, args[0].Length - 5);
                if (File.Exists(fileNoExt))
                {
                    File.Delete(fileNoExt);
                }
                File.WriteAllText(fileNoExt, content);
            }
            else
            {
                File.WriteAllText($"{args[0]}.orig", File.ReadAllText(args[0]));
                File.SetAttributes($"{args[0]}.orig", FileAttributes.ReadOnly);
                File.Delete(args[0]);
                File.WriteAllText(args[0], content);
            }

            if (notFoundCounter > 0)
            {
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Warning] Not all strings are translated!");
                Console.ForegroundColor = currentColor;
                Console.WriteLine($"Press any key to view");
                Console.ReadLine();
                foreach (var error in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed translation [{error}]");
                    Console.ForegroundColor = currentColor;
                }
                Console.ForegroundColor = Console.ForegroundColor;
            }

            Console.WriteLine("Translation complete.");
            Console.ReadLine();
        }

        private static void PrepareWords(string inputPath)
        {
            string text = File.ReadAllText(inputPath);

            var matches = Regex.Matches(text, @"\b(content|placeholder|label|comment|title|errMsg|tooltip|message):\s*""([^""]*[\u4e00-\u9fff][^""]*)""");

            var results = new HashSet<string>();

            foreach (Match match in matches)
            {
                //results.Add(match.Groups[2].Value);
                results.Add(match.Value);
            }

            File.WriteAllLines("output.csv", results);

            Console.WriteLine("Done. Saved: " + results.Count + " items.");
        }
    }
}
