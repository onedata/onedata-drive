using OnedataDrive;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TestProject
{
    [TestClass]
    public class FileIdentityTest
    {
        // [fileType:4][stringLength:4][UTF-8 string data:N]
        const uint HEADER_SIZE = (uint)(sizeof(int) + sizeof(int));

        #region helpers

        private static FileIdentity RoundTrip(FileIdentity identity)
        {
            using UnmanagedMem unmanagedMem = identity.ToUnmanagedMemory();
            return FileIdentity.FromUnmanagedMemory(unmanagedMem);
        }

        private static void AssertIdentity(FileIdentity expected, FileIdentity actual, string message)
        {
            Assert.AreEqual(expected.fileType, actual.fileType, message + " - fileType");
            Assert.AreEqual(expected.fileID, actual.fileID, message + " - fileID");
            Assert.AreEqual(expected.symlinkTargetId, actual.symlinkTargetId, message + " - symlinkTarget");
        }

        private static byte[] ReadAll(UnmanagedMem unmanagedMem)
        {
            byte[] bytes = new byte[unmanagedMem.GetSize()];
            Marshal.Copy(unmanagedMem.GetPointer(), bytes, 0, bytes.Length);
            return bytes;
        }

        #endregion

        #region constructors

        [TestMethod]
        public void DefaultConstructor_IsEmpty()
        {
            FileIdentity fileIdentity = new();

            Assert.AreEqual("", fileIdentity.fileID, "Default fileID is empty string");
            Assert.AreEqual(FileTypeOD.EMPTY, fileIdentity.fileType, "Default fileType is EMPTY");
        }

        [TestMethod]
        public void Constructor_SetsFields()
        {
            FileIdentity fileIdentity = new("1234567890abcdef", FileTypeOD.SYMLNK);

            Assert.AreEqual("1234567890abcdef", fileIdentity.fileID, "fileID is stored");
            Assert.AreEqual(FileTypeOD.SYMLNK, fileIdentity.fileType, "fileType is stored");
        }

        #endregion

        #region ToUnmanagedMemory - layout

        [TestMethod]
        public void ToUnmanagedMemory_Size_IsHeaderPlusUtf8Bytes()
        {
            List<(string fileID, string description)> values = new() {
                ("", "Empty string"),
                ("a", "One ASCII character - 1 byte"),
                ("1234567890abcdef", "ASCII string - 16 bytes"),
                ("ěščřžýáíé", "UTF-8 multi byte characters"),
                ("ファイル", "Japanese - 3 bytes per character"),
                ("🙂🙃", "Emoji - 4 bytes per character (surrogate pair)"),
                ("a\0b", "Embedded null character"),
            };

            foreach (var value in values)
            {
                uint expectedSize = HEADER_SIZE + (uint)Encoding.UTF8.GetByteCount(value.fileID);

                using UnmanagedMem unmanagedMem = new FileIdentity(value.fileID, FileTypeOD.REG)
                    .ToUnmanagedMemory();

                Assert.AreEqual(expectedSize, unmanagedMem.GetSize(),
                    "Allocated size for " + value.description);
            }
        }

        [TestMethod]
        public void ToUnmanagedMemory_Size_IsNotCharCount()
        {
            // 6 characters, 12 UTF-8 bytes - size has to follow the byte count
            const string fileID = "ěščřžý";

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.REG).ToUnmanagedMemory();

            Assert.AreEqual(HEADER_SIZE + 12, unmanagedMem.GetSize(),
                "Length prefix counts UTF-8 bytes, not UTF-16 chars");
        }

        [TestMethod]
        public void ToUnmanagedMemory_Size_EmptyIdIsHeaderOnly()
        {
            using UnmanagedMem unmanagedMem = new FileIdentity().ToUnmanagedMemory();

            Assert.AreEqual(HEADER_SIZE, unmanagedMem.GetSize(),
                "Empty fileID allocates only the header");
        }

        [TestMethod]
        public void ToUnmanagedMemory_Layout_MatchesDocumentedBytes()
        {
            // fileType = REG (1), length = 3, data = "abc"
            using UnmanagedMem unmanagedMem = new FileIdentity("abc", FileTypeOD.REG).ToUnmanagedMemory();

            Assert.AreEqual("0100000003000000616263",
                Convert.ToHexString(ReadAll(unmanagedMem)),
                "Little endian fileType, little endian length, then raw UTF-8 bytes");
        }

        [TestMethod]
        public void ToUnmanagedMemory_LengthPrefix_IsUtf8ByteCount()
        {
            const string fileID = "ūrling🙃";
            int byteCount = Encoding.UTF8.GetByteCount(fileID);

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.REG).ToUnmanagedMemory();
            byte[] bytes = ReadAll(unmanagedMem);

            // The header is the only thing the pointer is read through, so a wrong
            // prefix here is what would silently truncate the ID on the way back.
            Assert.AreEqual(byteCount, BitConverter.ToInt32(bytes, sizeof(int)),
                "Length prefix at offset 4 equals UTF-8 byte count");
            Assert.AreEqual(fileID, Encoding.UTF8.GetString(bytes, (int)HEADER_SIZE, byteCount),
                "String data written directly after the header");
        }

        [TestMethod]
        public void ToUnmanagedMemory_NoTrailingBytes()
        {
            // AllocCoTaskMem does not zero memory, so every byte inside the reported
            // size has to be explained by the layout - no terminator, no uninitialised gap.
            const string fileID = "some/long/file/identity";

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.DIR).ToUnmanagedMemory();
            byte[] bytes = ReadAll(unmanagedMem);

            int lengthPrefix = BitConverter.ToInt32(bytes, sizeof(int));

            Assert.AreEqual((int)FileTypeOD.DIR, BitConverter.ToInt32(bytes, 0), "fileType written");
            Assert.AreEqual(Encoding.UTF8.GetByteCount(fileID), lengthPrefix, "length prefix written");
            Assert.AreEqual((int)HEADER_SIZE + lengthPrefix, bytes.Length,
                "Reported size covers the header and the payload only");

            byte[] payload = new byte[lengthPrefix];
            Array.Copy(bytes, (int)HEADER_SIZE, payload, 0, lengthPrefix);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(fileID), payload,
                "Payload is the raw UTF-8 encoding, no null terminator appended");
        }

        #endregion

        #region round trip

        [TestMethod]
        public void RoundTrip_BothOverloads_Agree()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.DIR);

            using UnmanagedMem unmanagedMem = original.ToUnmanagedMemory();

            FileIdentity fromMem = FileIdentity.FromUnmanagedMemory(unmanagedMem);
            FileIdentity fromPointer = FileIdentity.FromUnmanagedMemory(
                unmanagedMem.GetPointer(), unmanagedMem.GetSize());

            AssertIdentity(original, fromMem, "FromUnmanagedMemory(UnmanagedMem) is reversible");
            AssertIdentity(original, fromPointer, "FromUnmanagedMemory(nint, uint) is reversible");
        }

        [TestMethod]
        public void RoundTrip_AllDefinedFileTypes()
        {
            foreach (FileTypeOD fileType in Enum.GetValues<FileTypeOD>())
            {
                FileIdentity original = new("file-id-" + fileType, fileType);

                AssertIdentity(original, RoundTrip(original),
                    "fileType " + fileType + " is reversible");
            }
        }

        [TestMethod]
        public void RoundTrip_FileIdVariants()
        {
            List<(string fileID, string description)> values = new() {
                ("", "Empty string"),
                ("a", "Single character"),
                ("1234567890abcdef", "Typical hex file ID"),
                ("00000000-0000-0000-0000-000000000000", "GUID like ID"),
                (" leading and trailing ", "Leading and trailing spaces"),
                ("a\0b", "Embedded null character"),
                ("tab\tnewline\n carriage\r", "Control characters"),
                ("ěščřžýáíéú", "Accented Latin - multi byte UTF-8"),
                ("Привет", "Cyrillic"),
                ("ファイル名", "CJK"),
                ("🙂🙃🌍", "Emoji - surrogate pairs"),
                ("<>:\"/\\|?*|", "Windows path and reserved characters"),
                ("", "Empty string again"),
            };

            foreach (var value in values)
            {
                foreach (FileTypeOD fileType in Enum.GetValues<FileTypeOD>())
                {
                    FileIdentity original = new(value.fileID, fileType);

                    AssertIdentity(original, RoundTrip(original),
                        "fileID is reversible - " + value.description + " / " + fileType);
                }
            }
        }

        [TestMethod]
        public void RoundTrip_DefaultInstance()
        {
            FileIdentity original = new();
            FileIdentity restored = RoundTrip(original);

            AssertIdentity(original, restored, "Default FileIdentity is reversible");
            Assert.AreEqual("", restored.fileID, "Restored fileID is empty string, not null");
            Assert.AreEqual(FileTypeOD.EMPTY, restored.fileType, "Restored fileType is EMPTY");
        }

        [TestMethod]
        public void RoundTrip_LongFileId()
        {
            List<int> lengths = new() { 1023, 1024, 1025, 65536 };

            foreach (int length in lengths)
            {
                string fileID = new string('x', length);
                FileIdentity original = new(fileID, FileTypeOD.REG);

                FileIdentity restored = RoundTrip(original);

                Assert.AreEqual(length, restored.fileID.Length,
                    "Nothing truncated at length " + length);
                AssertIdentity(original, restored, "Long fileID of length " + length + " is reversible");
            }
        }

        [TestMethod]
        public void RoundTrip_IsIdempotent()
        {
            FileIdentity original = new("ěščřžýáíé-1234567890", FileTypeOD.SYMLNK);

            using UnmanagedMem first = original.ToUnmanagedMemory();
            FileIdentity restored = FileIdentity.FromUnmanagedMemory(first);
            byte[] firstBytes = ReadAll(first);

            using UnmanagedMem second = restored.ToUnmanagedMemory();

            CollectionAssert.AreEqual(firstBytes, ReadAll(second),
                "Second conversion produces byte identical memory");
            AssertIdentity(restored, FileIdentity.FromUnmanagedMemory(second),
                "Second round trip is stable");
        }

        [TestMethod]
        public void RoundTrip_StructValueEquality()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.REG);

            Assert.AreEqual(original, RoundTrip(original),
                "Restored struct equals the original one");
            Assert.AreNotEqual(original, new FileIdentity("1234567890abcdeg", FileTypeOD.REG),
                "Different fileID is not equal");
            Assert.AreNotEqual(original, new FileIdentity("1234567890abcdef", FileTypeOD.DIR),
                "Different fileType is not equal");
        }

        #endregion

        #region symlinkTarget

        [TestMethod]
        public void DefaultConstructor_SymlinkTargetIsNull()
        {
            FileIdentity fileIdentity = new();

            Assert.IsNull(fileIdentity.symlinkTargetId, "Default symlinkTarget is null (no symlink)");
        }

        [TestMethod]
        public void Constructor_SymlinkTargetDefaultsToNull()
        {
            FileIdentity fileIdentity = new("1234567890abcdef", FileTypeOD.REG);

            Assert.IsNull(fileIdentity.symlinkTargetId, "Omitted symlinkTarget is null");
        }

        [TestMethod]
        public void ToUnmanagedMemory_NullSymlinkTarget_KeepsLegacySize()
        {
            // No target section at all - identities without a symlink stay byte compatible
            const string fileID = "1234567890abcdef";

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.REG).ToUnmanagedMemory();

            Assert.AreEqual(HEADER_SIZE + Encoding.UTF8.GetByteCount(fileID), unmanagedMem.GetSize(),
                "Null symlinkTarget allocates nothing extra");
        }

        [TestMethod]
        public void ToUnmanagedMemory_SymlinkTarget_SizeIsBothSections()
        {
            const string fileID = "1234567890abcdef";
            const string symlinkTarget = "/MySpace/dir/target";

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.SYMLNK, symlinkTarget)
                .ToUnmanagedMemory();

            uint expectedSize = HEADER_SIZE + (uint)Encoding.UTF8.GetByteCount(fileID)
                + sizeof(int) + (uint)Encoding.UTF8.GetByteCount(symlinkTarget);

            Assert.AreEqual(expectedSize, unmanagedMem.GetSize(),
                "Target length prefix and UTF-8 bytes are appended");
        }

        [TestMethod]
        public void ToUnmanagedMemory_SymlinkTarget_Layout_MatchesDocumentedBytes()
        {
            // fileType = SYMLNK (3), fileID length = 3, data = "abc",
            // symlinkTarget length = 3, data = "xyz"
            using UnmanagedMem unmanagedMem = new FileIdentity("abc", FileTypeOD.SYMLNK, "xyz")
                .ToUnmanagedMemory();

            Assert.AreEqual("03000000030000006162630300000078797A",
                Convert.ToHexString(ReadAll(unmanagedMem)),
                "Target length and raw UTF-8 bytes follow the fileID");
        }

        [TestMethod]
        public void ToUnmanagedMemory_EmptySymlinkTarget_WritesZeroLengthSection()
        {
            // Empty string is a real value distinct from null - it still occupies a length prefix
            using UnmanagedMem unmanagedMem = new FileIdentity("abc", FileTypeOD.SYMLNK, "")
                .ToUnmanagedMemory();

            Assert.AreEqual(HEADER_SIZE + 3 + sizeof(int), unmanagedMem.GetSize(),
                "Empty target is a length prefix with no payload");
        }

        [TestMethod]
        public void ToUnmanagedMemory_SymlinkTarget_NoTrailingBytes()
        {
            const string fileID = "1234567890abcdef";
            const string symlinkTarget = "/MySpace/Playground/files 1000";

            using UnmanagedMem unmanagedMem = new FileIdentity(fileID, FileTypeOD.SYMLNK, symlinkTarget)
                .ToUnmanagedMemory();
            byte[] bytes = ReadAll(unmanagedMem);

            int fileIDLength = BitConverter.ToInt32(bytes, sizeof(int));
            int targetLength = BitConverter.ToInt32(bytes, (int)HEADER_SIZE + fileIDLength);

            Assert.AreEqual(Encoding.UTF8.GetByteCount(symlinkTarget), targetLength,
                "Target length prefix written after the fileID payload");
            Assert.AreEqual((int)HEADER_SIZE + fileIDLength + sizeof(int) + targetLength, bytes.Length,
                "Reported size covers both sections only");
        }

        [TestMethod]
        public void RoundTrip_SymlinkTarget_Variants()
        {
            List<(string target, string description)> values = new() {
                ("", "Empty target"),
                ("/", "Root of the space"),
                ("/Playground/files 1000", "Target with a space in the name"),
                ("/MySpace/ěščřžýáíé", "Accented Latin - multi byte UTF-8"),
                ("/MySpace/ファイル", "CJK"),
                ("/MySpace/🙂🙃", "Emoji - surrogate pairs"),
                ("<__onedata_space_id:4ef7>/Playground", "Space id token kept verbatim"),
                ("relative/target", "Relative target"),
            };

            foreach (var value in values)
            {
                FileIdentity original = new("1234567890abcdef", FileTypeOD.SYMLNK, value.target);

                AssertIdentity(original, RoundTrip(original),
                    "symlinkTarget is reversible - " + value.description);
            }
        }

        [TestMethod]
        public void RoundTrip_SymlinkTarget_OnAllFileTypes()
        {
            foreach (FileTypeOD fileType in Enum.GetValues<FileTypeOD>())
            {
                FileIdentity original = new("file-id-" + fileType, fileType, "/MySpace/target");

                AssertIdentity(original, RoundTrip(original),
                    "symlinkTarget is reversible for fileType " + fileType);
            }
        }

        [TestMethod]
        public void RoundTrip_NullSymlinkTarget_StaysNull()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.REG, null);

            FileIdentity restored = RoundTrip(original);

            Assert.IsNull(restored.symlinkTargetId,
                "Null stays null - it is not turned into an empty string");
        }

        [TestMethod]
        public void RoundTrip_EmptySymlinkTarget_StaysEmpty()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.SYMLNK, "");

            FileIdentity restored = RoundTrip(original);

            Assert.AreEqual("", restored.symlinkTargetId,
                "Empty target comes back as empty string, not null");
        }

        [TestMethod]
        public void RoundTrip_NullAndEmptySymlinkTarget_AreDistinguishable()
        {
            FileIdentity withNull = new("1234567890abcdef", FileTypeOD.SYMLNK, null);
            FileIdentity withEmpty = new("1234567890abcdef", FileTypeOD.SYMLNK, "");

            Assert.AreNotEqual(RoundTrip(withNull), RoundTrip(withEmpty),
                "null (no symlink) and empty target serialize differently");
        }

        [TestMethod]
        public void RoundTrip_SymlinkTarget_IsIdempotent()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.SYMLNK, "/MySpace/Playground/files 1000");

            using UnmanagedMem first = original.ToUnmanagedMemory();
            FileIdentity restored = FileIdentity.FromUnmanagedMemory(first);
            byte[] firstBytes = ReadAll(first);

            using UnmanagedMem second = restored.ToUnmanagedMemory();

            CollectionAssert.AreEqual(firstBytes, ReadAll(second),
                "Second conversion produces byte identical memory");
            AssertIdentity(restored, FileIdentity.FromUnmanagedMemory(second),
                "Second round trip is stable");
        }

        [TestMethod]
        public void RoundTrip_LongSymlinkTarget()
        {
            foreach (int length in new List<int> { 1023, 1024, 1025, 65536 })
            {
                string target = "/MySpace/" + new string('x', length);
                FileIdentity original = new("1234567890abcdef", FileTypeOD.SYMLNK, target);

                FileIdentity restored = RoundTrip(original);

                Assert.AreEqual(target.Length, restored.symlinkTargetId?.Length,
                    "Nothing truncated at target length " + length);
                AssertIdentity(original, restored, "Long symlinkTarget is reversible");
            }
        }

        [TestMethod]
        public void FromUnmanagedMemory_LegacyLayoutWithoutTargetSection_IsRead()
        {
            // Identity written before symlinkTarget existed - hand built in the old layout
            const string fileID = "1234567890abcdef";
            byte[] fileIDBytes = Encoding.UTF8.GetBytes(fileID);
            uint legacySize = HEADER_SIZE + (uint)fileIDBytes.Length;

            using UnmanagedMem unmanagedMem = new(legacySize);
            nint ptr = unmanagedMem.GetPointer();
            Marshal.WriteInt32(ptr, (int)FileTypeOD.DIR);
            Marshal.WriteInt32(ptr + sizeof(int), fileIDBytes.Length);
            Marshal.Copy(fileIDBytes, 0, ptr + (int)HEADER_SIZE, fileIDBytes.Length);

            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual(FileTypeOD.DIR, restored.fileType, "fileType read from the legacy header");
            Assert.AreEqual(fileID, restored.fileID, "fileID read from the legacy payload");
            Assert.IsNull(restored.symlinkTargetId, "Missing target section reads as null");
        }

        [TestMethod]
        public void FromUnmanagedMemory_TruncatedTargetSection_ReadsAsEmpty()
        {
            // A length prefix whose payload does not fit must not read past the block
            using UnmanagedMem unmanagedMem = new(HEADER_SIZE + 3 + sizeof(int));
            nint ptr = unmanagedMem.GetPointer();
            Marshal.WriteInt32(ptr, (int)FileTypeOD.SYMLNK);
            Marshal.WriteInt32(ptr + sizeof(int), 3);
            Marshal.Copy(Encoding.UTF8.GetBytes("abc"), 0, ptr + (int)HEADER_SIZE, 3);
            Marshal.WriteInt32(ptr + (int)HEADER_SIZE + 3, 64);

            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual("abc", restored.fileID, "fileID is still read");
            Assert.AreEqual("", restored.symlinkTargetId, "Over-long target does not run past the block");
        }

        [TestMethod]
        public void FromUnmanagedMemory_NegativeTargetLength_ReturnsEmptyTarget()
        {
            using UnmanagedMem unmanagedMem = new(HEADER_SIZE + sizeof(int));
            nint ptr = unmanagedMem.GetPointer();
            Marshal.WriteInt32(ptr, (int)FileTypeOD.SYMLNK);
            Marshal.WriteInt32(ptr + sizeof(int), 0);
            Marshal.WriteInt32(ptr + (int)HEADER_SIZE, -1);

            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual("", restored.fileID, "fileID is still read");
            Assert.AreEqual("", restored.symlinkTargetId, "Negative length is treated as no target data");
        }

        #endregion

        #region documented lossy / lenient behaviour

        [TestMethod]
        public void RoundTrip_NullFileId_BecomesEmptyString()
        {
            FileIdentity original = new(null!, FileTypeOD.REG);

            FileIdentity restored = RoundTrip(original);

            Assert.AreEqual("", restored.fileID,
                "null fileID is converted to empty string, so null itself is not reversible");
            Assert.AreEqual(FileTypeOD.REG, restored.fileType, "fileType survives");
        }

        [TestMethod]
        public void RoundTrip_UndefinedFileType_KeepsRawValue()
        {
            FileTypeOD unknown = (FileTypeOD)42;
            FileIdentity original = new("1234567890abcdef", unknown);

            FileIdentity restored = RoundTrip(original);

            Assert.AreEqual(42, (int)restored.fileType,
                "fileType is not validated, the raw int survives the round trip");
            Assert.IsFalse(Enum.IsDefined(restored.fileType),
                "Value is still not a defined FileTypeOD member");
            AssertIdentity(original, restored, "Undefined fileType is reversible");
        }

        [TestMethod]
        public void RoundTrip_LoneSurrogate_IsLossy()
        {
            // Not a valid character - Encoding.UTF8 replaces it, this documents the limit
            const string fileID = "valid\ud83dinvalid";

            FileIdentity restored = RoundTrip(new FileIdentity(fileID, FileTypeOD.REG));

            Assert.AreEqual(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(fileID)), restored.fileID,
                "Lone surrogate comes back as the replacement character");
            Assert.AreNotEqual(fileID, restored.fileID, "Round trip of a lone surrogate is lossy");
        }

        #endregion

        #region ownership and independence

        [TestMethod]
        public void ToUnmanagedMemory_IsIndependentCopy()
        {
            FileIdentity fileIdentity = new("original-id", FileTypeOD.REG);

            using UnmanagedMem unmanagedMem = fileIdentity.ToUnmanagedMemory();

            fileIdentity.fileID = "changed-id";
            fileIdentity.fileType = FileTypeOD.DIR;

            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual("original-id", restored.fileID,
                "Memory holds a copy - changing the struct afterwards does not touch it");
            Assert.AreEqual(FileTypeOD.REG, restored.fileType,
                "Memory holds a copy - changing the struct afterwards does not touch it");
        }

        [TestMethod]
        public void ToUnmanagedMemory_ReturnsSeparateAllocations()
        {
            FileIdentity fileIdentity = new("1234567890abcdef", FileTypeOD.REG);

            using UnmanagedMem first = fileIdentity.ToUnmanagedMemory();
            using UnmanagedMem second = fileIdentity.ToUnmanagedMemory();

            Assert.AreNotEqual(first.GetPointer(), second.GetPointer(),
                "Each call allocates its own memory - caller owns and disposes it");
            AssertIdentity(fileIdentity, FileIdentity.FromUnmanagedMemory(first),
                "First block stays valid while the second one exists");
        }

        [TestMethod]
        public void FromUnmanagedMemory_DisposedMemory_Throws()
        {
            UnmanagedMem unmanagedMem = new FileIdentity("1234567890abcdef", FileTypeOD.REG)
                .ToUnmanagedMemory();
            unmanagedMem.Dispose();

            Assert.IsFalse(unmanagedMem.IsValid(), "Disposed memory reports as invalid");
            Assert.ThrowsExactly<UnmanagedMemoryException>(
                () => FileIdentity.FromUnmanagedMemory(unmanagedMem),
                "Disposed memory cannot be read back");
        }

        #endregion

        #region invalid input

        [TestMethod]
        public void FromUnmanagedMemory_NullPointer_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => FileIdentity.FromUnmanagedMemory(nint.Zero, HEADER_SIZE),
                "Null pointer is rejected");

            Assert.ThrowsExactly<ArgumentException>(
                () => FileIdentity.FromUnmanagedMemory(nint.Zero, 1024),
                "Null pointer is rejected even for a plausible size");
        }

        [TestMethod]
        public void FromUnmanagedMemory_SizeTooSmall_Throws()
        {
            using UnmanagedMem unmanagedMem = new FileIdentity("1234567890abcdef", FileTypeOD.REG)
                .ToUnmanagedMemory();
            nint ptr = unmanagedMem.GetPointer();

            for (uint size = 0; size < HEADER_SIZE; size++)
            {
                Assert.ThrowsExactly<ArgumentException>(
                    () => FileIdentity.FromUnmanagedMemory(ptr, size),
                    "Size " + size + " cannot hold the header and is rejected");
            }
        }

        [TestMethod]
        public void FromUnmanagedMemory_MinimumSize_IsAccepted()
        {
            using UnmanagedMem unmanagedMem = new UnmanagedMem(HEADER_SIZE);
            Marshal.WriteInt32(unmanagedMem.GetPointer(), (int)FileTypeOD.DIR);
            Marshal.WriteInt32(unmanagedMem.GetPointer() + sizeof(int), 0);

            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual(FileTypeOD.DIR, restored.fileType, "fileType read from the header");
            Assert.AreEqual("", restored.fileID, "Zero length means empty fileID");
        }

        [TestMethod]
        public void FromUnmanagedMemory_NegativeLengthPrefix_ReturnsEmptyId()
        {
            using UnmanagedMem unmanagedMem = new UnmanagedMem(HEADER_SIZE);
            Marshal.WriteInt32(unmanagedMem.GetPointer(), (int)FileTypeOD.REG);
            Marshal.WriteInt32(unmanagedMem.GetPointer() + sizeof(int), -1);

            // Not validated against the block size - a negative length simply skips the copy.
            FileIdentity restored = FileIdentity.FromUnmanagedMemory(unmanagedMem);

            Assert.AreEqual(FileTypeOD.REG, restored.fileType, "fileType is still read");
            Assert.AreEqual("", restored.fileID, "Negative length is treated as no string data");
        }

        [TestMethod]
        public void FromUnmanagedMemory_IgnoresExtraBytesAfterString()
        {
            FileIdentity original = new("1234567890abcdef", FileTypeOD.REG);

            using UnmanagedMem unmanagedMem = original.ToUnmanagedMemory();
            nint ptr = unmanagedMem.GetPointer();

            FileIdentity exactSize = FileIdentity.FromUnmanagedMemory(ptr, unmanagedMem.GetSize());
            // The symlinkTarget section is optional and bounded by the reported size, so an
            // over-reported size can no longer be told apart from a real (empty) target section.
            // fileType and fileID stay correct either way.
            FileIdentity paddedSize = FileIdentity.FromUnmanagedMemory(
                ptr, unmanagedMem.GetSize() + 32);

            AssertIdentity(original, exactSize, "Exact size reads the whole identity");
            Assert.AreEqual(original.fileType, paddedSize.fileType, "Bytes after the string are ignored");
            Assert.AreEqual(original.fileID, paddedSize.fileID, "Bytes after the string are ignored");
        }

        #endregion
    }
}
