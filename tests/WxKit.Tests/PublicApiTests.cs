using System.Globalization;
using System.Reflection;
using System.Text;

namespace WxKit.Tests;

// WxKit's public API, written out and compared with PublicApi.txt, so a change to it, such as a
// generator rule that renames members, shows up in review rather than in an application. The raw
// interop layer is internal, and fails this test if any of it becomes public.
//
// When a change is meant, set WXKIT_UPDATE_API to 1 and run this test, which rewrites the file.
public class PublicApiTests {
	private const string SnapshotName = "PublicApi.txt";

	[Fact]
	public void MatchesTheSnapshot() {
		string actual = PublicApi.Describe(typeof(Application).Assembly);
		string expectedPath = Path.Combine(AppContext.BaseDirectory, SnapshotName);
		string expected = File.Exists(expectedPath) ? File.ReadAllText(expectedPath).ReplaceLineEndings("\n") : string.Empty;
		if (actual == expected) return;
		if (Environment.GetEnvironmentVariable("WXKIT_UPDATE_API") == "1") {
			File.WriteAllText(Path.Combine(ProjectDirectory(), SnapshotName), actual);
			return;
		}
		string received = Path.Combine(AppContext.BaseDirectory, "PublicApi.received.txt");
		File.WriteAllText(received, actual);
		HashSet<string> before = [.. expected.Split('\n')];
		HashSet<string> after = [.. actual.Split('\n')];
		IEnumerable<string> changes = after.Where(line => !before.Contains(line)).Select(static line => "+ " + line)
			.Concat(before.Where(line => !after.Contains(line)).Select(static line => "- " + line));
		Assert.Fail($"WxKit's public API differs from {SnapshotName}. If the change is meant, set WXKIT_UPDATE_API to 1 and run this test again. The API as it is now is in {received}.\n{string.Join('\n', changes.Take(40))}");
	}

	private static string ProjectDirectory() => typeof(PublicApiTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(static attribute => attribute.Key == "ProjectDirectory").Value!;
}

// A line for each public type and member, in a C# like form with nullability and defaults, sorted
// so the file only changes where the API does.
internal static class PublicApi {
	private static readonly NullabilityInfoContext Nullability = new();

	private static readonly Dictionary<Type, string> Keywords = new() {
		[typeof(void)] = "void",
		[typeof(object)] = "object",
		[typeof(string)] = "string",
		[typeof(bool)] = "bool",
		[typeof(byte)] = "byte",
		[typeof(sbyte)] = "sbyte",
		[typeof(char)] = "char",
		[typeof(short)] = "short",
		[typeof(ushort)] = "ushort",
		[typeof(int)] = "int",
		[typeof(uint)] = "uint",
		[typeof(long)] = "long",
		[typeof(ulong)] = "ulong",
		[typeof(float)] = "float",
		[typeof(double)] = "double",
		[typeof(decimal)] = "decimal",
		[typeof(nint)] = "nint",
		[typeof(nuint)] = "nuint",
	};

	public static string Describe(Assembly assembly) {
		StringBuilder text = new();
		foreach (Type type in assembly.GetExportedTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal)) {
			text.Append(Header(type)).Append('\n');
			foreach (string member in Members(type).Order(StringComparer.Ordinal)) text.Append("    ").Append(member).Append('\n');
		}
		return text.ToString();
	}

	private static string Header(Type type) {
		string name = type.FullName!.Replace('+', '.');
		if (type.IsEnum) return $"{(type.IsDefined(typeof(FlagsAttribute)) ? "[Flags] " : string.Empty)}enum {name} : {Name(Enum.GetUnderlyingType(type))}";
		if (type.IsInterface) return $"interface {name}{Generics(type)}{Bases(type)}";
		if (typeof(Delegate).IsAssignableFrom(type)) {
			MethodInfo invoke = type.GetMethod("Invoke")!;
			return $"delegate {Name(invoke.ReturnType, Nullability.Create(invoke.ReturnParameter))} {name}{Generics(type)}({Parameters(invoke)})";
		}
		bool isRecord = type.GetMethod("<Clone>$") is not null || type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null || (type.IsValueType && type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null);
		string kind = type.IsValueType ? (isRecord ? "record struct" : "struct") : isRecord ? "record" : "class";
		string modifiers = type.IsValueType ? (type.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute)) ? "readonly " : string.Empty)
			: type.IsAbstract && type.IsSealed ? "static " : type.IsAbstract ? "abstract " : type.IsSealed ? "sealed " : string.Empty;
		return $"{modifiers}{kind} {name}{Generics(type)}{Bases(type)}";
	}

	private static string Bases(Type type) {
		List<string> bases = [];
		if (!type.IsValueType && !type.IsInterface && type.BaseType is Type baseType && baseType != typeof(object)) bases.Add(Name(baseType));
		Type[] inherited = type.BaseType?.GetInterfaces() ?? [];
		bases.AddRange(type.GetInterfaces().Where(face => !inherited.Contains(face) && face.IsPublic).Select(static face => Name(face)).Order(StringComparer.Ordinal));
		return bases.Count == 0 ? string.Empty : " : " + string.Join(", ", bases);
	}

	private static IEnumerable<string> Members(Type type) {
		const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		if (type.IsEnum) {
			foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static)) yield return $"{field.Name} = {Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}";
			yield break;
		}
		if (typeof(Delegate).IsAssignableFrom(type)) yield break;
		foreach (FieldInfo field in type.GetFields(all).Where(static field => IsVisible(field.IsPublic, field.IsFamily, field.IsFamilyOrAssembly) && !field.Name.Contains('<'))) {
			string value = field.IsLiteral ? " = " + Literal(field.GetRawConstantValue()) : string.Empty;
			string modifiers = field.IsLiteral ? "const " : (field.IsStatic ? "static " : string.Empty) + (field.IsInitOnly ? "readonly " : string.Empty);
			yield return $"{Access(field.IsPublic)}{modifiers}{Name(field.FieldType, Nullability.Create(field))} {field.Name}{value}";
		}
		foreach (ConstructorInfo constructor in type.GetConstructors(all).Where(static constructor => IsVisible(constructor.IsPublic, constructor.IsFamily, constructor.IsFamilyOrAssembly))) {
			yield return $"{Access(constructor.IsPublic)}{(constructor.IsStatic ? "static " : string.Empty)}{TypeName(type)}({Parameters(constructor)})";
		}
		foreach (PropertyInfo property in type.GetProperties(all)) {
			MethodInfo? getter = property.GetMethod is { } get && IsVisible(get.IsPublic, get.IsFamily, get.IsFamilyOrAssembly) ? get : null;
			MethodInfo? setter = property.SetMethod is { } set && IsVisible(set.IsPublic, set.IsFamily, set.IsFamilyOrAssembly) ? set : null;
			if ((getter ?? setter) is not MethodInfo accessor) continue;
			string indexer = property.GetIndexParameters().Length > 0 ? $"this[{Parameters(property.GetIndexParameters())}]" : property.Name;
			string accessors = (getter is null ? string.Empty : "get; ") + (setter is null ? string.Empty : IsInit(setter) ? "init; " : "set; ");
			yield return $"{Access(accessor.IsPublic)}{Modifiers(accessor)}{Name(property.PropertyType, Nullability.Create(property))} {indexer} {{ {accessors}}}";
		}
		foreach (EventInfo @event in type.GetEvents(all)) {
			if (@event.AddMethod is not MethodInfo add || !IsVisible(add.IsPublic, add.IsFamily, add.IsFamilyOrAssembly)) continue;
			yield return $"{Access(add.IsPublic)}{Modifiers(add)}event {Name(@event.EventHandlerType!, Nullability.Create(@event))} {@event.Name}";
		}
		foreach (MethodInfo method in type.GetMethods(all).Where(static method => IsVisible(method.IsPublic, method.IsFamily, method.IsFamilyOrAssembly) && (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal)) && !method.Name.Contains('<'))) {
			string generics = method.IsGenericMethodDefinition ? $"<{string.Join(", ", method.GetGenericArguments().Select(static argument => argument.Name))}>" : string.Empty;
			yield return $"{Access(method.IsPublic)}{Modifiers(method)}{Name(method.ReturnType, Nullability.Create(method.ReturnParameter))} {method.Name}{generics}({Parameters(method)})";
		}
	}

	private static bool IsVisible(bool isPublic, bool isFamily, bool isFamilyOrAssembly) => isPublic || isFamily || isFamilyOrAssembly;

	private static string Access(bool isPublic) => isPublic ? string.Empty : "protected ";

	private static string Modifiers(MethodInfo method) {
		if (method.IsStatic) return "static ";
		if (method.IsAbstract) return "abstract ";
		if (method.GetBaseDefinition() != method) return method.IsFinal ? "sealed override " : "override ";
		return method.IsVirtual && !method.IsFinal ? "virtual " : string.Empty;
	}

	private static bool IsInit(MethodInfo setter) => setter.ReturnParameter.GetRequiredCustomModifiers().Any(static modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

	private static string Parameters(MethodBase method) => Parameters(method.GetParameters());

	private static string Parameters(ParameterInfo[] parameters) => string.Join(", ", parameters.Select(static parameter => {
		Type type = parameter.ParameterType;
		string modifier = string.Empty;
		if (type.IsByRef) {
			modifier = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
			type = type.GetElementType()!;
		}
		if (parameter.IsDefined(typeof(ParamArrayAttribute))) modifier = "params ";
		string value = parameter.HasDefaultValue ? " = " + Literal(parameter.DefaultValue) : string.Empty;
		return $"{modifier}{Name(type, Nullability.Create(parameter))} {parameter.Name}{value}";
	}));

	private static string Literal(object? value) => value switch {
		null => "null",
		string text => $"\"{text}\"",
		bool flag => flag ? "true" : "false",
		char character => $"'{character}'",
		Enum member => string.Join(" | ", member.ToString().Split(", ").Select(part => $"{TypeName(member.GetType())}.{part}")),
		IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? "null",
	};

	private static string Generics(Type type) => type.IsGenericTypeDefinition ? $"<{string.Join(", ", type.GetGenericArguments().Select(static argument => argument.Name))}>" : string.Empty;

	private static string TypeName(Type type) {
		string name = type.Name;
		int tick = name.IndexOf('`');
		return tick < 0 ? name : name[..tick];
	}

	private static string Name(Type type, NullabilityInfo? nullability = null) {
		if (Nullable.GetUnderlyingType(type) is Type underlying) return Name(underlying, nullability?.GenericTypeArguments.FirstOrDefault()) + "?";
		string suffix = !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable ? "?" : string.Empty;
		if (Keywords.TryGetValue(type, out string? keyword)) return keyword + suffix;
		if (type.IsArray) return $"{Name(type.GetElementType()!, nullability?.ElementType)}[{new string(',', type.GetArrayRank() - 1)}]{suffix}";
		if (type.IsPointer) return Name(type.GetElementType()!) + "*";
		if (type.IsGenericParameter) return type.Name + suffix;
		string name = TypeName(type);
		if (type.IsNested && !type.IsGenericParameter) name = $"{Name(type.DeclaringType!)}.{name}";
		if (type.IsGenericType) {
			Type[] arguments = type.GetGenericArguments();
			name += $"<{string.Join(", ", arguments.Select((argument, index) => Name(argument, nullability is not null && index < nullability.GenericTypeArguments.Length ? nullability.GenericTypeArguments[index] : null)))}>";
		}
		return name + suffix;
	}
}
