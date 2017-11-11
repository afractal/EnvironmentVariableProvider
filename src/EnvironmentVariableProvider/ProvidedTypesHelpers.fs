namespace FSharp.Environment

[<AutoOpen>]
 module internal ProvidedTypesHelpers =

    let inline makeProvidedConstructor paramenters invokeCode =
        ProvidedConstructor(paramenters, InvokeCode = invokeCode)

    let inline makeProvidedProperty< ^T> getterCode propName =
        ProvidedProperty(propName, typeof< ^T>, GetterCode = getterCode)

    let inline makeProvidedMethod< ^T> parameters invokeCode methodName =
        ProvidedMethod(methodName, parameters, typeof< ^T>, InvokeCode = invokeCode)

    let inline makeProvidedParameter< ^T> paramName =
        ProvidedParameter(paramName, typeof< ^T>)

    let inline addXmlDocDelayed comment providerMember =
        (^a: (member AddXmlDocDelayed: (unit -> string) -> unit) providerMember,
             (fun () -> comment) )

    let inline tee fn x = x |> fn |> ignore; x

    let inline (|>!) x fn = tee fn x



