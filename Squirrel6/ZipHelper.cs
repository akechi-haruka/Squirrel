using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squirrel {

    // https://stackoverflow.com/a/35416368
    public static class ZipHelper {
        public static void CreateFromDirectory(
            string sourceDirectoryName
        , string destinationArchiveFileName
        , CompressionLevel compressionLevel
        , bool includeBaseDirectory
        , Predicate<string> filter // Add this parameter
        ) {
            if (string.IsNullOrEmpty(sourceDirectoryName)) {
                throw new ArgumentNullException("sourceDirectoryName");
            }
            if (string.IsNullOrEmpty(destinationArchiveFileName)) {
                throw new ArgumentNullException("destinationArchiveFileName");
            }
            var filesToAdd = Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories);
            var entryNames = GetEntryNames(filesToAdd, sourceDirectoryName, includeBaseDirectory);
            using (var zipFileStream = new FileStream(destinationArchiveFileName, FileMode.Create)) {
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create)) {
                    for (int i = 0; i < filesToAdd.Length; i++) {
                        // Add the following condition to do filtering:
                        if (!filter(filesToAdd[i])) {
                            continue;
                        }
                        archive.CreateEntryFromFile(filesToAdd[i], entryNames[i], compressionLevel);
                    }
                }
            }
        }

        private static string[] GetEntryNames(string[] names, string sourceFolder, bool includeBaseName) {
            if (names == null || names.Length == 0)
                return new string[0];

            if (includeBaseName)
                sourceFolder = Path.GetDirectoryName(sourceFolder);

            int length = string.IsNullOrEmpty(sourceFolder) ? 0 : sourceFolder.Length;
            if (length > 0 && sourceFolder != null && sourceFolder[length - 1] != Path.DirectorySeparatorChar && sourceFolder[length - 1] != Path.AltDirectorySeparatorChar)
                length++;

            var result = new string[names.Length];
            for (int i = 0; i < names.Length; i++) {
                result[i] = names[i].Substring(length);
            }

            return result;
        }
    }
}
