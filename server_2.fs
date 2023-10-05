module Server

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

type Socket = System.Net.Sockets.Socket
type IPEndPoint = System.Net.IPEndPoint

let addressFamily = AddressFamily.InterNetwork
let socketType = SocketType.Stream
let protocolType = ProtocolType.Tcp

let serverPort = 23456
let serverEndpoint = IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort)

let clientBufferSize = 1024
let clientTimeout = TimeSpan.FromSeconds(30)

let calculate (command: string) (numbers: int list) =
    match command with
    | "add" -> List.sum numbers
    | "subtract" -> List.reduce (-) numbers
    | "multiply" -> List.reduce (*) numbers
    | _ -> -1 // Handle incorrect operation command with error code -1

let sendHello (writer: StreamWriter) =
    Console.WriteLine("Sending Hello message...")
    let helloMsg = "Hello"
    writer.WriteLine(helloMsg)
    writer.Flush()


let parseInputs (inputs: string array) =
    try
        let numbers = Array.map Int32.Parse inputs
        if Array.length numbers = 1 then
            None, -2
        elif Array.length numbers >= 2 && Array.length numbers <= 4 then
            Some (Array.toList numbers), 0 // Return a list of numbers and error code 0 (no error)
        else
            None, -3 // Error code -3: number of inputs is more than four or less than two
    with
    | _ -> None, -4 // Error code -4: one or more of the inputs contain(s) non-number(s)


let handleClient (clientSocket: Socket) =
    async {
        let clientEndPoint = clientSocket.RemoteEndPoint.ToString()
        let networkStream = new NetworkStream(clientSocket)
        use reader = new StreamReader(networkStream)
        use writer = new StreamWriter(networkStream)

        Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint.ToString())

        sendHello writer

        let rec processStream () =
            async {
                try
                    let! data = reader.ReadLineAsync() |> Async.AwaitTask
                    if not (String.IsNullOrEmpty data) then
                        
                        Console.WriteLine("Received from " + clientSocket.RemoteEndPoint.ToString() + ": " + data)
                        if data = "bye" then
                            //Console.WriteLine("Client disconnected: " + clientEndPoint)
                            writer.WriteLine(-5) // Error code -5: Connection closed
                            writer.Flush()
                            return! processStream ()
                        else 
                            let inputs = data.Split(' ')
                            let command_init = inputs.[0]
                            let op_code = ["add"; "subtract"; "multiply"]
                            let contains_op = List.contains command_init op_code
                            if Array.length inputs = 1 then
                                writer.WriteLine(-1) // Error code -2: number of inputs is less than two
                                writer.Flush()
                                return! processStream ()
                            
                            elif not contains_op then
                                writer.WriteLine(-1)
                                writer.Flush()
                                return! processStream ()
                            
                            elif Array.length inputs >= 2 then
                                let command = inputs.[0]
                                let parsedInputs, errorCode = parseInputs (Array.sub inputs 1 (inputs.Length - 1))
                                match parsedInputs with
                                | Some numbers ->
                                    let result = calculate command numbers
                                    writer.WriteLine(result)
                                    writer.Flush()
                                    return! processStream ()
                                | None ->
                                    writer.WriteLine(errorCode) // Send error code to client
                                    writer.Flush()
                                    return! processStream ()
                            else
                                writer.WriteLine(-2) // Error code -2: number of inputs is less than two
                                writer.Flush()
                                return! processStream ()
                    else
                        networkStream.Close()
                        clientSocket.Close()
                        Console.WriteLine("Client disconnected: " + clientEndPoint)
                with
                | :? System.IO.IOException -> 
                    networkStream.Close()
                    clientSocket.Close()
                    Console.WriteLine("Client disconnected unexpectedly: " + clientEndPoint)
            }

        do! processStream ()
    }

let main argv =
    let ipAddress = IPAddress.Parse("127.0.0.1")
    let port = 23456
    let serverEndPoint = IPEndPoint(ipAddress, port)

    let serverSocket = new Socket(addressFamily, socketType, protocolType)
    serverSocket.Bind(serverEndPoint)
    serverSocket.Listen(5)

    Console.WriteLine("Server is running and listening on port " + port.ToString())

    let rec acceptClients () =
        async {
            try
                let! clientSocket = Async.AwaitTask(serverSocket.AcceptAsync())
                Async.Start(handleClient clientSocket) |> ignore
                return! acceptClients ()
            with
            | :? System.ObjectDisposedException -> return () // Server socket closed, stop accepting clients
        }

    Async.RunSynchronously (acceptClients ())
    Console.WriteLine("Server closed.")
    0
