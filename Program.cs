using static System.ConsoleColor;
using static System.Console;
using System.Text.RegularExpressions;
using System.IO;

namespace WhitespaceCleaner
{
    class Program
    {
        static int totalBlockRemoveCount = 0;
        static int totalCharRemoveCount = 0;
        static int extraLineBlockCount = 0;
        static int extraLineRemoveCount = 0;

        static void Main(string[] args)
        {
            if (args.Length == 0) {
                PromptEnd("Drag and drop the file/folder onto the .exe of this program to clean up the file/files.");
                return;
            }

            string directory = args[0];
            string extension = Path.GetExtension(directory);
            int fileCount = 1;
            switch (extension) {
                case "":
                    CreateBackup(directory);
                    WriteLine("Opening given folder.\n");
                    fileCount = CleanDirectory(directory);
                    break;
                case ".tas":
                    CreateBackup(directory);
                    CleanTasFile(directory);
                    break;
                default:
                    PromptEnd("Invalid arguments.");
                    return;
            }

            PrintTotals(fileCount);
            PromptEnd();
        }

        #region FeedbackAndBackups

        static void PromptEnd(string msg = null)
        {
            if (msg != null) {
                ForegroundColor = White;
                WriteLine(msg);
            }
            ForegroundColor = Gray;
            WriteLine("\nPress enter to close.");
            ReadLine();
        }

        static void CreateBackup(string dir)
        {
            Write("Backup files just in case? It will be placed in the same directory as this target file.\ny/n: ");
            while (true) {
                var rawInput = ReadLine();
                var input = rawInput.Trim().ToLower();
                if (input == "") continue;
                if (input[0] == 'y') {
                    var m = Regex.Match(dir, @"^(.+?)(\.tas)?$");
                    bool isTasFile = m.Groups[2].Success;
                    var target = m.Result("$1 - BACKUP$2");

                    if (isTasFile)
                        File.Copy(dir, target);
                    else {
                        if (Directory.Exists(target))
                            Directory.Delete(target);
                        CopyFolderContents(new DirectoryInfo(dir), Directory.CreateDirectory(target));
                    }
                    break;
                }
                else if (input[0] == 'n')
                    break;
                else {
                    SetCursorPosition(5, 1);
                    Write(new string(' ', rawInput.Length));
                    SetCursorPosition(5, 1);
                }
            }

            void CopyFolderContents(DirectoryInfo source, DirectoryInfo target)
            {
                foreach (var file in source.GetFiles())
                    file.CopyTo($"{target.FullName}\\{file.Name}");
                foreach (var dir in source.GetDirectories())
                    CopyFolderContents(new DirectoryInfo(dir.FullName), target.CreateSubdirectory(dir.Name));
            }
        }

        static void PrintTotals(int fileCount)
        {
            ForegroundColor = Yellow;
            WriteLine("Totals:");

            ForegroundColor = White;
            Write($"Removed spaces: " + totalCharRemoveCount);
            WriteLine(fileCount == 1 ? "" : $" in {totalBlockRemoveCount} {(totalBlockRemoveCount == 1 ? "block" : "blocks")}");

            if (extraLineBlockCount != 0) {
                Write("Removed excessive end lines: " + extraLineRemoveCount);
                WriteLine(fileCount == 1 ? "" : $" from {extraLineBlockCount} {(extraLineBlockCount == 1 ? "file" : "files")}");
            }
        }

        #endregion

        #region FileCleaning

        static Regex getSpaces = new Regex(@"[ \t]+(?=$|[\r\n])");
        static Regex getExtraEndLines = new Regex(@"(\r\n){2,}$");

        static int CleanDirectory(string dir)
        {
            var files = Directory.GetFiles(dir, "*.tas", SearchOption.AllDirectories);
            foreach (string tasFile in files)
                CleanTasFile(tasFile);
            return files.Length;
        }

        static void CleanTasFile(string dir)
        {
            bool foundAnything = false;

            WriteLine("Checking " + Regex.Match(dir, @"(?<=\\|^)[^\\]+$").Value);
            string text = File.ReadAllText(dir);

            ForegroundColor = Green;

            int blockCount = 0;
            int charCount = 0;
            text = getSpaces.Replace(text, m => {
                blockCount++;
                charCount += m.Length;
                foundAnything = true;
                return "";
            });
            if (charCount != 0)
                WriteLine($"Removed {charCount} unnecessary " + (charCount == 1 ? "space." : $"spaces in {blockCount} {(blockCount == 1 ? "block" : "blocks")}."));
            totalBlockRemoveCount += blockCount;
            totalCharRemoveCount += charCount;

            int extraLineCount = 0;
            text = getExtraEndLines.Replace(text, m => {
                extraLineBlockCount++;
                extraLineCount = m.Length / 2 - 1;
                foundAnything = true;
                return "\r\n";
            });
            extraLineRemoveCount += extraLineCount;
            if (extraLineCount != 0)
                WriteLine($"Removed {extraLineCount} excessive extra {(extraLineCount == 1 ? "line" : "lines")} at end of file");

            ForegroundColor = Gray;
            if (foundAnything)
                File.WriteAllText(dir, text);

            WriteLine();
        }

        #endregion
    }
}
