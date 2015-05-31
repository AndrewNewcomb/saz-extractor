module CmdLineParams
    // inspired by  http://fsharpforfunandprofit.com/posts/pattern-matching-command-line/

    type RequestHeadersOption = IncludeRequestHeaders | ExcludeRequestHeaders
    type ResponseHeadersOption = IncludeResponseHeaders | ExcludeResponseHeaders
    type UsageOption = ShowUsage | DoNotShowUsage

    type CommandLineOptions = {
        inputFile: string;
        outputFolder: string;
        rulesFile: string;
        requestHeaders: RequestHeadersOption;
        responseHeaders: ResponseHeadersOption;
        usageOption: UsageOption
        }

    let showUsage =
        printfn """Purpose:"""
        printfn """    Processes a Fiddler .saz archive matching the corresponding requests and responses for each session outputting them to a file per session. Regular expresssions can be used to modify the results."""
        printfn """"""
        printfn """Usage:"""
        printfn """    inputSazFile"""        
        printfn """    /o outputFolder"""                
        printfn """    /r jsonRulesFile"""
        printfn """    /reqh"""
        printfn """    /resph"""
        printfn """    /h or /?"""
        printfn ""
        printfn """Where:"""
        printfn """    /o outputFolder - The output folder defaults to a child folder in the same folder as the inputSazFile, but with a ".extracted" extension. An input file of "c:\temp\input.saz" would give folder "c:\temp\input.saz.extracted". The output folder will be created if it does not exits. If the output folder already exists any files in it will be deleted."""
        printfn """    /r jsonRulesFile - The json rules file defaults to a file the same name as the inputSazFile, but with a ".json extension". An input file of "c:\temp\input.saz" would give "c:\temp\input.saz.json"."""
        printfn """    /reqh - will include the request headers in the output."""
        printfn """    /resph - will include the response headers in the output."""
        printfn """    /h or /? will show this usage information."""
        printfn ""
        printfn """Note:"""
        printfn """    Before saving the fiddler sessions expand gzipped responses via Fiddler menu option Rules, Remove All Encodings"""
        printfn ""
        

    // create the "helper" recursive function
    let rec parseCommandLineRec args optionsSoFar = 
        match args with 
        // empty list means we're done.
        | [] -> 
            optionsSoFar  
            
        // match output flag
        | "/o"::xs -> 
            match xs with
            | h::t -> 
                let newOptionsSoFar = { optionsSoFar with outputFolder=h}
                parseCommandLineRec t newOptionsSoFar 
            | _ -> 
                parseCommandLineRec xs optionsSoFar 

        // match output flag
        | "/r"::xs -> 
            match xs with
            | h::t -> 
                let newOptionsSoFar = { optionsSoFar with rulesFile=h}
                parseCommandLineRec t newOptionsSoFar 
            | _ -> 
                parseCommandLineRec xs optionsSoFar 
    
        // match request header flag
        | "/reqh"::xs -> 
            let newOptionsSoFar = { optionsSoFar with requestHeaders=IncludeRequestHeaders}
            parseCommandLineRec xs newOptionsSoFar 
            
        // match response header flag
        | "/resph"::xs -> 
            let newOptionsSoFar = { optionsSoFar with responseHeaders=IncludeResponseHeaders}
            parseCommandLineRec xs newOptionsSoFar 

        // match response header flag
        | "/h"::xs 
        | "/?"::xs -> 
            let newOptionsSoFar = { optionsSoFar with usageOption=ShowUsage}
            parseCommandLineRec xs newOptionsSoFar 

        // handle unrecognized option and keep looping
        | x::xs -> 
            printfn "Option '%s' is unrecognized" x
            parseCommandLineRec xs optionsSoFar 

    // create the "public" parse function
    let parseCommandLine (args: string array) = 
        let inputFile = if args.Length > 0 then args.[0] else ""
        

        // create the defaults
        let defaultOptions = match args.Length with
        | 0 ->  
            {
            inputFile = "";
            outputFolder = "";
            rulesFile = "";
            requestHeaders = ExcludeRequestHeaders;
            responseHeaders = ExcludeResponseHeaders;
            usageOption = DoNotShowUsage
            }
        | _ ->  
            {
            inputFile = inputFile;
            outputFolder = inputFile + ".extracted";
            rulesFile = inputFile + ".json";
            requestHeaders = ExcludeRequestHeaders;
            responseHeaders = ExcludeResponseHeaders;
            usageOption = DoNotShowUsage
            }

        // call the recursive one with the initial options
        parseCommandLineRec (List.ofArray args).Tail defaultOptions 


  
