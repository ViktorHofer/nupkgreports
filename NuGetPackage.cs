using System;
using System.Collections.Generic;

namespace NuPkgReports
{
    class NuGetPackage
    {
        public string PackageId { get; set; }

        public IEnumerable<NuGetFolder> Folders { get; set; }
    }

    class NuGetFolder : IEquatable<NuGetFolder>
    {
        public string TargetFramework { get; set; }

        public bool IsPlaceholder { get; set; }

        public bool IsHarvested { get; set; }

        public NuGetFolder(string targetFramework, bool isPlaceholder, bool isHarvested)
        {
            TargetFramework = targetFramework;
            IsPlaceholder = isPlaceholder;
            IsHarvested = isHarvested;
        }

        public override bool Equals(object obj) =>
            Equals(obj as NuGetFolder);

        public bool Equals(NuGetFolder other) =>
            other != null &&
            other.TargetFramework == TargetFramework &&
            other.IsPlaceholder == IsPlaceholder &&
            other.IsHarvested == IsHarvested;

        public override int GetHashCode() =>
            TargetFramework.GetHashCode();
    }

    enum NuGetFolderFilter
    {
        All,
        Harvested,
        Placeholders,
        NonPlaceholders
    }
}
