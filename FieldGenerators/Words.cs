using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Roentgenium.FieldGenerators
{
    static class Words
    {
        private readonly static string ResourceId = "Resource.FieldGenerators.Words.cs";
        private readonly static string DictFile = "aspell6_en_2018_04_16_0_en_wo_accents";
        private readonly static Regex SafeCharsRegex = new Regex("[^a-zA-Z0-9]");
        private static List<string> _words = new List<string>();

        static Words()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceId))
            using (var rr = new ResourceReader(stream))
            {
                var rre = rr.GetEnumerator();

                while (rre.MoveNext())
                {
                    if ((string)rre.Key == DictFile)
                    {
                        // https://github.com/microsoft/msbuild/issues/2221
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            _words = new List<string>();
                            var fPathInter = (rre.Value as string).Split(';')[0].Split('\\');
                            if (fPathInter[0] != ".." || fPathInter[1] != "resources")
                                throw new InvalidDataException();
                            var fPath = Path.Combine("Resources", fPathInter[fPathInter.Length - 1]);
                            using (var fStream = new FileStream(fPath, FileMode.Open))
                            using (var sr = new StreamReader(fStream))
                            {
                                string nextLine = null;
                                while ((nextLine = sr.ReadLine()) != null)
                                    _words.Add(nextLine);
                            }
                        }
                        else
                            _words = new List<string>((rre.Value as string).Split(new char[] { '\n' }));
                    }
                }
            }
        }

        public static string RandomWord()
        {
            return _words[GeneratorsStatic.Random.Next(_words.Count)].TrimEnd();
        }

        public static string RandomSafeCharsWord()
        {
            return SafeCharsRegex.Replace(RandomWord(), "");
        }

        public static string RandomSafeCharsWordsOfLength(int minLength)
        {
            var bStr = "";
            while (bStr.Length < minLength)
                bStr += RandomSafeCharsWord() + " ";
            return bStr;
        }
    }
}
