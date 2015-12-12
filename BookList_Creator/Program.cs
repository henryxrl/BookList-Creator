using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BookList_Creator
{
    public class Program
    {
        #region Variables

        private static Boolean DESCENDING = true;
        private static Boolean SHOWSIZE = false;

        private static String BOOKLISTFILE = @"E:\Books\TXT\小说\精校目录.txt";
        private static String DIRECTORY = @"E:\Books\TXT\小说\精校";

        private static String REGEX = @"《.*》.*作者：.*";
        private static Regex REG = new Regex(REGEX);

        private static List<Tuple<String, Int64>> BOOKS = new List<Tuple<String, Int64>>();

        #endregion

        public static void Main(String[] args)
        {
            switch (args.Length)
            {
                case 0:
                    break;
                case 1:
                    ProcessArg(args[0]);
                    break;
                case 2:
                    ProcessArgs(args[0], args[1]);
                    break;
                default:
                    ConsolePrintError();
                    break;
            }

            GetAllBookInfo();
            PrintBooks();
        }

        #region BookList Creator Methods

        private static void GetAllBookInfo()
        {
            GetAllBookInfoHelper(DIRECTORY);

            // Sort BOOKS
            if (DESCENDING)
            {
                BOOKS.Sort((x, y) =>
                {
                    int result = y.Item2.CompareTo(x.Item2);
                    return result == 0 ? x.Item1.CompareTo(y.Item1) : result;
                });
            }
            else
            {
                BOOKS.Sort((x, y) =>
                {
                    int result = x.Item2.CompareTo(y.Item2);
                    return result == 0 ? x.Item1.CompareTo(y.Item1) : result;
                });
            }
        }

        private static void GetAllBookInfoHelper(String directory)
        {
            // Get all matched files
            var files = Directory.GetFiles(directory, "*.*").Where(path => REG.IsMatch(path));
            foreach (String file in files)
            {
                FileInfo f = new FileInfo(file);
                BOOKS.Add(new Tuple<String, Int64>(Path.GetFileNameWithoutExtension(file), f.Length));
            }

            // Get all matched directories
            var dirs = Directory.GetDirectories(directory, "*.*").Where(path => REG.IsMatch(path));
            foreach (String dir in dirs)
            {
                DirectoryInfo d = new DirectoryInfo(dir);
                BOOKS.Add(new Tuple<String, Int64>(d.Name, GetDirectorySize(ref d)));
            }

            // Get all un-matched directories
            var other_dirs = Directory.GetDirectories(directory, "*.*").Where(path => !REG.IsMatch(path));
            if (other_dirs != null && other_dirs.Any())
            {
                foreach (String dir in other_dirs)
                {
                    GetAllBookInfoHelper(dir);
                }
            }
        }

        private static void PrintBooks()
        {
            using (StreamWriter writer = new StreamWriter(BOOKLISTFILE))
            {
                Int64 total_size = 0;
                Int32 total_books = BOOKS.Count;
                foreach (var tuple in BOOKS)
                {
                    if (!SHOWSIZE)
                    {
                        writer.WriteLine(String.Format("{0}", tuple.Item1));
                    }
                    else
                    {
                        writer.WriteLine(String.Format("{0}\t{1}", StrFormatByteSize(tuple.Item2), tuple.Item1));
                    }
                    total_size += tuple.Item2;
                }

                Int64 half_size_accumulator = 0;
                Int64 half_size = total_size / 2;
                Tuple<String, Int64> median_book = new Tuple<String, Int64>("", 0);
                Int32 median_book_index = -1;
                for (Int32 i = 0; i < total_books; i++)
                {
                    half_size_accumulator += BOOKS[i].Item2;

                    if (half_size_accumulator == half_size)
                    {
                        median_book = BOOKS[i];
                        median_book_index = i;
                        break;
                    }

                    if (half_size_accumulator > half_size)
                    {
                        median_book = BOOKS[i - 1];
                        median_book_index = i - 1;
                        break;
                    }
                }

                writer.WriteLine("\n\n");
                writer.WriteLine(String.Format("书籍数量： {0}", total_books));
                writer.WriteLine(String.Format("书籍总大小： {0}", StrFormatByteSize(total_size)));
                writer.WriteLine(String.Format("书籍平均大小： {0}", StrFormatByteSize(total_size / total_books)));
                writer.WriteLine();
                writer.WriteLine("中位书籍名称： {0}", median_book.Item1);
                writer.WriteLine("中位书籍大小： {0}", StrFormatByteSize(median_book.Item2));
                writer.WriteLine("中位书籍排名： {0}", (median_book_index + 1));
            }
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern Int64 StrFormatByteSize(Int64 fileSize, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer, Int32 bufferSize);
        private static String StrFormatByteSize(Int64 filesize)
        {
            StringBuilder sb = new StringBuilder(11);
            StrFormatByteSize(filesize, sb, sb.Capacity);
            return sb.ToString();
        }

        private static Int64 GetDirectorySize(ref DirectoryInfo d)
        {
            return d.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        #endregion

        #region Command Line Methods

        private static void ProcessArg(String arg)
        {
            if (arg[0] == '/' && arg.Length == 2)
            {
                switch (Char.ToLower(arg[1]))
                {
                    case 'a':
                        DESCENDING = false;
                        break;
                    case 'd':
                        break;
                    case 's':
                        SHOWSIZE = true;
                        break;
                    case '?':
                        ConsolePrintHelp();
                        break;
                    default:
                        ConsolePrintError();
                        break;
                }
            }
            else
            {
                ConsolePrintError();
            }
        }

        private static void ProcessArgs(String arg1, String arg2)
        {
            if (arg1[0] == '/' && arg2[0] == '/' && arg1.Length == 2 && arg2.Length == 2)
            {
                Char a1 = Char.ToLower(arg1[1]);
                Char a2 = Char.ToLower(arg2[1]);

                if ((a1 == 'a' && a2 == 's') || (a1 == 's' && a2 == 'a'))
                {
                    DESCENDING = false;
                    SHOWSIZE = true;
                }
                else if ((a1 == 'd' && a2 == 's') || (a1 == 's' && a2 == 'd'))
                {
                    DESCENDING = true;
                    SHOWSIZE = true;
                }
                else
                {
                    ConsolePrintError();
                }
            }
            else
            {
                ConsolePrintError();
            }
        }

        private static void ConsolePrint(Char msg)
        {
            Console.WriteLine(msg);
        }

        private static void ConsolePrint(String msg)
        {
            Console.WriteLine(msg);
        }

        private static void ConsolePrintError()
        {
            Console.WriteLine("The syntax of the command is incorrect.");
        }

        private static void ConsolePrintHelp()
        {
            ConsolePrint(String.Format("Create a book list at \"{0}\" for \"{1}\".\n", BOOKLISTFILE, DIRECTORY));
            ConsolePrint("BOOKLIST_CREATOR [/A | /D] [/S]\n");
            ConsolePrint("    /A      Output all books in ascending order with respect to file size.");
            ConsolePrint("    /D      Output all books in descending order with respect to file size.\n");
            ConsolePrint("    /S      Output all books and their file sizes.");
        }

        #endregion

    }
}
