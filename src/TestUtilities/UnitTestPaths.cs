﻿using System.IO;

namespace TestUtilities
{
    public static class UnitTestPaths
    {
        private const string TestDataName = "TestData";
        private const string GenomesName = "Genomes";

        public static string WorkingDirectory { get { return Directory.GetCurrentDirectory(); } }
        public static string TestDataDirectory { get { return Path.Combine(WorkingDirectory, TestDataName); } }
        public static string TestGenomesDirectory { get { return Path.Combine(TestDataDirectory, GenomesName); } }

    }
}
