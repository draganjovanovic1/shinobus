namespace Mocosha.Shinobus

module EventBus =
    open System
    open System.Threading

    type EventOf<'a> = { Id : Guid; OccurredAt : DateTime; Data : 'a }

    type EventOf with
        static member Create data =
            { Id = Guid.NewGuid(); OccurredAt = DateTime.Now; Data = data }

    let private tryCast<'a> (event : EventOf<obj>) =
        match event.Data with
        | :? 'a as a -> Some { Id = event.Id; OccurredAt = event.OccurredAt; Data = a }
        | _ -> None

    let private tryCastStrict<'a> (event : EventOf<obj>) =
        match event.Data.GetType() = typeof<'a> with
        | true ->
            let data = event.Data :?> 'a
            { Id = event.Id; OccurredAt = event.OccurredAt; Data = data } |> Some
        | false ->
            None

    let private downcastEvent event =
        let data = downcast (event.Data :> obj)
        { Id = event.Id; OccurredAt = event.OccurredAt; Data = data }
        
    type Bus private (syncContext : SynchronizationContext option) =
        let stream = new Event<EventOf<obj>>()

        let reportEvent event =
            match syncContext with
            | None ->
                stream.Trigger event
            | Some ctx ->
                ctx.Post((fun _ -> stream.Trigger event), null)

        let eventPublisher = MailboxProcessor.Start(fun inbox ->
            let rec loop() = async {
                try
                    let! event = inbox.Receive()
                    reportEvent event
                with
                    | ex -> ()
                return! loop()
            }
            loop ()
        )
        
        new (synchronizationContext) = Bus(Some synchronizationContext)
        
        new () = Bus(None)
        
        member x.Publish event =
            event 
            |> downcastEvent
            |> eventPublisher.Post

        member x.EventStream<'a> ()=
            stream.Publish
            |> Observable.map (fun e -> tryCast<'a> e)
            |> Observable.filter (fun e -> e.IsSome)
            |> Observable.map (fun e -> e.Value)