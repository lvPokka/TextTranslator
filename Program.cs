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
            Console.Title = "TextTranslator v1.04";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string translationsPath = @"en.txt";
            string patchesPath = @"patch.txt";

#if DEBUG
            //args = new string[] { "C:\\Users\\PoKka\\Desktop\\root\\cdn.cnbj1.fds.api.mi-img.com\\watchface-renderer-normal\\assets\\index-8966d1b3.js.orig" };//
            args = new string[] { "C:\\Users\\PoKka\\source\\repos\\TextTranslator\\TextTranslator\\bin\\Debug\\source.js.orig" };//
#endif
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

            if (!File.Exists(patchesPath))
            {
                Console.WriteLine("[Fail] Patch dictionary file not found. (require 'patch.txt')");
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

            string content = File.ReadAllText(inputPath, System.Text.Encoding.UTF8);
            var translations = new Dictionary<string, string>();

            foreach (var line in File.ReadLines(translationsPath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("¦"))
                    continue;

                var parts = line.Split(new[] { '¦' }, 2);
                if (!translations.ContainsKey(parts[0]))
                    translations[parts[0]] = parts[1];
            }

            var sorted = translations
                .OrderByDescending(kv => kv.Key.Length)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var pair in sorted)
            {
                bool success;
                if (pair.Key[0] == '⚡')
                {
                    content = FastTemplatePatch(content, pair.Key.Substring(1), pair.Value, out success);
                }
                else
                {
                    success = content.IndexOf(pair.Key, StringComparison.Ordinal) >= 0;
                    if (success)
                    {
                        content = content.Replace(pair.Key, pair.Value);
                    }
                }

                if (success)
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


            Console.WriteLine($"\n\n**************Patch Executor**************");
            foreach (var line in File.ReadLines(patchesPath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("¦"))
                    continue;

                var parts = line.Split(new[] { '¦' }, 3);

                bool isTemplatePatch = parts[0].StartsWith("⚡");
                string patchName = isTemplatePatch ? parts[0].Substring(1) : parts[0];
                string sourcePattern = parts[1];
                string targetPattern = parts[2];

                bool success = false;
                if (isTemplatePatch)
                {
                    content = FastTemplatePatch(content, sourcePattern, targetPattern, out success);
                }
                else
                {
                    int patchIdx = content.IndexOf(sourcePattern, StringComparison.Ordinal);
                    if (patchIdx >= 0)
                    {
                        content = content.Replace(sourcePattern, targetPattern);
                        success = true;
                    }
                }

                var currentColor = Console.ForegroundColor;
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Patch: {patchName}\nStatus: Success");
                    Console.ForegroundColor = currentColor;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Patch: {patchName}\nStatus: Failed");
                    Console.ForegroundColor = currentColor;
                }
                Console.WriteLine($"******************************************");
            }

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
            }

            Console.WriteLine("Translation complete.");
            Console.ReadLine();
        }

        // Task 2: Ultra-fast IndexOf-based scanner (No regex engine overhead, hardware-accelerated substring search)
        public static string FastTemplatePatch(string content, string sourceTemplate, string targetTemplate, out bool isSuccess)
        {
            var sourceMarkers = Regex.Matches(sourceTemplate, @"#(\d+)#");
            if (sourceMarkers.Count == 0)
            {
                int simpleIdx = content.IndexOf(sourceTemplate, StringComparison.Ordinal);
                if (simpleIdx >= 0)
                {
                    isSuccess = true;
                    return content.Replace(sourceTemplate, targetTemplate);
                }
                isSuccess = false;
                return content;
            }

            string[] anchors = Regex.Split(sourceTemplate, @"#\d+#");
            int[] sourceParamIds = new int[sourceMarkers.Count];
            int maxParamId = 0;
            for (int i = 0; i < sourceMarkers.Count; i++)
            {
                sourceParamIds[i] = int.Parse(sourceMarkers[i].Groups[1].Value);
                if (sourceParamIds[i] > maxParamId)
                    maxParamId = sourceParamIds[i];
            }

            // Pre-parse target template
            string[] targetParts = Regex.Split(targetTemplate, @"#\d+#");
            var targetMarkers = Regex.Matches(targetTemplate, @"#(\d+)#");
            int[] targetParamIds = new int[targetMarkers.Count];
            for (int i = 0; i < targetMarkers.Count; i++)
            {
                targetParamIds[i] = int.Parse(targetMarkers[i].Groups[1].Value);
            }

            var captured = new string[maxParamId + 1];
            System.Text.StringBuilder result = null;
            int lastCopiedIndex = 0;
            int searchIndex = 0;
            const int maxArgLength = 2000; // Protection

            while (searchIndex < content.Length)
            {
                int matchStart = content.IndexOf(anchors[0], searchIndex, StringComparison.Ordinal);
                if (matchStart < 0)
                    break;

                int currentPos = matchStart + anchors[0].Length;
                bool chainMatched = true;

                for (int i = 1; i < anchors.Length; i++)
                {
                    string anchor = anchors[i];
                    int nextPos = string.IsNullOrEmpty(anchor) 
                        ? currentPos 
                        : content.IndexOf(anchor, currentPos, StringComparison.Ordinal);

                    if (nextPos < 0 || (nextPos - currentPos) > maxArgLength)
                    {
                        chainMatched = false;
                        break;
                    }

                    int intermediateStart = content.IndexOf(anchors[0], currentPos, StringComparison.Ordinal);
                    if (intermediateStart >= 0 && intermediateStart < nextPos)
                    {
                        while (true)
                        {
                            int nextIntermediate = content.IndexOf(anchors[0], intermediateStart + 1, StringComparison.Ordinal);
                            if (nextIntermediate >= 0 && nextIntermediate < nextPos)
                                intermediateStart = nextIntermediate;
                            else
                                break;
                        }
                        chainMatched = false;
                        matchStart = intermediateStart - 1;
                        break;
                    }

                    int paramId = sourceParamIds[i - 1];
                    captured[paramId] = content.Substring(currentPos, nextPos - currentPos);
                    currentPos = nextPos + anchor.Length;
                }

                if (chainMatched)
                {
                    if (result == null)
                        result = new System.Text.StringBuilder(content.Length + 256);

                    result.Append(content, lastCopiedIndex, matchStart - lastCopiedIndex);

                    for (int i = 0; i < targetParts.Length; i++)
                    {
                        result.Append(targetParts[i]);
                        if (i < targetParamIds.Length)
                        {
                            int pId = targetParamIds[i];
                            if (pId < captured.Length && captured[pId] != null)
                            {
                                result.Append(captured[pId]);
                            }
                        }
                    }

                    lastCopiedIndex = currentPos;
                    searchIndex = currentPos;
                }
                else
                {
                    searchIndex = matchStart + 1;
                }
            }

            if (result != null)
            {
                result.Append(content, lastCopiedIndex, content.Length - lastCopiedIndex);
                isSuccess = true;
                return result.ToString();
            }

            isSuccess = false;
            return content;
        }
    }
}
