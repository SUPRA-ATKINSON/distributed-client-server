module Server

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

// Define a function to perform arithmetic operations
let calculate (command: string) (numbers: int list) =
    match command with
    | "add" -> List.sum numbers
    | "subtract" -> List.reduce (-) numbers
    | "multiply" -> List.reduce (*) numbers
    | _ -> 0 // Handle invalid command

let handleClient (clientSocket : Socket) =
    async {
        let clientEndPoint = clientSocket.RemoteEndPoint.ToString()
        let buffer = Array.zeroCreate 1024
        let networkStream = new NetworkStream(clientSocket)
        use reader = new StreamReader(networkStream)
        use writer = new StreamWriter(networkStream)

        Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint.ToString())

        // Send a welcome message to the client
        writer.WriteLine("Hello!")
        writer.Flush()

        let rec processStream () =
            async {
                let! data = reader.ReadLineAsync() |> Async.AwaitTask
                if not (String.IsNullOrEmpty(data)) then
                    Console.WriteLine("Received: " + data)
                    let inputs = data.Split(' ')
                    if inputs.Length >= 3 && inputs.Length <= 5 then
                        let command = inputs.[0]
                        let numbers = Array.map Int32.Parse (Array.sub inputs 1 (inputs.Length - 1))
                        let result = calculate command (Array.toList numbers)
                        writer.WriteLine(result)
                        writer.Flush()
                    else
                        writer.WriteLine("Invalid command. Usage: add/subtract/multiply number1 number2 ...")
                        writer.Flush()
                    return! processStream ()
                else
                    networkStream.Close()
                    clientSocket.Close()
                    Console.WriteLine("Client disconnected: " + clientEndPoint)
            }
        
        do! processStream ()
    }

let main argv =
    let ipAddress = IPAddress.Parse("127.0.0.1")
    let port = 35627
    let serverEndPoint = IPEndPoint(ipAddress, port)

    let serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    serverSocket.Bind(serverEndPoint)
    serverSocket.Listen(5)

    Console.WriteLine("Server is running and listening on port " + port.ToString())

    let rec acceptClients () =
        async {
            try
                let! clientSocket = Async.AwaitTask(serverSocket.AcceptAsync())
                Async.Start(handleClient clientSocket)
                return! acceptClients ()
            with
            | :? SocketException as ex ->
                Console.WriteLine("SocketException: " + ex.Message)
            | :? ObjectDisposedException as ex ->
                Console.WriteLine("ObjectDisposedException: " + ex.Message)
            | ex ->
                Console.WriteLine("Exception: " + ex.Message)
                return! acceptClients ()
        }

    Async.RunSynchronously(acceptClients ())
    0