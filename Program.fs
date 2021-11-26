module Program

open System.Reflection
open Argu
open Clam
open Clam.Types


let runCommands (parser: ArgumentParser<CliArguments>) (args: string array) =
    let parsingResult = parser.Parse args

    match parsingResult.GetAllResults() with
    | [ List ] -> Commands.runList ()
    | [ Add subCommand ] ->
        let opts =
            { repositoryName = subCommand.GetResult(RepositoryName)
              branch =
                subCommand.TryGetResult(Branch)
                |> Option.flatten
                |> Option.defaultValue "main" }

        Commands.runAdd opts
    | [ Update subCommand ] ->
        let opts =
            { repositoryName = subCommand.GetResult(RepositoryName)
              branch =
                subCommand.TryGetResult(Branch)
                |> Option.flatten
                |> Option.defaultValue "main" }

        Commands.runUpdate opts
    | [ Remove repositoryName ] -> Commands.runRemove repositoryName
    | [ New subCommand ] ->
        let opts =
            { projectName = subCommand.GetResult(ProjectName)
              templateName = subCommand.GetResult(Template) }

        Commands.runNewProject opts
    | [ Version ] ->
        let version =
            Assembly.GetEntryAssembly().GetName().Version

        printfn $"{version.Major}.{version.Minor}.{version.Revision}"
    | _ ->
        let helpText = parser.PrintUsage()
        printfn $"{helpText}"

[<EntryPoint>]
let main args =
    try
        let parser = ArgumentParser.Create<CliArguments>()
        runCommands parser args
        Database.closeDatabase ()
    with
    | ex ->
        eprintfn $"{ex.Message}"
        Database.closeDatabase ()

    0
