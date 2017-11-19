namespace FSharp.Environment
// #nowarn "0025"

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System
open System.Reflection
open System.Collections

type EnvironmentTarget =
    | Process = 0
    | User = 1
    | Machine = 2

// cfg : TypeProviderConfig
[<TypeProvider>]
type EnvironmentVariableTypeProvider () as this =
    inherit TypeProviderForNamespaces()

    let asm = Assembly.GetExecutingAssembly()
    let [<Literal>] ns = "FSharp.Environment.TypeProviders"
    let [<Literal>] typeName = "EnvironmentVariableProvider"

    let providerDefinition = ProvidedTypeDefinition(asm, ns, typeName, None)
    let staticParams = [ ProvidedStaticParameter("target", typeof<EnvironmentTarget>) ]

    let instantiate typeName ([| target |]: obj array) =
        let provider = ProvidedTypeDefinition(asm, ns, typeName, None)
        // provider.SetAttributes(provider.Attributes  ||| TypeAttributes.Abstract ||| TypeAttributes.Sealed)

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
        |>! addXmlDocDelayed "Creates a reader for the environment."
        |> provider.AddMember

        envVariables
        |> Seq.cast<DictionaryEntry>
        |> Seq.map (fun (kvp) -> (string(kvp.Key), string(kvp.Value)))
        |> Seq.map (fun (key, value) ->
            makeProvidedProperty<string> (fun [] -> <@@ value @@>) key
            |>! addXmlDocDelayed value
            )
        |> Seq.toList
        |> provider.AddMembers

        provider

    do
        providerDefinition.DefineStaticParameters(staticParams, instantiate)
    do
        this.AddNamespace(ns, [ providerDefinition ])

[<assembly:TypeProviderAssembly>]
do ()
