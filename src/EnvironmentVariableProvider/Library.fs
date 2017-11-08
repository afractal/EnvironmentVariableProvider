namespace  FSharp.Environment
// #nowarn "0025"

open ProviderImplementation.ProvidedTypes
open ProviderImplementation.ProvidedTypes.ProvidedTypesHelpers
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System
open System.Reflection
open System.Collections

type EnvironmentTarget =
    | Process = 0
    | User = 1
    | Machine = 2

[<TypeProvider>]
type EnvironmentVariableTypeProvider() as this =
    inherit TypeProviderForNamespaces()

    let asm = Assembly.GetExecutingAssembly()
    let [<Literal>] ns = "FSharp.Environment.TypeProviders"
    let [<Literal>] typeName = "EnvironmentVariableProvider"

    let providerDefinition = ProvidedTypeDefinition(asm, ns, typeName, None)

    let instantiate typeName ([| target |]: obj array) =
        let provider = ProvidedTypeDefinition(asm, ns, typeName, None)

        let targetType =
            match target with
            | :? EnvironmentTarget as castedTarget ->
                match castedTarget with
                | EnvironmentTarget.Machine -> EnvironmentVariableTarget.Machine
                | EnvironmentTarget.User -> EnvironmentVariableTarget.User
                | EnvironmentTarget.Process -> EnvironmentVariableTarget.Process
                | _ -> EnvironmentVariableTarget.Process
            | _ -> EnvironmentVariableTarget.Process

        let envVariables = Environment.GetEnvironmentVariables(targetType)

        makeProvidedConstructor List.empty (fun [] -> <@@ "" @@>)
        |>! addXmlDocDelayed "Creates a reader for the specified file."
        |> provider.AddMember

        envVariables
        |> Seq.cast<DictionaryEntry>
        |> Seq.map (fun (kvp) -> (string(kvp.Key), string(kvp.Value)))
        |> Seq.map (fun (key, value) ->
            makeProvidedProperty<string> (fun [] -> <@@ value @@>) key
            |>! addXmlDocDelayed "Gets the picture attached to the file."
            )
        |> Seq.toList
        |> provider.AddMembers

        provider

    do
        providerDefinition.DefineStaticParameters(
            [ProvidedStaticParameter("target", typeof<EnvironmentTarget>)] ,
            instantiate)
    do
        this.AddNamespace(ns, [ providerDefinition ])

[<assembly:TypeProviderAssembly>]
do ()
