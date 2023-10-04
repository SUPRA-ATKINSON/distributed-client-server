module Client

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

let connectToServer (ipAddress : IPAddress) (port : int) =
    async {
        let client = new TcpClient()
        do! client.ConnectAsync(ipAddress, port) |> Async.AwaitTask

        use networkStream = client.GetStream()
        use reader = new StreamReader(networkStream)
        use writer = new StreamWriter(networkStream)

        let mutable continueReading = true

        while continueReading do
            let message = Console.ReadLine()
            if message = "bye" then
                continueReading <- false
            elif message = "terminate" then
                writer.WriteLine(message)
                writer.Flush()
                continueReading <- false
            else
                writer.WriteLine(message)
                writer.Flush()
                let response = reader.ReadLine()
                Console.WriteLine("Server response: " + response)
        client.Close()
        printfn "Connection closed."
    }

let main argv =
    let ipAddress = IPAddress.Parse("127.0.0.1") // Replace with the server's IP address
    let port = 35627 // Replace with the server's port number

    printfn "Connecting to server..."
    Async.RunSynchronously (connectToServer ipAddress port)
    0
