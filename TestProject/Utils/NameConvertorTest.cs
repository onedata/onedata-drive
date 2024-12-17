using OnedataDrive.Utils;
using System.Text;

namespace TestProject.Utils
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
                "'Abcd_.'#@$%^&!(123)",
                "AUXa"
            };

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
                ("abc" + (char) 5 + "def", "Contains unprintable character"),
                ("AUX", "Invalid name"),
                ("AUX.txt@1234567890", "Invalid name"),
            };

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

            const string FILE_ID = "000000000052F57F67756964233939393438626238356161646331373166646138393261363461646637373132636839366364233534663561353262343730323434323239353333643033343934643963343732636839366364";
            List<(string input, string description, string expected)> values = new() {
                ("", "Empty name", "_@3393939343"),
                ("123456789 ", "Space at the end", "123456789@3393939343"),
                ("Abcd>Efgh", "One invalid character", "Abcd_Efgh@3393939343"),
                ("<>:\"/\\|?*", "No valid characters", "_________@3393939343"),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc_def@3393939343"),
                ("AUX", "Invalid Windows file name", "AUX@3393939343"),
                ("AUX.txt@1234567890", "Invalid Windows file name", "_AUX.txt@1234567890@3393939343")
            };

            foreach (var value in values)
            {
                Assert.AreEqual(
                    value.expected,
                    nameConvertor.MakeWindowsCorrect(value.input, out bool _, FILE_ID),
                    " -> " + value.description
                    );
            }
        }

        [TestMethod]
        public void MakeWindowsCorrect_NewReplaceChar()
        {
            NameConvertor nameConvertor = new();
            const string FILE_ID = "000000000052F57F67756964233939393438626238356161646331373166646138393261363461646637373132636839366364233534663561353262343730323434323239353333643033343934643963343732636839366364";
            const char REPLACE_CHAR = '#';

            List<(string input, string description, string expected)> values = new() {
                ("", "Empty name", "#@3393939343"),
                ("123456789 ", "Space at the end", "123456789@3393939343"),
                ("Abcd>Efgh", "One invalid character", "Abcd#Efgh@3393939343"),
                ("<>:\"/\\|?*", "No valid characters", "#########@3393939343"),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc#def@3393939343"),
                ("AUX", "Invalid Windows file name", "AUX@3393939343"),
                ("AUX.txt@1234567890", "Invalid Windows file name", "#AUX.txt@1234567890@3393939343")
            };

            foreach (var value in values)
            {
                Assert.AreEqual(
                    value.expected,
                    nameConvertor.MakeWindowsCorrect(value.input, out _, FILE_ID, REPLACE_CHAR),
                    " -> " + value.description
                    );
            }
        }

        [TestMethod]
        public void MakeWindowsCorrect_AppendFileId()
        {
            NameConvertor nameConvertor = new();
            const string FILE_ID = "000000000052F57F67756964233939393438626238356161646331373166646138393261363461646637373132636839366364233534663561353262343730323434323239353333643033343934643963343732636839366364";
            const string FILE_ID_MINIMAL_LEN = "000000000000000000000000000000000000";
            const string FILE_ID_SHORT =         "00000000000000000000000000000000000";
            const string FILE_ID_EMPTY = "";

            List<(string input, string description, string expected, bool expected_nameWasModified, string fileId)> values = new() {
                ("", "Empty name", "_@3393939343", true, FILE_ID),
                ("123456789 ", "Space at the end", "123456789@3393939343", true, FILE_ID),
                ("Abcd>Efgh", "One invalid character", "Abcd_Efgh@3393939343", true, FILE_ID),
                ("abc" + (char) 5 + "def", "Contains unprintable character", "abc_def@3393939343", true, FILE_ID),
                ("correctName", "Correct name", "correctName", false, FILE_ID),
                ("correctName", "Correct name, short FILE_ID", "correctName", false, FILE_ID_SHORT),
                ("correctName", "Correct name, FILE_ID empty", "correctName", false, FILE_ID_EMPTY),
                ("Abcd>Efgh", "Incorrect name, short FILE_ID", "Abcd_Efgh@", true, FILE_ID_SHORT),
                ("Abcd>Efgh", "Incorrect name, FILE_ID empty", "Abcd_Efgh@", true, FILE_ID_EMPTY),
                ("Abcd>Efgh", "Incorrect name, short FILE_ID", "Abcd_Efgh@0000000000", true, FILE_ID_MINIMAL_LEN),
                ("AUX", "Invalid Windows file name", "AUX@3393939343", true, FILE_ID),
                ("AUX.txt@1234567890", "Invalid Windows file name", "_AUX.txt@1234567890@3393939343", true, FILE_ID)
            };

            foreach (var value in values)
            {
                string output = nameConvertor.MakeWindowsCorrect(value.input, out bool nameWasCorrect, value.fileId);

                Assert.AreEqual(
                    value.expected,
                    output,
                    " -> " + value.description
                    );
                Assert.AreEqual(
                    value.expected_nameWasModified,
                    nameWasCorrect,
                    " -> " + value.description);
            }
        }
    }
}