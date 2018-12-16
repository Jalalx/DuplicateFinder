using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DuplicateFinder
{

    class FileData
    {
        public string FileName { get; set; }

        public string DuplicateFileName { get; set; }
    }

    class Program
    {
        static readonly Dictionary<string, FileData> _files = new Dictionary<string, FileData>();

        private static void Traverse(string path)
        {
            try
            {
                var files = Directory.GetFiles(path);
                foreach (var fileName in files)
                {
                    UpdateLine(fileName);
                    ProcessFile(fileName);
                }
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    Traverse(dir);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        static private void ProcessFile(string fileName)
        {
            var info = new FileInfo(fileName);
            var hash = CalculateHash(fileName);
            if (!string.IsNullOrEmpty(hash))
            {
                if (_files.ContainsKey(hash))
                {
                    var alreadyExistFileName = _files[hash].FileName;
                    var alreadyExistFileInfo = new FileInfo(fileName);

                    if (alreadyExistFileInfo.CreationTimeUtc < info.CreationTimeUtc)
                    {
                        _files[hash].FileName = alreadyExistFileName;
                        _files[hash].DuplicateFileName = fileName;

                        ShowDuplicateFiles(_files[hash]);
                    }
                    else
                    {
                        _files[hash].DuplicateFileName = alreadyExistFileName;
                        ShowDuplicateFiles(_files[hash]);
                    }
                }
                else
                {
                    _files.Add(hash, new FileData { FileName = fileName });
                }
            }
        }

        private static void ShowDuplicateFiles(FileData fileData)
        {
            WarningMessage($"\r\nOriginal: {fileData.FileName}\tDuplicate: ${fileData.DuplicateFileName}");
        }

        private static string CalculateHash(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("filename is null or empty.", nameof(fileName));
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("File not found!", fileName);
            }

            try
            {
                using (var hasher = MD5.Create())
                using (var bufferedStream = new BufferedStream(File.OpenRead(fileName), 5 * 1024 * 1024))
                {
                    var hashedBytes = hasher.ComputeHash(bufferedStream);
                    return hashedBytes.Select(b => b.ToString("X2")).Aggregate((a, b) => a + b);
                }
            }
            catch (IOException ex)
            {
                ErrorMessage(ex);
                return null;
            }
        }

        static void ErrorMessage(Exception ex)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("\r\n" + ex.Message);
            Console.ForegroundColor = color;
        }

        static void WarningMessage(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine("\r\n" + message);
            Console.ForegroundColor = color;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("No path is specified!");
            }
            else if (!Directory.Exists(args[0]))
            {
                System.Console.WriteLine($"Path '{args[0]}' not exists!");
            }
            else
            {
                Traverse(args[0]);

                var duplicateFiles = _files
                    .Where(x => !string.IsNullOrEmpty(x.Value.DuplicateFileName))
                    .Select(x => x.Value)
                    .ToList();

                if (duplicateFiles.Any())
                {
                    WarningMessage($"{duplicateFiles.Count} duplicate files where found!");
                }
                else
                {
                    Console.WriteLine("No duplicate file was found.");
                }

                Console.WriteLine("Press any key to exit!");
                Console.ReadKey();
            }
        }

        private static void UpdateLine(string message)
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);

            Console.Write(message);
        }
    }
}
