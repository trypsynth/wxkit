using WxKit.Interop;

namespace WxKit;

public unsafe partial class CredentialEntryDialog {
	/// <summary>The user name typed, or the one to start with. Wraps GetCredentials().GetUser() and SetUser.</summary>
	public string UserName {
		get => NativeString.Take(WxKitNative.CredentialEntryDialogGetUserName(Native));
		set {
			ArgumentNullException.ThrowIfNull(value);
			fixed (char* text = value) WxKitNative.CredentialEntryDialogSetUserName(Native, text, NativeString.Length(value));
		}
	}

	/// <summary>
	/// The password typed, or the one to start with. Wraps GetCredentials().GetPassword() and
	/// SetPassword. wxWidgets wipes its own copy, but a string cannot be wiped, so keep it no longer
	/// than you need it.
	/// </summary>
	public string Password {
		get => NativeString.Take(WxKitNative.CredentialEntryDialogGetPassword(Native));
		set {
			ArgumentNullException.ThrowIfNull(value);
			fixed (char* text = value) WxKitNative.CredentialEntryDialogSetPassword(Native, text, NativeString.Length(value));
		}
	}
}

public unsafe partial class TreeListCtrl {
	/// <summary>The selected items, in no particular order. Wraps GetSelections.</summary>
	public TreeListItem[] Selections => NativeNumbers.TakeTreeListItems(WxKitNative.TreeListCtrlGetSelections(Native));
}
