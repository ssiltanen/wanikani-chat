[<AutoOpen>]
module Utility

open System.Text.Json
open System.Text.Json.Serialization

let serializerOptions =
    JsonFSharpOptions
        .Default()
        .WithAllowNullFields()
        .WithUnionTagCaseInsensitive()
        .WithUnionExternalTag()
        .WithUnionUnwrapFieldlessTags()
        .WithUnionUnwrapSingleFieldCases()
        .ToJsonSerializerOptions()

let serialize data =
    JsonSerializer.Serialize(data, serializerOptions)

let deserialize<'T> (data: string) =
    JsonSerializer.Deserialize<'T>(data, serializerOptions)

module Async =
    let bind f computation = async.Bind(computation, f)
    let map f = bind (f >> async.Return)
