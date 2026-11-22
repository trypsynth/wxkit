using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WxKit.Build.Tests;

// WxKit's targets, run by MSBuild on a project of their own, which compiles the tasks as an
// application's build does.
public class TargetsTests {
	private static readonly string TargetsFile = typeof(TargetsTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "TargetsFile").Value!;

	[Fact]
	public void ExtractsAProjectsStrings() {
		string project = Directory.CreateTempSubdirectory("wxkit-extract-").FullName;
		try {
			File.WriteAllText(Path.Combine(project, "App.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
					</PropertyGroup>
					<Import Project="{TargetsFile}" />
				</Project>
				""");
			File.WriteAllText(Path.Combine(project, "Program.cs"), """
				// TRANSLATORS: The window's title.
				string title = _("Hello");
				string files = _n("{0} file", "{0} files", 2, 2);
				string label = _(name);
				""");
			Directory.CreateDirectory(Path.Combine(project, "obj"));
			File.WriteAllText(Path.Combine(project, "obj", "Generated.cs"), "_(\"Not the project's own\");");
			(int exitCode, string output) = Run(project, "msbuild", "-t:WxKitExtract", "-nologo");
			Assert.True(exitCode == 0, output);
			Assert.Contains("warning WXKIT2001", output);
			string template = File.ReadAllText(Path.Combine(project, "po", "App.pot"));
			Assert.Contains("#. TRANSLATORS: The window's title.\n#: Program.cs:2\nmsgid \"Hello\"\n", template);
			Assert.Contains("#: Program.cs:3\n#, csharp-format\nmsgid \"{0} file\"\nmsgid_plural \"{0} files\"\n", template);
			Assert.DoesNotContain("Not the project's own", template);
		} finally {
			Directory.Delete(project, true);
		}
	}

	// The catalogs a build compiles, from po by default, or from WxKitCatalog items laid out otherwise.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FindsAProjectsCatalogs(bool laidOutOtherwise) {
		string project = Directory.CreateTempSubdirectory("wxkit-catalogs-").FullName;
		try {
			string items = laidOutOtherwise ? "<ItemGroup><WxKitCatalog Include=\"locale/pt-BR/App.po\" Language=\"pt-BR\" /></ItemGroup>" : string.Empty;
			File.WriteAllText(Path.Combine(project, "App.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
					</PropertyGroup>
					{items}
					<Import Project="{TargetsFile}" />
				</Project>
				""");
			string catalog = laidOutOtherwise ? Path.Combine(project, "locale", "pt-BR", "App.po") : Path.Combine(project, "po", "pt-BR.po");
			Directory.CreateDirectory(Path.GetDirectoryName(catalog)!);
			File.WriteAllText(catalog, "msgid \"\"\nmsgstr \"\"\n");
			// dotnet --version stands in for msgfmt --version, so the catalogs are found without gettext.
			(int exitCode, string output) = Run(project, "msbuild", "-t:_WxKitResolveCatalogs", "-getItem:_WxKitCatalog", "-p:WxKitMsgfmt=dotnet", "-nologo");
			Assert.True(exitCode == 0, output);
			Assert.Contains("\"LogicalName\": \"locale/pt_BR/App.mo\"", output);
		} finally {
			Directory.Delete(project, true);
		}
	}

	// The catalogs updated are the ones the build compiles, so catalogs laid out otherwise are too.
	[Fact]
	public void UpdatesCatalogsLaidOutOtherwise() {
		Assert.SkipUnless(HasMsgmerge(), "msgmerge, from GNU gettext, is not installed.");
		string project = Directory.CreateTempSubdirectory("wxkit-update-").FullName;
		try {
			File.WriteAllText(Path.Combine(project, "App.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
						<WxKitTranslationTemplate>locale/messages.pot</WxKitTranslationTemplate>
					</PropertyGroup>
					<ItemGroup><WxKitCatalog Include="locale/de/App.po" Language="de" /></ItemGroup>
					<Import Project="{TargetsFile}" />
				</Project>
				""");
			File.WriteAllText(Path.Combine(project, "Program.cs"), "string title = _(\"Hello\");");
			string catalog = Path.Combine(project, "locale", "de", "App.po");
			Directory.CreateDirectory(Path.GetDirectoryName(catalog)!);
			File.WriteAllText(catalog, "msgid \"\"\nmsgstr \"Content-Type: text/plain; charset=UTF-8\\n\"\n");
			(int exitCode, string output) = Run(project, "msbuild", "-t:WxKitUpdateCatalogs", "-nologo");
			Assert.True(exitCode == 0, output);
			Assert.True(File.Exists(Path.Combine(project, "locale", "messages.pot")), output);
			Assert.Contains("msgid \"Hello\"", File.ReadAllText(catalog));
		} finally {
			Directory.Delete(project, true);
		}
	}

	// A catalog keeps its CRLF line endings, as does the template, and a new string similar to an
	// old one gets no guessed translation, unless WxKitFuzzyMatching asks for one.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void UpdatesCatalogsWithoutChangingTheirLineEndings(bool fuzzyMatching) {
		Assert.SkipUnless(HasMsgmerge(), "msgmerge, from GNU gettext, is not installed.");
		string project = Directory.CreateTempSubdirectory("wxkit-merge-").FullName;
		try {
			File.WriteAllText(Path.Combine(project, "App.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
						<WxKitFuzzyMatching>{fuzzyMatching}</WxKitFuzzyMatching>
					</PropertyGroup>
					<Import Project="{TargetsFile}" />
				</Project>
				""");
			File.WriteAllText(Path.Combine(project, "Program.cs"), "string title = _(\"Open the file\");");
			Directory.CreateDirectory(Path.Combine(project, "po"));
			string template = Path.Combine(project, "po", "App.pot");
			File.WriteAllText(template, "msgid \"\"\r\nmsgstr \"\"\r\n");
			string catalog = Path.Combine(project, "po", "de.po");
			File.WriteAllText(catalog, "msgid \"\"\r\nmsgstr \"Content-Type: text/plain; charset=UTF-8\\n\"\r\n\r\nmsgid \"Open the files\"\r\nmsgstr \"Die Dateien öffnen\"\r\n");
			(int exitCode, string output) = Run(project, "msbuild", "-t:WxKitUpdateCatalogs", "-nologo");
			Assert.True(exitCode == 0, output);
			string merged = File.ReadAllText(catalog);
			Assert.DoesNotContain("\n", merged.Replace("\r\n", string.Empty));
			Assert.DoesNotContain("\n", File.ReadAllText(template).Replace("\r\n", string.Empty));
			Assert.Contains("msgid \"Open the file\"", merged);
			Assert.Equal(fuzzyMatching, merged.Contains("#, fuzzy"));
		} finally {
			Directory.Delete(project, true);
		}
	}

	private static bool HasMsgmerge() {
		try {
			using Process process = Process.Start(new ProcessStartInfo("msgmerge", "--version") { RedirectStandardOutput = true, RedirectStandardError = true })!;
			process.WaitForExit();
			return process.ExitCode == 0;
		} catch (System.ComponentModel.Win32Exception) {
			return false;
		}
	}

	private static (int ExitCode, string Output) Run(string directory, params string[] arguments) {
		ProcessStartInfo start = new("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (string argument in arguments) start.ArgumentList.Add(argument);
		// The test run's own MSBuild settings would otherwise reach the build it starts.
		foreach (string variable in new[] { "MSBuildExtensionsPath", "MSBUILD_EXE_PATH", "MSBuildSDKsPath" }) start.Environment.Remove(variable);
		using Process process = Process.Start(start)!;
		string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
		process.WaitForExit();
		return (process.ExitCode, output);
	}
}
