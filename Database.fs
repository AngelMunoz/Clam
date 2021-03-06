namespace Clam

open System
open LiteDB
open FsToolkit.ErrorHandling
open Clam.Types

module Database =
    let private db =
        lazy (new LiteDatabase("./templates.db"))

    let private clamRepos () =
        let repo = db.Value.GetCollection<ClamRepo>()

        repo.EnsureIndex(fun clamRepo -> clamRepo.fullName)
        |> ignore

        repo.EnsureIndex(fun clamRepo -> clamRepo.name)
        |> ignore

        repo

    let closeDatabase () = db.Value.Dispose()

    let listEntries () = clamRepos().FindAll()

    let createEntry (clamRepo: ClamRepo option) =
        option {
            let! clamRepo = clamRepo
            let result = clamRepos().Insert(clamRepo)

            match result |> Option.ofObj with
            | Some _ -> return clamRepo
            | None -> return! None
        }

    /// <summary>
    /// Checks if the the repository with given a name in the form of
    /// Username/Repository
    /// exists
    /// <param name="fullName">Full name of the template in the Username/Repository scheme</param>
    /// </summary>
    let existsByFullName fullName =
        clamRepos()
            .Exists(fun clamRepo -> clamRepo.fullName = fullName)

    /// <summary>
    /// Checks if the repository exists given a simple name 
    /// <param name="name">Simple name of the repository (not including the GitHub owner)</param>
    /// </summary>
    let existsByName name =
        clamRepos()
            .Exists(fun clamRepo -> clamRepo.name = name)

    /// <summary>
    /// Finds a repository using the name of the repository.
    /// <param name="name">Simple name of the repository (not including the GitHub owner)</param>
    /// </summary>
    let findByName name =
        clamRepos().FindOne(fun repo -> repo.name = name) :> obj
        |> Option.ofObj
        |> Option.map (fun o -> o :?> ClamRepo)

    /// <summary>
    /// Finds a repository using the full name of the repository (ex. Username/Repository)
    /// <param name="fullName">Full name of the repository including the GitHub owner</param>
    /// </summary>
    let findByFullName fullName =
        clamRepos()
            .FindOne(fun repo -> repo.fullName = fullName)
        :> obj
        |> Option.ofObj
        |> Option.map (fun o -> o :?> ClamRepo)

    let updateByName name =
        match findByName name with
        | Some repo ->
            let repo =
                { repo with
                      updatedAt = Nullable(DateTime.Now) }

            clamRepos().Update(repo)
        | None -> false

    let updateEntry (repo: ClamRepo option) =
        option {
            let! repo = repo
            return! updateByName repo.fullName
        }

    let deleteByFullName fullName =
        match findByFullName fullName with
        | Some repo -> clamRepos().Delete(BsonValue(repo._id))
        | None -> false
