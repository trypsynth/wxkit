#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Updates each of an application's catalogs from the template with GNU gettext's msgmerge, which
// keeps the translations of strings that are still there, marks the ones whose string changed
// fuzzy, for a translator to review, and keeps the ones no longer used as obsolete. Unless
// FuzzyMatching is on, it doesn't guess at a translation for a new string from a similar old one,
// which is wrong more often than not. msgmerge writes LF line endings, so a catalog that had CRLF
// ones gets them back, rather than every line showing as changed.
public sealed class WxKitMergeCatalogs : Task {
	public ITaskItem[] Catalogs { get; set; } = Array.Empty<ITaskItem>();

	[Required]
	public string Template { get; set; } = string.Empty;

	[Required]
	public string Msgmerge { get; set; } = string.Empty;

	public bool FuzzyMatching { get; set; }

	public override bool Execute() {
		foreach (ITaskItem catalog in Catalogs) {
			string path = catalog.GetMetadata("FullPath");
			if (!File.Exists(path)) continue;
			bool crlf = File.ReadAllText(path).Contains("\r\n");
			string arguments = "--update --backup=none --quiet" + (FuzzyMatching ? string.Empty : " --no-fuzzy-matching") + " " + Quote(path) + " " + Quote(Template);
			ProcessStartInfo start = new ProcessStartInfo(Msgmerge, arguments) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
			string errors;
			int exitCode;
			try {
				using (Process process = Process.Start(start)!) {
					process.StandardOutput.ReadToEnd();
					errors = process.StandardError.ReadToEnd();
					process.WaitForExit();
					exitCode = process.ExitCode;
				}
			} catch (System.ComponentModel.Win32Exception) {
				Log.LogError($"msgmerge, from GNU gettext, is not installed, so {path} was not updated. Install gettext, or set WxKitMsgmerge to msgmerge's path.");
				continue;
			}
			if (exitCode != 0) {
				Log.LogError($"msgmerge could not update {path}: {errors.Trim()}");
				continue;
			}
			if (crlf) File.WriteAllText(path, File.ReadAllText(path).Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(false));
		}
		return !Log.HasLoggedErrors;
	}

	// A path as one argument. Catalog paths have no quotes of their own.
	private static string Quote(string path) => "\"" + path + "\"";
}
