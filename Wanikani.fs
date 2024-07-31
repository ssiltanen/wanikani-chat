module Wanikani

open System
open System.Net
open System.Text.Json.Serialization
open FsHttp
open FsHttp.Operators

open Database
open System.Data

let getResources<'a> token since path queryParams =
    let rec paged acc =
        function
        | None -> async.Return acc
        | Some path' ->
            http {
                GET path'
                IfModifiedSince(since |> Option.defaultValue DateTime.MinValue)
                query queryParams

                config_transformHeader (fun header ->
                    { header with
                        target.address = Some("https://api.wanikani.com" </> header.target.address.Value)
                        headers =
                            header.headers
                            |> Map.add "Cache-Control" "no-cache"
                            |> Map.add "Wanikani-Revision" "20170710"
                            |> Map.add "Authorization" $"Bearer {token}" })

            }
            |> Request.sendAsync
            |> Async.map (Response.expectHttpStatusCode HttpStatusCode.OK)
            |> Async.bind (function
                | Ok res ->
                    Response.deserializeJsonAsync<Collection<Resource<'a>>> res
                    |> Async.bind (fun collection ->
                        collection.pages.next_url
                        |> Option.map (_.PathAndQuery)
                        |> paged (Array.append acc collection.data))
                | Error expectation -> async.Return acc)

    paged [||] (Some path)

let fetchAndSaveChanges<'a>
    (conn: IDbConnection)
    (request: DateTime option -> Async<Resource<'a>[]>)
    (save: IDbConnection -> string -> Resource<string>[] -> Async<unit>)
    =
    let mapResourceToDb (r: Resource<'a>) =
        { id = r.id
          data_updated_at = r.data_updated_at
          data = serialize r.data }

    let table = typeof<'a>.Name

    tryGetLatestUpdateTime<'a> conn table
    |> Async.bind request
    |> Async.bind (Array.map mapResourceToDb >> save conn table)

let getAllSaved<'table> (conn: IDbConnection) =
    let mapResourceFromDb (r: Resource<string>) =
        { id = r.id
          data_updated_at = r.data_updated_at
          data = deserialize<'table> r.data }

    getAll<Resource<string>> conn typeof<'table>.Name
    |> Async.map (Seq.toArray >> Array.Parallel.map mapResourceFromDb)

module Assignment =

    let request token since =
        getResources<Assignment> token since "/v2/assignments" [ "subject_types", "vocabulary" ]

    let refreshAndRead conn token =
        fetchAndSaveChanges conn (request token) insertOrReplaceMultiple
        |> Async.bind (fun _ -> getAllSaved<Assignment> conn)

module Vocabulary =

    let request token since =
        getResources<Vocabulary> token since "/v2/subjects" [ "types", "vocabulary" ]

    let refreshAndRead conn token =
        fetchAndSaveChanges conn (request token) insertOrReplaceMultiple
        |> Async.bind (fun _ -> getAllSaved<Vocabulary> conn)
