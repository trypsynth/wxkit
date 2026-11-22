module WxKit.FSharp.Tests.TranslationTests

open WxKit.FSharp
open Xunit

// Without a catalog for the language, each function gives back its text, or the form for n.
[<Fact>]
let ``gives back text that has no translation`` () =
    Assert.Equal("&Open", tr "&Open")
    Assert.Equal("{0} file", trn "{0} file" "{0} files" 1L)
    Assert.Equal("{0} files", trn "{0} file" "{0} files" 2L)
    Assert.Equal("Open", trp "verb" "Open")
    Assert.Equal("{0} items", trpn "menu" "{0} item" "{0} items" 5L)
