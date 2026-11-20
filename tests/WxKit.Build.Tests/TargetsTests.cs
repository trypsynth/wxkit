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
