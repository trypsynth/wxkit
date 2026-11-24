module Checklist.Drawing

open WxKit

/// The system's color for an element, or the fallback where it has none.
let systemColor color fallback =
    SystemSettings.GetColor color |> Option.ofNullable |> Option.defaultValue fallback

/// An opaque color from its red, green, and blue.
let rgb red green blue = Color(byte red, byte green, byte blue)
