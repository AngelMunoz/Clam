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

    let private matchParseResult result =
        match result with
        | Ok _ -> ()
        | Error MissingRepoName
        | Error WrongGithubFormat ->
            printfn "Wrong repository format"
            printfn "The repository name should look like this:"
            printfn "Username/templateRepo"

    let private updateRepo repoName branch =
        option {
            let! repo = Database.findByName repoName
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
                printfn $"[{entry.createdAt.ToShortTimeString()}]{entry.fullName}: {entry.path}"

    let runAdd (opts: RepositoryOptions) =
        result {
            let! repoName = getRepoName opts.repositoryName

            if Database.existsByName opts.repositoryName then
                printfn "The Repository already exists, Do you want to update it? [y/N]"

                match Console.ReadKey().Key with
                | ConsoleKey.Y -> updateRepo repoName opts.branch |> ignore
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
            updateRepo repoName opts.branch |> ignore
        }
        |> matchParseResult


    let runRemove (name: string) =
        option {
            let! repo = Database.findByName name
            Directory.Delete(repo.path, true)
            return! Database.deleteByName name
        }
        |> fun result ->
            match result with
            | Some true -> printfn $"{name} deleted from repositories."
            | Some false -> printfn $"{name} could not be deleted from repositories."
            | None -> printfn "Repository Not Fund"

    let runNewProject (opts: ProjectOptions) = ()
