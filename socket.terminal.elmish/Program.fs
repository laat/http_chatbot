﻿module Counter

open Terminal.Gui.Elmish
open System.Net
open Terminal.Gui
open ConnectionController
open System

let server = Server.server 
// The model with the server object, not very immutable is it... 
type Model = {
    Connections: IPEndPoint list 
    SelectedItem: IPEndPoint option
//    Server: MailboxProcessor<ConnectionController.ConnectionMsg>
}

type Msg =
    | ConnectionEstablished of IPEndPoint 
    | Tick 

module Commands =
    let listenForConnection =
        fun dispatch ->
            async {
                do! Async.Sleep 20
                let reply = server.PostAndReply(fun channel -> (GetNewConnection channel))
                match reply with 
                    | Some (client, iPEndPoint) -> dispatch (ConnectionEstablished iPEndPoint) 
                    | None -> dispatch (Tick)
            }
            |> Async.StartImmediate
        |> Cmd.ofSub

let init () : Model * Cmd<Msg> =
    let model = {
        Connections = [] 
        SelectedItem = None 
    }
    model, Commands.listenForConnection 

let update (msg:Msg) (model:Model) =
    match msg with 
        | Tick -> model, Commands.listenForConnection
        | ConnectionEstablished conn ->  { model with Connections = conn :: model.Connections }, Commands.listenForConnection

let getSelectedItem (items: IPEndPoint list) (selectedItem: IPEndPoint option): int =
    match selectedItem with 
        | None -> 0
        | Some c -> (items |> List.findIndex ( fun c -> c = c))

let view (model:Model) (dispatch:Msg->unit) =
    View.page [
//        page.menuBar [ ]
        View.window [
            window.title "hey"
            window.children [
                View.listView [
                    prop.position.x.at 0
                    prop.position.y.at 0
                    prop.width.filled
                    prop.height.filled
                    listView.selectedItem (getSelectedItem model.Connections model.SelectedItem)
                    listView.source (model.Connections |> List.map (fun x -> sprintf "%A:%A" x.Address x.Port))
                ]
                View.label [
                    prop.position.x.center
                    prop.position.y.at 1
                    prop.textAlignment.left
                    prop.color (Color.BrightYellow, Color.Green)
                    label.text $"hei {model.Connections.Length}" 
                ] 
            ] 
        ]
    ]

// Should to the "program with subscription thing here to wire in the servercommand-thingy
Program.mkProgram init update view  
|> Program.run

