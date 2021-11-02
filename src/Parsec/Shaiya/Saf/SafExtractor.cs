﻿using System.IO;
using Parsec.Extensions;
using Parsec.Helpers;

namespace Parsec.Shaiya
{
    public partial class Saf
    {
        /// <summary>
        /// Binary reader used to read the saf file. Must be disposed after reading is complete.
        /// </summary>
        private BinaryReader _safReader;

        /// <summary>
        /// Extracts all the content of the saf file into a directory
        /// </summary>
        /// <param name="extractionPath">Path where files should be saved</param>
        public void Extract(string extractionPath)
        {
            Directory.CreateDirectory(extractionPath);

            // Create binary reader
            _safReader = new BinaryReader(File.OpenRead("data.saf"));

            ExtractFolder(_sah.RootFolder, extractionPath);

            // Dispose binary reader
            _safReader.Dispose();
        }

        /// <summary>
        /// Extracts a shaiya folder from the saf file
        /// </summary>
        /// <param name="folder">Folder to extract</param>
        /// <param name="extractionDirectory">Directory where folder should be extracted</param>
        private void ExtractFolder(ShaiyaFolder folder, string extractionDirectory)
        {
            // Extract files
            foreach (var file in folder.Files)
            {
                ExtractFile(file, extractionDirectory);
            }

            // Extract subfolders
            foreach (var subfolder in folder.Subfolders)
            {
                var path = Path.Combine(extractionDirectory, subfolder.Name);

                Directory.CreateDirectory(path);

                ExtractFolder(subfolder, path);
            }
        }

        /// <summary>
        /// Extracts a shaiya file from the saf
        /// </summary>
        /// <param name="file">File to extract</param>
        /// <param name="extractionDirectory">Directory where file should be saved</param>
        private void ExtractFile(ShaiyaFile file, string extractionDirectory)
        {
            // Skip files with invalid characters
            if (file.Name.HasInvalidCharacters())
                return;

            var path = Path.Combine(extractionDirectory, file.Name);

            using var fileWriter = new BinaryWriter(File.OpenWrite(path));

            // Set offset to file's starting offset
            _safReader.BaseStream.Seek(file.Offset, SeekOrigin.Begin);

            // Read file bytes (length bytes)
            var bytesToWrite = _safReader.ReadBytes(file.Length);

            // Write bytes to file
            fileWriter.Write(bytesToWrite);
        }

        /// <summary>
        /// Extracts a single file into a directory
        /// </summary>
        /// <param name="file">The <see cref="ShaiyaFile"/> instance to extract</param>
        /// <param name="extractionDirectory">The directory where the file should be saved</param>
        public void Extract(ShaiyaFile file, string extractionDirectory)
        {
            var fileBytes = ReadSafBytes(file.Offset, file.Length);
            FileHelper.WriteFile(Path.Combine(extractionDirectory, file.Name), fileBytes);
        }

        /// <summary>
        /// Reads an array of bytes from the saf file
        /// </summary>
        /// <param name="offset">Offset where to start reading</param>
        /// <param name="length">Amount of bytes to read</param>
        private byte[] ReadSafBytes(long offset, int length)
        {
            var safReader = new BinaryReader(File.OpenRead(_sah.SafPath));

            safReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            // Read bytes
            byte[] bytes = safReader.ReadBytes(length);

            safReader.Dispose();

            return bytes;
        }
    }
}
