using System;
using System.Collections.Generic;
using System.IO;
using StarBreaker.FileSystem;

namespace StarBreaker.P4k
{
    public sealed class P4kFileSystem : IFileSystem
    {
        private readonly P4kDirectoryNode _rootNode;

        public P4kFileSystem(P4kFile file)
        {
            _rootNode = P4kDirectoryNode.FromP4k(file);
        }

        public IEnumerable<string> EnumerateFiles(string path) =>
            _rootNode.EnumerateFiles(path);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
            _rootNode.EnumerateFiles(path, searchPattern);

        public IEnumerable<string> EnumerateDirectories(string path) =>
            _rootNode.EnumerateDirectories(path);

        public bool FileExists(string path) =>
            _rootNode.FileExists(path);

        public Stream OpenRead(string path) =>
            _rootNode.OpenRead(path);

        public byte[] ReadAllBytes(string path) =>
            _rootNode.ReadAllBytes(path);
    }
} 