using OnedataDrive;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace TestProject.Utils
{
    [TestClass]
    public class PathUtilsTest
    {
        const string PATH1 = "C:\\Users\\User\\win-client\\syncRoot\\space\\FILE.txt";
        const string PATH2 = "C:\\Users\\User\\win-client\\syncRoot\\space\\FILE.txt\\";
        const string PATH3 = "C:\\Users\\User\\win-client\\syncRoot\\";
        const string PATH4 = "C:\\";
        const string PATH5 = "C:\\Users\\User\\win-client\\syncRoot";
        const string PATH6 = "";
        const string ROOT_PATH = "C:\\Users\\User\\win-client\\syncRoot";

        Config conf;

        public PathUtilsTest()
        {
            conf = new Config();
            conf.Init("host", ROOT_PATH, "token");
            CloudSync.configuration = conf;
        }

        [TestMethod]
        public void GetSpaceName_Test()
        {
            Assert.AreEqual(
                "space",
                PathUtils.GetSpaceName(PATH1),
                "Path without \\ at the end");

            Assert.AreEqual(
                "space",
                PathUtils.GetSpaceName(PATH2),
                "Path with \\ at the end");
        }

        [TestMethod]
        public void GetParentPath()
        {
            Assert.AreEqual(
                "C:\\Users\\User\\win-client\\syncRoot\\space\\",
                PathUtils.GetParentPath(PATH1),
                "Path without \\ at the end");

            Assert.AreEqual(
                "C:\\Users\\User\\win-client\\syncRoot\\space\\",
                PathUtils.GetParentPath(PATH2),
                "Path with \\ at the end");

            Assert.AreEqual(
                "",
                PathUtils.GetParentPath(PATH4),
                "Path with only one element");
        }

        [TestMethod]
        public void GetLastInPath()
        {
            Assert.AreEqual(
                "FILE.txt",
                PathUtils.GetLastInPath(PATH1),
                "Path without \\ at the end");

            Assert.AreEqual(
                "FILE.txt",
                PathUtils.GetLastInPath(PATH2),
                "Path with \\ at the end");
        }

        [TestMethod]
        public void GetPathFromSpace()
        {
            Assert.AreEqual(
                "space\\FILE.txt\\",
                PathUtils.GetPathFromSpace(PATH1),
                "Path without \\ at the end");

            Assert.AreEqual(
                "space\\FILE.txt\\",
                PathUtils.GetPathFromSpace(PATH2),
                "Path with \\ at the end");

            Assert.AreEqual(
                "",
                PathUtils.GetPathFromSpace(PATH3),
                "Path with \\ at the end, contains no space");
        }

        [TestMethod]
        public void GetFullPath_Test()
        {
            CF_CALLBACK_INFO callbackInfo1 = new CF_CALLBACK_INFO
            {
                VolumeDosName = "C:",
                NormalizedPath = "\\space\\FILE.txt"
            };

            CF_CALLBACK_INFO callbackInfo2 = new CF_CALLBACK_INFO
            {
                VolumeDosName = "",
                NormalizedPath = "\\space\\FILE.txt"
            };

            CF_CALLBACK_INFO callbackInfo3 = new CF_CALLBACK_INFO
            {
                VolumeDosName = "C:",
                NormalizedPath = "\\space\\FILE.txt\\"
            };

            Assert.AreEqual(
                "C:\\space\\FILE.txt\\",
                PathUtils.GetFullPath(callbackInfo1),
                "Correct path");

            Assert.ThrowsException<ArgumentException> (
                () => PathUtils.GetFullPath(callbackInfo2),
                "Incorrect input argument");

            Assert.AreEqual(
                "C:\\space\\FILE.txt\\",
                PathUtils.GetFullPath(callbackInfo3),
                "Normalized path with \\ at the end");
        }

        [TestMethod]
        public void IsRootPath()
        {
            Assert.AreEqual(
                true,
                PathUtils.IsRootPath(PATH3),
                "Correct path");

            Assert.AreEqual(
                true,
                PathUtils.IsRootPath(PATH5),
                "Path without \\ at the end");

            Assert.AreEqual(
                false,
                PathUtils.IsRootPath(PATH1),
                "Path to space");

            Assert.AreEqual(
                false,
                PathUtils.IsRootPath(PATH4),
                "Just drive path");

            Assert.AreEqual(
                false,
                PathUtils.IsRootPath(PATH6),
                "Empty path");
        }
    }
}

