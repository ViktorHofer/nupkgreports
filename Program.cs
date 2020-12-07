using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet.Packaging;
using SwiftExcel;

namespace NuPkgReports
{
    class Program
    {
        // Assume that assets in packages that are older than 3 days are harvested.
        static readonly DateTimeOffset CompareOffset = DateTimeOffset.UtcNow.AddDays(-3);

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Missing arguments...");
                Console.WriteLine("NuPkgReports <input:directory-with-nupkgs> <output:reports-output-directory>");
                return;
            }

            string nugetPackageDirectoryPath = args[0];
            string outputDirectory = args[1];

            // Analyze packages
            List<NuGetPackage> packages = new();
            foreach (string packageFile in Directory.GetFiles(nugetPackageDirectoryPath, "*.nupkg", SearchOption.TopDirectoryOnly))
            {
                packages.Add(GetPackage(packageFile));
            }

            // Create workbook reports and write to disk
            foreach (var filter in Enum.GetValues<NuGetFolderFilter>())
            {
                string alias = filter.ToString().ToLowerInvariant();
                Sheet sheet = new()
                {
                    Name = alias,
                    ColumnsWidth = new List<double> { 50, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 }
                };

                WriteWorkbook(Path.Combine(outputDirectory,
                    $"{alias}.xlsx"),
                    sheet, packages, filter);
            }
        }

        static NuGetPackage GetPackage(string packageFile)
        {
            PackageArchiveReader root = new(packageFile);

            var x = root.GetItems("runtimes");

            IEnumerable<NuGetFolder> folders = GetFolders(root.GetReferenceItems(), "ref", root)
                .Concat(GetFolders(root.GetLibItems(), "lib", root))
                .Concat(GetFolders(x, "runtimes", root))
                .Distinct()
                .OrderBy(b => b.TargetFramework);

            return new NuGetPackage
            {
                PackageId = root.NuspecReader.GetId(),
                Folders = folders
            };
        }

        static IEnumerable<NuGetFolder> GetFolders(IEnumerable<FrameworkSpecificGroup> frameworkSpecificGroups, string packageDirectory, PackageArchiveReader packageArchiveReader)
        {
            foreach (var frameworkSpecificGroup in frameworkSpecificGroups)
            {
                if (packageDirectory == "runtimes")
                {
                    foreach (string item in frameworkSpecificGroup.Items.Where(f => f.EndsWith(".dll")))
                    {
                        bool isHarvested = packageArchiveReader.GetEntry(item).LastWriteTime < CompareOffset;
                        yield return new NuGetFolder(item.Remove(item.LastIndexOf('/')), frameworkSpecificGroup.HasEmptyFolder, isHarvested);
                    }
                }
                else
                {
                    bool isHarvested = packageArchiveReader.EnumeratePackageEntries(frameworkSpecificGroup.Items, packageDirectory)
                        .Where(f => f.FileFullPath.EndsWith(".dll"))
                        .Any(s => s.PackageEntry.LastWriteTime < CompareOffset);
                    yield return new NuGetFolder(frameworkSpecificGroup.TargetFramework.GetShortFolderName(), frameworkSpecificGroup.HasEmptyFolder, isHarvested);
                }
            }
        }

        static void WriteWorkbook(string outputPath, Sheet sheet, IEnumerable<NuGetPackage> packages, NuGetFolderFilter filter)
        {
            using ExcelWriter ew = new(outputPath, sheet);
            int row = 1;

            foreach (NuGetPackage package in packages)
            {
                IEnumerable<NuGetFolder> folders = package.Folders;

                switch (filter)
                {
                    case NuGetFolderFilter.Harvested:
                        folders = folders.Where(f => f.IsHarvested && !f.IsPlaceholder);
                        break;
                    case NuGetFolderFilter.NonPlaceholders:
                        folders = folders.Where(f => !f.IsPlaceholder);
                        break;
                    case NuGetFolderFilter.Placeholders:
                        folders = folders.Where(f => f.IsPlaceholder);
                        break;
                }

                // Skip empty packages
                if (!folders.Any())
                {
                    continue;
                }

                ew.Write(package.PackageId, 1, row);
                for (int col = 0; col < folders.Count(); col++)
                {
                    ew.Write(folders.ElementAt(col).TargetFramework, col + 2, row);
                }

                row++;
            }
        }
    }
}
