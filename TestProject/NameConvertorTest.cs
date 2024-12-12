using OnedataDrive.CloudSync.Utils;
using System.Text;

namespace TestProject
{
    [TestClass]
    public class NameConvertorTest
    {
        [TestMethod]
        public void WindowsCorrect_Correct()
        {
            NameConvertor nameConvertor = new();

            List<string> names = new() {
                "abcd", 
                "123456789", 
                "Abcd Efgh", 
                "'Abcd_.'#@$%^&!(123)" };

            var msg = new Func<string, string>(name => 
                { return name + " is correct Windows file name"; });

            foreach (string name in names)
            {
                Assert.IsTrue(nameConvertor.WindowsCorrect(name), msg(name));
            }
        }

        [TestMethod]
        public void WindowsCorrect_Incorrect()
        {
            NameConvertor nameConvertor = new();

            List<(string name, string description)> values = new() {
                ("", "Empty name"),
                ("123456789 ", "Space at the end"),
                ("Abcd>Efgh", "One invalid character"),
                ("<>:\"/\\|?*", "No valid characters"),
                ("abc" + (char) 5 + "def", "Contains unprintable character") };

            var msg = new Func<string, string, string>((name, optional) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(name + " is NOT correct Windows file name");
                if (optional.Length != 0)
                    sb.Append(" - " + optional);
                return sb.ToString();
            });

            foreach (var value in values)
            {
                Assert.IsFalse(nameConvertor.WindowsCorrect(value.name), 
                    msg(value.name, value.description));
            }
        }

        [TestMethod]
        public void MakeWindowsCorrect_DefaultReplaceChar()
        {
            NameConvertor nameConvertor = new();

            List<(string input, string description, string expected)> values = new() {
                ("", "Empty name", "_"),
                ("123456789 ", "Space at the end", "123456789"),
                ("Abcd>Efgh", "One invalid character", "Abcd_Efgh"),
                ("<>:\"/\\|?*", "No valid characters", "_________"),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc_def")
            };

            foreach (var value in values)
            {
                Assert.AreEqual(
                    value.expected,
                    nameConvertor.MakeWindowsCorrect(value.input),
                    " -> " + value.description
                    );
            }
        }

        [TestMethod]
        public void MakeWindowsCorrect_NewReplaceChar()
        {
            const char REPLACE_CHAR = '#';
            NameConvertor nameConvertor = new();

            List<(string input, string description, string expected)> values = new() {
                ("", "Empty name", "#"),
                ("123456789 ", "Space at the end", "123456789"),
                ("Abcd>Efgh", "One invalid character", "Abcd#Efgh"),
                ("<>:\"/\\|?*", "No valid characters", "#########"),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc#def")
            };

            foreach (var value in values)
            {
                Assert.AreEqual(
                    value.expected,
                    nameConvertor.MakeWindowsCorrect(value.input, REPLACE_CHAR),
                    " -> " + value.description
                    );
            }
        }

        [TestMethod]
        public void MakeWindowsCorrect_AppendFileId()
        {
            NameConvertor nameConvertor = new();
            string fileId = "000000000052F57F67756964233939393438626238356161646331373166646138393261363461646637373132636839366364233534663561353262343730323434323239353333643033343934643963343732636839366364";
            string fileIdMinimalLength = "000000000000000000000000000000000000";
            string fileIdShort =         "00000000000000000000000000000000000";
            string fileIdEmpty = "";

            List<(string input, string description, string expected, bool expectedNameWasCorrect, string fileId)> values = new() {
                ("", "Empty name", "_@3393939343", false, fileId),
                ("123456789 ", "Space at the end", "123456789@3393939343", false, fileId),
                ("Abcd>Efgh", "One invalid character", "Abcd_Efgh@3393939343", false, fileId),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc_def@3393939343", false, fileId),
                ("correctName", "Correct name", "correctName", true, fileId),
                ("correctName", "Correct name, short fileId", "correctName", true, fileIdShort),
                ("Abcd>Efgh", "Incorrect name, short fileId", "Abcd_Efgh@", false, fileIdShort),
                ("Abcd>Efgh", "Incorrect name, short fileId - empty", "Abcd_Efgh@", false, fileIdEmpty),
                ("Abcd>Efgh", "Incorrect name, short fileId - empty", "Abcd_Efgh@0000000000", false, fileIdMinimalLength)
            };

            foreach (var value in values)
            {
                string output;
                bool nameWasCorrect = nameConvertor.MakeWindowsCorrect(value.input, out output, value.fileId);

                Assert.AreEqual(
                    value.expected,
                    output,
                    " -> " + value.description
                    );
                Assert.AreEqual(
                    value.expectedNameWasCorrect,
                    nameWasCorrect,
                    " -> " + value.description);
            }
        }
    }
}