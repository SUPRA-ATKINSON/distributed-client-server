module Client

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

let establishConnection (ipAddress : IPAddress) (port : int) =
    async {
        let client = new TcpClient()
        do! client.ConnectAsync(ipAddress, port) |> Async.AwaitTask

        use networkStream = client.GetStream()
        use reader = new StreamReader(networkStream)
        use writer = new StreamWriter(networkStream)

        let rec readAndPrint () =
            let message = Console.ReadLine()
            if message = "bye" then
                return false
            elif message = "terminate" then
                writer.WriteLine(message)
                writer.Flush()
                return false
            else
                writer.WriteLine(message)
                writer.Flush()

            // Read and print the server's response
            let response = reader.ReadLine()
            Console.WriteLine("Server response: " + response)

            return true

        let mutable keepRunning = true
        while keepRunning do
            keepRunning <- readAndPrint ()

        client.Close()
        printfn "Connection closed."
    }

let main argv =
    let ipAddress = IPAddress.Parse("127.0.0.1") 
    let port = 35627 

    printfn "Establishing connection..."
    Async.RunSynchronously (establishConnection ipAddress port)
    0