/// Hashing files, which runs on whatever thread calls it.
module Checksums.Checksum

open System
open System.IO
open System.Security.Cryptography
open System.Threading

/// A file's SHA-256, or why it couldn't be read.
type Checksum = { Name: string; Hash: Result<string, string> }

let describe sum =
    match sum.Hash with
    | Ok hash -> $"{sum.Name}: {hash}"
    | Error error -> $"{sum.Name}: {error}"

let private hash (file: string) =
    try
        use stream = File.OpenRead file
        Ok(Convert.ToHexStringLower(SHA256.HashData stream))
    with
    | :? IOException
    | :? UnauthorizedAccessException as error -> Error error.Message

/// Hashes the files in order, telling progress about each before it starts, and stopping between
/// files once the token is canceled.
let compute (files: string list) (progress: IProgress<int * string>) (token: CancellationToken) =
    files
    |> List.mapi (fun index file ->
        token.ThrowIfCancellationRequested()
        let name = Path.GetFileName file |> Option.ofObj |> Option.defaultValue file
        progress.Report((index, name))
        { Name = name; Hash = hash file })
