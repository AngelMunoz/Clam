namespace Clam

open System
open System.IO
open System.IO.Compression
open Scriban
open FsHttp
open FsHttp.DslCE
open FsToolkit.ErrorHandling

open Clam.Types

module Scaffolding =
    let downloadRepo repo =
        let url =
            $"https://github.com/{repo.fullName}/archive/refs/heads/{repo.branch}.zip"

        try
            get url { send }
            |> Response.saveFile $"{repo.name}.zip"

            Some repo
        with
        | ex -> None

    let unzipAndClean repo =
        match repo with
        | Some repo ->
            Directory.CreateDirectory "./templates" |> ignore
            Directory.Delete(repo.path, true) |> ignore

            let relativePath =
                Path.Join(repo.path, "../", "../")
                |> Path.GetFullPath

            let zipPath =
                Path.Combine(relativePath, $"{repo.name}.zip")
                |> Path.GetFullPath

            ZipFile.ExtractToDirectory(zipPath, "./templates/")
            File.Delete(zipPath)
            Some repo
        | None -> None

    let collectRepositoryFiles (path: string) =
        let foldFilesAndTemplates (files, templates) (next: string) =
            if next.Contains(".tpl.") then
                (files, next :: templates)
            else
                (next :: files, templates)

        Directory.EnumerateFiles(path, "*.*")
        |> Seq.fold foldFilesAndTemplates (List.empty<string>, List.empty<string>)

    let compileFiles (payload: obj) (file: string) =
        let tpl = Template.Parse(file)
        tpl.Render(payload)

    let compileAndCopy (origin: string) (target: string) (payload: obj) =
        let (files, templates) = collectRepositoryFiles origin

        let copyFiles () =
            files
            |> Array.ofList
            |> Array.Parallel.iter (fun file -> File.Copy(file, file.Replace(origin, target), true))

        let copyTemplates () =
            templates
            |> Array.ofList
            |> Array.Parallel.iter (fun path ->
                let content =
                    File.ReadAllText(path) |> compileFiles payload

                File.WriteAllText(content, path.Replace(origin, target).Replace(".tpl", "")))

        Directory.CreateDirectory(target) |> ignore
        copyFiles ()
        copyTemplates ()
