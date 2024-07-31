module Database

open System
open System.Data
open Microsoft.Data.Sqlite
open Dapper.FSharp.SQLite

let schema =
    """
CREATE TABLE IF NOT EXISTS Assignment (
    id INTEGER PRIMARY KEY,
    data_updated_at DATETIME,
    data JSONB
);

CREATE TABLE IF NOT EXISTS Vocabulary (
    id INTEGER PRIMARY KEY,
    data_updated_at DATETIME,
    data JSONB
);
"""

// Map database nulls with Options
Dapper.FSharp.SQLite.OptionTypes.register ()

let connection =
    let conn = new SqliteConnection("Data Source=turtles.db")
    conn.Open()
    conn

let createIfNotExist (conn: SqliteConnection) =
    use cmd = conn.CreateCommand()
    cmd.CommandText <- schema
    cmd.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore

let insertOrReplaceMultiple<'table> (conn: IDbConnection) (tableName: string) (insertValues: 'table[]) =
    if Array.isEmpty insertValues then
        async.Return()
    else
        insert {
            into (table'<'table> tableName)
            values (List.ofArray insertValues)
        }
        |> conn.InsertOrReplaceAsync
        |> Async.AwaitTask
        |> Async.Ignore

let tryGetLatestUpdateTime<'table> (conn: IDbConnection) (tableName: string) =
    select {
        for row in (table'<'table> tableName) do
            max "data_updated_at" "latest"
    }
    |> conn.SelectAsync<{| latest: DateTime option |}>
    |> Async.AwaitTask
    |> Async.map (Seq.tryHead >> (Option.bind _.latest))

let getAll<'table> (conn: IDbConnection) (tableName: string) : Async<seq<'table>> =
    select {
        for row in (table'<'table> tableName) do
            selectAll
    }
    |> conn.SelectAsync<'table>
    |> Async.AwaitTask
