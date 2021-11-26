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

    let existsByName name =
        clamRepos()
            .Exists(fun clamRepo -> clamRepo.fullName = name)

    let findByName name =
        clamRepos().FindOne(fun repo -> repo.name = name) :> obj
        |> Option.ofObj
        |> Option.map (fun o -> o :?> ClamRepo)


    let updateByName name =
        match findByName name with
        | Some repo ->
            let repo =
                { repo with updatedAt = Nullable(DateTime.Now) }

            clamRepos().Update(repo)
        | None -> false

    let updateEntry (repo: ClamRepo option) =
        option {
            let! repo = repo
            return! updateByName repo.fullName
        }

    let deleteByName name =
        match findByName name with
        | Some repo -> clamRepos().Delete(BsonValue(repo._id))
        | None -> false
