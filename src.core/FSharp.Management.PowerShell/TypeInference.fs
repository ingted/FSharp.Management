﻿module FSharp.Management.PowerShellProvider.TypeInference

open System
open System.Management.Automation
open FSharp.Management

type PSCommandSignature =
    {
        Name    : string // PowerShell command name
        UniqueID: string // Unique command identifier
        ParametersInfo    : (string*bool*Type)[]
        ResultType        : Type
        ResultObjectTypes : Type[]
    }

let getOutputTypes (command:CommandInfo) =
    let types =
        command.OutputType
        |> Seq.map (fun psTy ->
            match psTy.Type with
            | null -> typeof<obj>
            | ty -> ty)
        |> Seq.toArray
    match types with
    | _ when 0 < types.Length && types.Length <= 7
        -> types
    | _ -> Array.empty

let buildResultType (resultObjectTypes:Type[]) =
    //let tys =
    //    resultObjectTypes
    //    |> Array.map (fun ty ->
    //        typedefof<list<_>>.MakeGenericType([|ty|]))
    let choise =
        match resultObjectTypes.Length with
        | 1 -> resultObjectTypes.[0]
        | 2 -> typedefof<Choice<_,_>>.MakeGenericType(resultObjectTypes)
        | 3 -> typedefof<Choice<_,_,_>>.MakeGenericType(resultObjectTypes)
        | 4 -> typedefof<Choice<_,_,_,_>>.MakeGenericType(resultObjectTypes)
        | 5 -> typedefof<Choice<_,_,_,_,_>>.MakeGenericType(resultObjectTypes)
        | 6 -> typedefof<Choice<_,_,_,_,_,_>>.MakeGenericType(resultObjectTypes)
        | 7 -> typedefof<Choice<_,_,_,_,_,_,_>>.MakeGenericType(resultObjectTypes)
        | _ -> typeof<PSObject> //TODO: test it
    let orderedType =
        typedefof<list<_>>.MakeGenericType([|choise|])


    let failType = 
        typeof<list<ErrorRecord>>

    let returnType = 
        //[|choise; failType|]
        [|orderedType; failType|]

    typedefof<PsCmdletResult<_,_>>.MakeGenericType(returnType)

let getParameterProperties (parameterSet: CommandParameterSetInfo) =
    match parameterSet.Parameters with
    | null -> [||]
    | parameters ->
        parameters
        |> Seq.map (fun p-> p.Name, p.IsMandatory, p.ParameterType)
        |> Seq.toArray

let getPSCommandSignature (command:CommandInfo) (parameterSet:CommandParameterSetInfo) =
    let resultObjectTypes = getOutputTypes command
    {
        Name                = command.Name
        UniqueID            = command.Name + parameterSet.Name;
        ResultObjectTypes   = resultObjectTypes
        ResultType          = buildResultType resultObjectTypes
        ParametersInfo      = getParameterProperties parameterSet
    }



let toCamelCase s =
    if (String.IsNullOrEmpty(s) || not <| Char.IsLetter(s.[0]) || Char.IsLower(s.[0]))
        then s
        else sprintf "%c%s" (Char.ToLower(s.[0])) (s.Substring(1))

type CollectionConverter<'T> =
    static member Convert (objSeq:obj seq) =
        objSeq |> Seq.cast<'T> |> Seq.toList


//let getTypeOfObjects (types:Type[]) (collection:PSObject seq) =
//    let typeCandidates =
//        types 
//        |> Array.filter (fun ty ->
//            collection 
//            |> Seq.map(fun x ->
//                x.BaseObject) 
//            |> Seq.exists (fun x ->
//                (ty.IsInstanceOfType x) || (ty.IsInstanceOfType (unbox x))))
//    match typeCandidates with
//    | [||] -> None
//    | _ -> Some(typeCandidates)