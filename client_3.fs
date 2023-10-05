module Client

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO

let establishConnection (ipAddress: IPAddress) (port: int) =
    async {
        let client = new TcpClient()
        do! client.ConnectAsync(ipAddress, port) |> Async.AwaitTask

        use networkStream = client.GetStream()
        use reader = new StreamReader(networkStream)
        use writer = new StreamWriter(networkStream)

        // Read and print the initial server response (hello message)
        let helloMessage = reader.ReadLine()
        Console.WriteLine("Server says: " + helloMessage)

        let rec readAndPrint () =
            let message = Console.ReadLine()
            writer.WriteLine(message)
            writer.Flush()

            // Check for termination conditions
            if message = "bye" || message = "terminate" then
                // Read and print the server's response
                let response = reader.ReadLine()
                if response = "-5" then
                    Console.WriteLine("exit")
                else
                    Console.WriteLine("Need change")
                false
            else
                // Read and print the server's response
                let response = reader.ReadLine()
                let response_message = 
                    if response = "-1" then
                        "incorrect operation command"
                    elif response = "-2" then
                        "number of inputs is less than two"
                    elif response = "-3" then
                        "number of inputs is more than four"
                    elif response = "-4" then
                        "one or more of the inputs contain(s) non-number(s)"
                    else
                        response
                Console.WriteLine("Server response: " + response_message)
                true

        let mutable keepRunning = true
        while keepRunning do
            keepRunning <- readAndPrint ()

        client.Close()
        printfn "Connection closed."
    }

let main argv =
    let ipAddress = IPAddress.Parse("127.0.0.1") 
    let port = 23456 

    printfn "Establishing connection..."
    Async.RunSynchronously (establishConnection ipAddress port)
    0
