using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Filesplitter
{
    class Program
    {
        static int Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "-f",
                    description: "Name of the file to process"),

                new Option<int>(
                    "-d",
                    getDefaultValue: () => 42,
                    description: "An option whose argument is parsed as an int"),
                new Option<bool>(
                    "--bool-option",
                    "An option whose argument is parsed as a bool"),
                new Option<FileInfo>(
                    "--file-option",
                    "An option whose argument is parsed as a FileInfo")
            };

            rootCommand.Description = "My sample app";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<int, bool, FileInfo>((intOption, boolOption, fileOption) =>
            {
                Console.WriteLine($"The value for --int-option is: {intOption}");
                Console.WriteLine($"The value for --bool-option is: {boolOption}");
                Console.WriteLine($"The value for --file-option is: {fileOption?.FullName ?? "null"}");
            });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
//            Console.WriteLine("Hello World!");
        }
        
        static void Main2(string[] args)
        {
            var command = "j"; //args[0];
            
            if (!command.Equals("s") || !command.Equals("j"))
                Console.WriteLine("Only valid commands are s (split) or j (join)");
            
            var fileName = "/Users/claesradstrom/RiderProjects/FileMgr/FileMgr/test.b64";
            //var fileName = args[1];

            var chunkSize = 15000;
                
            if (command.Equals("s"))
            {
                //chunkSize = Convert.ToInt32(args[2]) ;
                SplitFile(fileName, chunkSize);
            }
            
            if (command.Equals("j"))
            {
                JoinFiles(fileName);
            }
            
            Console.WriteLine("Done");
        }
        
        private static void SplitFile(string fileName, int chunkSize)
        {
            var fileContents = File.ReadAllBytes(fileName);
            var filepartNr = 1;

            for (var i = 0; i < fileContents.Length; i += chunkSize)
            {
                var data = fileContents.Skip(i).Take(chunkSize).ToArray();
                var newFilename = $"{fileName}.{filepartNr.ToString()}"; 
                File.WriteAllBytes(newFilename, data);

                filepartNr++;
            }
        }
        
        private static void JoinFiles(string fileName)
        {
            byte[] resultArray = new byte[0];
            
            for (var i = 1; i < 1000; i++)
            {
                var fragmentFileName = $"{fileName}.{i.ToString()}";
                
                if (!File.Exists(fragmentFileName))
                    break;
                
                var fileContents = File.ReadAllBytes(fragmentFileName);
                
                resultArray = resultArray.Concat(fileContents).ToArray();
            }
            
            File.WriteAllBytes(fileName, resultArray);
        }
    }
}
