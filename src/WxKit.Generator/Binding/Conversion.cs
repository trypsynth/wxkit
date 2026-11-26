namespace WxKit.Generator.Binding;

// How a value crosses from managed code through the shim to wxWidgets and back. Each kind of
// type has one conversion, which knows its form in all three places and the code that converts
// between them: the C parameters and return type in the shim, the C# types in the interop layer,
// and the C# type in the managed API. Managed code names WxKit's own types with global::WxKit,
// since a generated member can hide one, as Pen's Color property hides the Color type.
internal abstract class Conversion {
	/// <summary>A short name used to tell overloads apart, such as String or Int.</summary>
	public abstract string Suffix { get; }

	// The shim's side.
	public abstract IEnumerable<string> CParameters(string name);

	public abstract string ToCpp(string name);

	public abstract string CReturnType { get; }

	public abstract string FromCpp(string expression);

	/// <summary>What the shim returns when wxWidgets throws.</summary>
	public abstract string Fallback { get; }

	// The interop layer's side.
	public abstract IEnumerable<string> InteropParameters(string name);

	public abstract string InteropReturnType { get; }

	// The managed side.
	public abstract string ManagedType(bool nullable);

	/// <summary>The managed type of a value coming back, which can be more specific than the type going in.</summary>
	public virtual string ManagedReturnType(bool nullable) => ManagedType(nullable);

	// A director's callback, which wxWidgets calls through an overridden virtual member. Its
	// parameters come from C++ the way a return value does, so they use FromCpp and ManagedReturn,
	// and its result goes back the way a parameter does, through these.

	/// <summary>The C type a director's callback returns this as, or null when a callback cannot return it.</summary>
	public virtual string? CallbackCType => null;

	// A parameter a director's callback takes, as the shim passes it on from the overridden member.
	public virtual string CallbackParameterCType => CReturnType;

	public virtual string CallbackParameterInteropType => InteropReturnType;

	public virtual string ToCallbackArgument(string name) => FromCpp(name);

	public virtual string CallbackInteropType => InteropReturnType;

	/// <summary>C++ that turns what a callback returned into the C++ value.</summary>
	public virtual string FromCallback(string expression) => expression;

	/// <summary>C# that turns the managed value into what a callback returns.</summary>
	public virtual string ToCallback(string expression) => expression;

	/// <summary>A null check, for parameters that cannot be null.</summary>
	public virtual string? Check(string name, bool nullable) => null;

	/// <summary>A fixed statement the call needs, such as pinning a string.</summary>
	public virtual string? Fixed(string name) => null;

	/// <summary>A statement the call needs before it, such as building a native copy of a list.</summary>
	public virtual string? Prepare(string name) => null;

	/// <summary>A statement after the call, such as reading a list the call filled into an out parameter.</summary>
	public virtual string? Finish(string name) => null;

	public abstract IEnumerable<string> InteropArguments(string name, bool nullable);

	public abstract string ManagedReturn(string expression);

	/// <summary>Whether an object of this type can be null, so a default of nullptr can be written as null.</summary>
	public virtual bool AcceptsNull => false;
}
