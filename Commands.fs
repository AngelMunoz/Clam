namespace Clam

open System
open System.IO
open LiteDB
open FsToolkit.ErrorHandling
open Clam.Types

module Commands =

    let private getRepoName (fullRepoName: string) =
        match
            fullRepoName.Split("/")
            |> Array.filter (String.IsNullOrWhiteSpace >> not)
            with
        | [| _; repoName |] -> Ok repoName
        | [| _ |] -> Error MissingRepoName
        | _ -> Error WrongGithubFormat

    let private getTemplateAndChild (templateName: string) =
        match
            templateName.Split("/")
            |> Array.filter (String.IsNullOrWhiteSpace >> not)
            with
        | [| user; template; child |] -> Some user, template, Some child
        | [| template; child |] -> None, template, Some child
        | [| template |] -> None, template, None
        | _ -> None, templateName, None

    let private matchParseResult result =
        match result with
        | Ok _ -> ()
        | Error MissingRepoName
        | Error WrongGithubFormat ->
            printfn "Wrong repository format"
            printfn "The repository name should look like this:"
            printfn "Username/templateRepo"

    let private updateRepo repoFullName branch =
        option {
            let! repo = Database.findByFullName repoFullName
            let repo = { repo with branch = branch }

            return!
                repo
                |> Scaffolding.downloadRepo
                |> Scaffolding.unzipAndClean
                |> Database.updateEntry
        }

    let runList () =
        let results = Database.listEntries ()

        if Seq.isEmpty results then
            printfn "No Repositories downloaded"
        else
            for entry in results do
                let date =
                    match entry.updatedAt |> Option.ofNullable with
                    | Some date -> date.ToShortDateString()
                    | None -> entry.createdAt.ToShortDateString()

                printfn $"[{date}] {entry.fullName} [{entry.branch}] -> {entry.path}"

    let runAdd (opts: RepositoryOptions) =
        result {
            let! repoName = getRepoName opts.repositoryName

            if Database.existsByFullName opts.repositoryName then
                printfn "The Repository already exists, Do you want to update it? [y/N]"

                match Console.ReadKey().Key with
                | ConsoleKey.Y ->
                    updateRepo opts.repositoryName opts.branch
                    |> ignore
                | _ -> ()
            else
                let path =
                    $"./templates/{repoName}-{opts.branch}"
                    |> Path.GetFullPath

                let newRepo =
                    { _id = ObjectId.NewObjectId()
                      name = repoName
                      fullName = opts.repositoryName
                      branch = opts.branch
                      path = path
                      createdAt = DateTime.Now
                      updatedAt = Nullable() }

                newRepo
                |> Scaffolding.downloadRepo
                |> Scaffolding.unzipAndClean
                |> Database.createEntry
                |> ignore
        }
        |> matchParseResult


    let runUpdate (opts: RepositoryOptions) =
        result {
            let! repoName = getRepoName opts.repositoryName

            match Database.findByFullName opts.repositoryName with
            | Some repo -> updateRepo repo.fullName opts.branch |> ignore
            | None -> ()
        }
        |> matchParseResult


    let runRemove (fullName: string) =
        option {
            let! repo = Database.findByFullName fullName
            Directory.Delete(repo.path, true)
            return! Database.deleteByFullName repo.fullName
        }
        |> fun result ->
            match result with
            | Some true -> printfn $"{fullName} deleted from repositories."
            | Some false -> printfn $"{fullName} could not be deleted from repositories."
            | None -> printfn "Repository Not Fund"

    let runNewProject (opts: ProjectOptions) =
        option {
            let (user, template, child) = getTemplateAndChild opts.templateName

            let! repo =
                match user, child with
                | Some user, Some _ -> Database.findByFullName $"{user}/{template}"
                | Some _, None -> Database.findByFullName opts.templateName
                | None, _ -> Database.findByName template

            let templatePath =
                match child with
                | Some child -> Path.Combine(repo.path, child)
                | None -> repo.path
                |> Path.GetFullPath

            let targetPath =
                Path.Combine("./", opts.projectName)
                |> Path.GetFullPath

            let content =
                let readTemplateScript =
                    try
                        File.ReadAllText(Path.Combine(templatePath, "templating.fsx"))
                        |> Some
                    with
                    | _ -> None

                let readRepoScript () =
                    try
                        File.ReadAllText(Path.Combine(repo.path, "templating.fsx"))
                        |> Some
                    with
                    | _ -> None

                readTemplateScript
                |> Option.orElseWith (fun () -> readRepoScript ())

            match content with
            | Some content ->
                Extensibility.getConfigurationFromScript content
                |> Scaffolding.compileAndCopy templatePath targetPath
            | None -> Scaffolding.compileAndCopy templatePath targetPath None
        }
        |> ignore
