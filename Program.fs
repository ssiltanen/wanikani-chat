namespace WanikaniChat

open System
open System.CommandLine
open FsHttp
open Wanikani
open Database

module Program =

    GlobalConfig.Json.defaultJsonSerializerOptions <- serializerOptions

    let printPrompt token =
        async {
            do! createIfNotExist connection
            let! assignments = Assignment.refreshAndRead connection token
            let! vocabulary = Vocabulary.refreshAndRead connection token

            let output =
                assignments
                |> Array.map (fun assignment ->
                    let subject = vocabulary |> Array.find (_.id >> (=) assignment.data.subject_id)

                    sprintf "%s: %u" subject.data.characters assignment.data.srs_stage)
                |> String.concat ", "

            printfn
                "You are a helpful japanese language/kanji learning tutor penpal. The following list is a collection of kanji vocabulary I have learned. Each item is attached with a number between 0 and 9 meaning how well I know it. The higher the number, the better I know it. Please have a chat with me using the kanji I know. For kanjis I don't know use hiragana or katana. %s"
                output
        }

    [<EntryPoint>]
    let main argv =
        async {
            let tokenOpt = Option<string>([| "--token"; "-t" |], "Wanikani api token")
            tokenOpt.IsRequired <- true

            let rootCmd = RootCommand "Wanikani AI chat partner prompt creator"
            rootCmd.AddOption tokenOpt
            rootCmd.SetHandler(Action<_>(printPrompt >> Async.RunSynchronously), tokenOpt)

            return! rootCmd.InvokeAsync argv |> Async.AwaitTask
        }
        |> Async.RunSynchronously
