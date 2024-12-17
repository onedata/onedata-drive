using OnedataDrive;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Utils
{
    [TestClass]
    public class PathUtilsTest
    {
        const string PATH1 = "C:\\Users\\User\\win-client\\syncRoot\\space\\FILE.txt";
        const string PATH2 = "C:\\Users\\User\\win-client\\syncRoot\\space\\FILE.txt\\";
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
    }
}

