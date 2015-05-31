open Extractor

[<EntryPoint>]
let main argv = 
    //printfn "%A" argv

    let cmdLineParams = CmdLineParams.parseCommandLine argv

    if cmdLineParams.usageOption = CmdLineParams.ShowUsage then
        CmdLineParams.showUsage 
    else
        printf "Output folder is: %s\r\n" cmdLineParams.outputFolder
        entry cmdLineParams

    //System.Console.ReadLine |> ignore
    0
