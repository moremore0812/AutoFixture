﻿namespace Ploeh.AutoFixture.Idioms.FsCheck

open FsCheck
open FsCheck.Fluent
open System
open System.Reflection

module internal FsCheckInvoker =
    let GetValues (tuple : Type, owner) =
        seq {
            for pi in tuple.GetProperties() do
                yield tuple.GetProperty(pi.Name).GetValue(owner, null) }
        |> Seq.toArray

    let Invoke<'tuple> (methodInfo : MethodInfo, owner) =
       Check.QuickThrowOnFailure((fun (x : 'tuple) ->
           methodInfo.Invoke(
               owner,
               GetValues(typeof<'tuple>, x)) <> null))

[<AutoOpen>]
module internal ReturnValueExercises =
    let Exercise
        (methodInfo : MethodInfo)
        (owner)
        (parameters : ParameterInfo list) =
        let keys =
            parameters
            |> Seq.map (fun p -> p.ParameterType)
            |> Seq.toArray

        let tuple =
            Type.GetType("System.Tuple`" + keys.Length.ToString())
                .MakeGenericType(keys);

        try
            Assembly
                .GetExecutingAssembly()
                .GetType("Ploeh.AutoFixture.Idioms.FsCheck.FsCheckInvoker")
                .GetMethod(
                    "Invoke",
                    BindingFlags.Static ||| BindingFlags.NonPublic)
                .MakeGenericMethod(tuple)
                .Invoke(null, [| methodInfo; owner |]);
        with
        | e -> raise <| ReturnValueMustNotBeNullException(
                "The method "
                + methodInfo.Name
                + " returns null which is never an acceptable return"
                + " value for a public Query (method that returns a value)."
                + Environment.NewLine
                + e.InnerException.Message)