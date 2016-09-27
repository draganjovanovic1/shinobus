namespace Shinobus

module EventBus =
    open System
    open System.Threading

    let private tryCast<'a> (event : obj) =
        match event with
        | :? 'a as e -> Some e
        | _ -> None

    let private tryCastStrict<'a> event =
        match event.GetType() = typeof<'a> with
        | true -> tryCast<'a> event
        | false -> None

    let private downcastEvent event = downcast (event :> obj)
        
    type Bus private (syncContext : SynchronizationContext option) =
        let stream = new Event<_>()

        let reportEvent event =
            match syncContext with
            | None ->
                stream.Trigger event
            | Some ctx ->
                ctx.Post((fun _ -> stream.Trigger event), null)

        let eventPublisher = MailboxProcessor.Start (fun inbox ->
            let rec loop() = async {
                try
                    let! event = inbox.Receive ()
                    reportEvent event
                with
                    | ex -> ()
                return! loop()
            }
            loop ()
        )
        
        new (synchronizationContext) = Bus (Some synchronizationContext)
        
        new () = Bus (None)
        
        member x.Publish event =
            event 
            |> downcastEvent
            |> eventPublisher.Post

        member x.PublishWithDelay delay data =
            async {
                do! Async.Sleep delay
                x.Publish data
            } |> Async.StartImmediate

        member x.Stream<'a> ()=
            stream.Publish
            |> Observable.map (fun e -> tryCastStrict<'a> e)
            |> Observable.filter (fun e -> e.IsSome)
            |> Observable.map (fun e -> e.Value)