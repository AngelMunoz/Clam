namespace Clam.Types

open System
open Argu
open LiteDB

[<CLIMutable>]
type ClamRepo =
    { _id: ObjectId
      name: string
      fullName: string
      branch: string
      path: string
      createdAt: DateTime
      updatedAt: Nullable<DateTime> }

type NameParsingErrors =
    | MissingRepoName
    | WrongGithubFormat

type TemplateNameKind =
    | SimpleName = 1
    | FullName = 2

type RepositoryOptions =
    { repositoryName: string
      branch: string }

type ProjectOptions =
    { name: string
      nameKind: TemplateNameKind }

type RepositoryArgs =
    | [<AltCommandLine("-n")>] RepositoryName of string
    | [<AltCommandLine("-b")>] Branch of string option

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | RepositoryName _ -> "Name of the repository where the template lives"
            | Branch _ -> "Branch to pick the repository from, defaults to \"main\""

type NewProjectArgs =
    | [<AltCommandLine("-n")>] Name of string
    | [<AltCommandLine("-k")>] NameKind of TemplateNameKind option

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "Name of the template to use"
            | NameKind _ -> "Choose between Username/repository (Full Name) and repository (Simple Name)."

type CliArguments =
    | [<CliPrefix(CliPrefix.None)>] List
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<RepositoryArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<RepositoryArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove of name: string
    | [<CliPrefix(CliPrefix.None)>] New of ParseResults<NewProjectArgs>
    | [<CliPrefix(CliPrefix.DoubleDash); AltCommandLine("-v")>] Version

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | List -> "Shows the existing template repositories."
            | Add _ -> "Adds a new template repository."
            | Update _ -> "Updates a specific template repository."
            | Remove _ -> "Deletes a template repository."
            | New _ -> "Scaffolds a new project from the existing repositories."
            | Version -> "Shows the CLI version."
