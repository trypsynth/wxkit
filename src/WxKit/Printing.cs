using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>The pages a document has, and the ones to print unless the user chooses others.</summary>
/// <param name="MinPage">The first page, usually 1.</param>
/// <param name="MaxPage">The last page.</param>
/// <param name="FromPage">The first page to print by default.</param>
/// <param name="ToPage">The last page to print by default.</param>
public readonly record struct PageRange(int MinPage, int MaxPage, int FromPage, int ToPage) {
	/// <summary>Every page from 1 to <paramref name="count"/>, all of them printed by default.</summary>
	public static PageRange All(int count) => new(1, count, 1, count);
}

/// <summary>
/// A document to print or preview, drawn a page at a time. Wraps wxPrintout. Derive from it,
/// override <see cref="OnPrintPage"/> to draw each page, and <see cref="GetPageInfo"/> to say how
/// many there are, then give it to <see cref="Printer.Print"/> or <see cref="PrintPreview.Show"/>.
/// </summary>
/// <remarks>
/// A printout the application prints is the application's, which it disposes when the printing is
/// done, as it does a DC. One given to a preview is the preview's, which deletes it when the preview
/// closes, after which <see cref="NativeObject.IsDisposed"/> is true.
/// </remarks>
public abstract unsafe partial class Printout {
	private static bool _directorsSet;
	private readonly Holder? _holder;

	/// <summary>Makes a printout, whose title the system's print queue shows.</summary>
	protected Printout(string title = "Printout") : this(new Holder(), title) {
	}

	private Printout(Holder holder, string title) : this(Create(holder, title)) {
		holder.Printout = this;
		_holder = holder;
	}

	/// <summary>Draws a page, numbered from 1, on the DC, which is only good until this returns. Returns false to stop printing.</summary>
	protected abstract bool OnPrintPage(int page, DC dc);

	/// <summary>The pages the document has. By default, it has one.</summary>
	protected virtual PageRange GetPageInfo() => new(1, 1, 1, 1);

	/// <summary>Whether the document has the page. By default, the pages <see cref="GetPageInfo"/> gives.</summary>
	protected virtual bool HasPage(int page) {
		PageRange pages = GetPageInfo();
		return page >= pages.MinPage && page <= pages.MaxPage;
	}

	/// <summary>Called before anything else, once the printout knows the printer's page size, which is where to count the pages.</summary>
	protected virtual void OnPreparePrinting() {
	}

	/// <summary>Called once at the start of printing.</summary>
	protected virtual void OnBeginPrinting() {
	}

	/// <summary>Called once at the end of printing.</summary>
	protected virtual void OnEndPrinting() {
	}

	/// <summary>Called at the start of each copy, after wxWidgets has started the document. Returns false to stop printing.</summary>
	protected virtual bool OnBeginDocument(int startPage, int endPage) => true;

	/// <summary>Called at the end of each copy, before wxWidgets ends the document.</summary>
	protected virtual void OnEndDocument() {
	}

	// Given to a preview, which frees it from then on.
	internal void GiveAway() {
		_ = Native;
		Disown();
		_holder?.KeepAlive();
	}

	private static WxkObject* Create(Holder holder, string title) {
		ArgumentNullException.ThrowIfNull(title);
		Application.VerifyAccess();
		SetDirectors();
		// The native printout holds the managed one weakly, so one the application lets go of is
		// collected and freed, as other native objects are, and Directors frees the handle when it is
		// deleted. Once a preview takes it, the holder keeps it alive itself.
		GCHandle handle = GCHandle.Alloc(holder, GCHandleType.Weak);
		fixed (char* titleText = title) {
			WxkObject* native = WxKitNative.PrintoutNew((void*)GCHandle.ToIntPtr(handle), titleText, NativeString.Length(title));
			if (native is null) handle.Free();
			return native;
		}
	}

	// The callbacks every printout's director calls, with the userdata it was made with, which
	// Directors frees when wxWidgets deletes the printout.
	private static void SetDirectors() {
		if (_directorsSet) return;
		_directorsSet = true;
		Directors.Install();
		WxKitNative.PrintoutSetDirectors(&PrintPage, &HasPageCallback, &GetPageInfoCallback, &PreparePrinting, &BeginPrinting, &EndPrinting, &BeginDocument, &EndDocument);
	}

	private static Printout? Of(void* userdata) => ((Holder?)GCHandle.FromIntPtr((nint)userdata).Target)?.Printout;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte PrintPage(WxkObject* self, void* userdata, int page) {
		try {
			// The DC is only good while the page prints.
			WxkObject* dc = WxKitNative.PrintoutGetDC(self);
			if (Of(userdata) is not Printout printout || dc is null) return 0;
			using DC lent = DC.Lend(dc);
			return printout.OnPrintPage(page, lent) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte HasPageCallback(WxkObject* self, void* userdata, int page) {
		try {
			return Of(userdata)?.HasPage(page) == true ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void GetPageInfoCallback(WxkObject* self, void* userdata, int* minPage, int* maxPage, int* fromPage, int* toPage) {
		// wxWidgets doesn't set what it reads, so a printout that can't answer has one page.
		*minPage = *maxPage = *fromPage = *toPage = 1;
		try {
			if (Of(userdata) is not Printout printout) return;
			PageRange pages = printout.GetPageInfo();
			*minPage = pages.MinPage;
			*maxPage = pages.MaxPage;
			*fromPage = pages.FromPage;
			*toPage = pages.ToPage;
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void PreparePrinting(WxkObject* self, void* userdata) => Call(userdata, static printout => printout.OnPreparePrinting());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void BeginPrinting(WxkObject* self, void* userdata) => Call(userdata, static printout => printout.OnBeginPrinting());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void EndPrinting(WxkObject* self, void* userdata) => Call(userdata, static printout => printout.OnEndPrinting());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte BeginDocument(WxkObject* self, void* userdata, int startPage, int endPage) {
		try {
			return Of(userdata)?.OnBeginDocument(startPage, endPage) != false ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void EndDocument(WxkObject* self, void* userdata) => Call(userdata, static printout => printout.OnEndDocument());

	private static void Call(void* userdata, Action<Printout> call) {
		try {
			if (Of(userdata) is Printout printout) call(printout);
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	// The handle's target, made before the printout so the native one can be given it. wxWidgets
	// deletes the printout when the application disposes it or a preview closes.
	private sealed class Holder : IDirected {
		private GCHandle _keepAlive;

		public Printout? Printout { get; set; }

		// wxWidgets owns a printout a preview took, and calls it until it deletes it, though the
		// application may have let go of it.
		public void KeepAlive() {
			if (!_keepAlive.IsAllocated) _keepAlive = GCHandle.Alloc(this);
		}

		public void OnNativeFreed() {
			Printout?.Invalidate();
			if (_keepAlive.IsAllocated) _keepAlive.Free();
		}
	}
}

/// <summary>Shows a preview of a printout, in a frame of its own. Wraps wxPrintPreview and wxPreviewFrame.</summary>
public static unsafe class PrintPreview {
	/// <summary>
	/// Shows the preview, and returns its frame, or null when wxWidgets could not make it, as when
	/// there is no printer. The preview takes the printouts, and deletes them when it closes, or
	/// at once when it could not be made: <paramref name="printout"/> to show, and
	/// <paramref name="printoutForPrinting"/>, a second of the same document, which the preview's
	/// Print button prints, or null for none.
	/// </summary>
	public static Frame? Show(Printout printout, Printout? printoutForPrinting = null, Window? parent = null, string title = "Print Preview", PrintDialogData? data = null, Size? size = null) {
		ArgumentNullException.ThrowIfNull(printout);
		ArgumentNullException.ThrowIfNull(title);
		if (ReferenceEquals(printout, printoutForPrinting)) throw new ArgumentException("The preview needs a printout of its own to print with, not the one it shows.", nameof(printoutForPrinting));
		Application.VerifyAccess();
		WxkObject* shown = printout.Native;
		WxkObject* printing = printoutForPrinting is null ? null : printoutForPrinting.Native;
		WxkObject* dialogData = data is null ? null : data.Native;
		printout.GiveAway();
		printoutForPrinting?.GiveAway();
		fixed (char* titleText = title) {
			WxkWindow* frame = WxKitNative.PrintPreviewShow(shown, printing, dialogData, Window.NativeOf(parent), titleText, NativeString.Length(title), size ?? Size.Default);
			GC.KeepAlive(data);
			return ObjectRegistry.Wrap<Frame>(frame);
		}
	}
}
