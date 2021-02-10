using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Filesplitter
{
    class Program
    {
        private const string b64 = "b64.";
        private const string FileFormat = "000";

        [Verb("split", HelpText = "Split file into parts.")]
        class SplitOptions
        {
            [Option('c', "chunk", Required = false, Default = 20,  HelpText = "Size of splitted files in kb.")]
            public int ChunkSize { get; set; }
            
            [Option('b', "base64", Required = false, Default = false,  HelpText = "Base64 encode splitted files.")]
            public bool Base64Encode { get; set; }

            [Option('d', "dryrun", Required = false, Default = false, HelpText = "Perform a dryrun without creating any real files.")]
            public bool Dryrun { get; set; }

            [Option('f', "file", Required = true, HelpText = "File to split.")]
            public string Filename { get; set; }
        }

        [Verb("join", HelpText = "Join file parts to one file (max 100 parts).")]
        class JoinOptions
        {
            [Option('f', "file", Required = true, HelpText = "First file part in series.")]
            public string Filename { get; set; }

            [Option('o', "outfile", Required = false, HelpText = "Override filename of joined file.")]
            public string OutFilename { get; set; }

            [Option('b', "base64Decode", Required = false, Default = false, HelpText = "Base64 decode joined file.")]
            public bool Base64Decode { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<SplitOptions, JoinOptions>(args)
               .MapResult(
                 (SplitOptions opts) => RunAddAndReturnExitCode(opts),
                 (JoinOptions opts) => RunCommitAndReturnExitCode(opts),
                 errs => 1);
        }

        private static int RunAddAndReturnExitCode(SplitOptions opts)
        {
            return SplitFile(opts.Filename, opts.ChunkSize, opts.Base64Encode, opts.Dryrun);
        }
        
        private static int  RunCommitAndReturnExitCode(JoinOptions opts)
        {
            return JoinFiles(opts.Filename, opts.OutFilename, opts.Base64Decode);
        }

        private static void VerifyFilename(string fileName)
        {
            var currentPath = Directory.GetCurrentDirectory();

            if (!File.Exists(Path.Combine(currentPath, fileName)))
            {
                Console.WriteLine("Missing file: " + fileName);
                Environment.Exit(-1);
            }
        }

        private static int SplitFile(string fileName, int chunkSize, bool base64Encode, bool dryRun)
        {
            Console.WriteLine($"Splitting file {fileName} into chunks of {chunkSize} kb");

            VerifyFilename(fileName);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            var fileContents = File.ReadAllBytes(filePath);

            var encodedContents = fileContents;
            var addedExtension = string.Empty;

            if (base64Encode)   
            {
                var base64String = Convert.ToBase64String(fileContents);
                encodedContents = Encoding.ASCII.GetBytes(base64String);
                addedExtension = b64;
            }

            chunkSize = chunkSize * 1024;

            var filepartNr = 1;
            for (var i = 0; i < encodedContents.Length; i += chunkSize)
            {
                var data = encodedContents.Skip(i).Take(chunkSize).ToArray();
                var newFilename = $"{fileName}.{addedExtension}{filepartNr.ToString(FileFormat)}";
                var newFilepath = Path.Combine(Directory.GetCurrentDirectory(), newFilename);

                if (!dryRun) File.WriteAllBytes(newFilepath, data);
                if (dryRun) Console.WriteLine($"Dry wrote file: {newFilename}");

                filepartNr++;
            }

            return 0;
        }

        private static int JoinFiles(string fileName, string outFilename, bool base64Decode)
        {
            VerifyFilename(fileName);

            fileName = fileName.Replace(".001", "");

            byte[] resultArray = new byte[0];
            
            for (var i = 1; i < 100; i++)
            {
                var fragmentFileName = $"{fileName}.{i.ToString(FileFormat)}";
                
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fragmentFileName)))
                    break;
                
                var fileContents = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fragmentFileName));
                
                resultArray = resultArray.Concat(fileContents).ToArray();
            }

            if (string.IsNullOrEmpty(outFilename))
                outFilename = fileName.Replace(".b64", "");

            if (base64Decode)
            { 
                var base64EncodedData = Encoding.ASCII.GetString(resultArray);
                var orgByteData = Convert.FromBase64String(base64EncodedData);
                resultArray = orgByteData;
            }

            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), outFilename), resultArray);

            return 0;
        }
    }
}
