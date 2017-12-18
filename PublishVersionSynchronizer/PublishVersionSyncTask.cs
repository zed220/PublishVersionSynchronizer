using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PublishVersionSynchronizer {
    public class PublishVersionSyncTask : Task {
        [Required]
        public string ProjectFilePath {
            get; set;
        }
        [Required]
        public string VersionStringFilePath {
            get; set;
        }

        [Output]
        public string Error {
            get { return this._error; }
            set { this._error = value; }
        }
        string _error;

        public override bool Execute() {
            if(!File.Exists(ProjectFilePath)) {
                Error = $"Project File \"{ProjectFilePath}\" does not exists";
                return true;
            }
            if(!File.Exists(VersionStringFilePath)) {
                Error = $"Version File \"{VersionStringFilePath}\" does not exists";
                return true;
            }
            string versionString = null;
            var allCodeLines = File.ReadAllLines(VersionStringFilePath);
            foreach(var codeLine in allCodeLines) {
                if(codeLine.Contains("VersionString")) {
                    versionString = codeLine.Split('"').Where(s => s.Contains('.')).FirstOrDefault();
                    break;
                }
            }
            if(String.IsNullOrEmpty(versionString)) {
                Error = "Can not find version string.";
                return true;
            }
            if(versionString.Split('.').Length != 3) {
                Error = $"Version string has wrong format: {versionString}. It must be x.y.z";
                return true;
            }
            allCodeLines = File.ReadAllLines(ProjectFilePath);
            List<string> fixedCodeLines = new List<string>();
            foreach(var codeLine in allCodeLines) {
                if(!codeLine.Contains("<ApplicationVersion>")) {
                    fixedCodeLines.Add(codeLine);
                    continue;
                }
                if(codeLine.Contains(versionString))
                    return true;
                fixedCodeLines.Add($"    <ApplicationVersion>{versionString}.%2a</ApplicationVersion>");

            }
            try {
                if(File.Exists(ProjectFilePath + ".bak"))
                    File.Delete(ProjectFilePath + ".bak");
            }
            catch {
                Error = $"Can not delete {ProjectFilePath}.bak";
                return true;
            }
            File.Copy(ProjectFilePath, ProjectFilePath + ".bak");
            try {
                File.Delete(ProjectFilePath);
            }
            catch {
                File.Delete(ProjectFilePath + ".bak");
                Error = $"Can not delete {ProjectFilePath}";
                return true;
            }
            File.WriteAllLines(ProjectFilePath, fixedCodeLines);
            File.Delete(ProjectFilePath + ".bak");
            return true;
        }
    }
}
