module Extractor

open System.IO
open System.IO.Compression
open System.Text.RegularExpressions
open System.Collections.Generic
open FSharp.Data
open CmdLineParams

type SessionId = string
type FileType = | Request | Response
type ExtractedParts = {Id: SessionId; FirstLine:string; Headers : string; Body:string}
type State = {Id: SessionId; ReqResult: ExtractedParts option; RespResult: ExtractedParts option}
type RulesJson = JsonProvider<""" [{"appliesTo":[".*"],"replaces":[{ "f":"abc", "t":"def"}]}] """>
type FromTo = { F:Regex; T:string}
type Rules = { AppliesTo:Regex array; Replaces:FromTo array}
type Options = {Rules:Rules array; CmdLineOptions:CommandLineOptions}

let (|SazMatch|_|) input =
   let m = Regex.Match(input, "raw/(\\d+)_(c|s).txt") 
   match m.Success with
   | true when m.Groups.[2].Captures.[0].Value = "c" -> 
        Some (m.Groups.[1].Captures.[0].Value, Request)
   | true when m.Groups.[2].Captures.[0].Value = "s" -> 
        Some (m.Groups.[1].Captures.[0].Value, Response) 
   | _ -> None 

let dict = new Dictionary<SessionId, State>() 

let combineAndOutput (state:State) (options:Options) =
    let req = state.ReqResult.Value
    let resp = state.RespResult.Value
    
    let applicableReplaces =
        options.Rules
        |> Array.filter (fun rule -> rule.AppliesTo |> Array.exists (fun appliesTo -> appliesTo.IsMatch(req.FirstLine)))
        |> Array.collect (fun rule -> rule.Replaces)

    let replace startText =
        applicableReplaces
        |> Array.fold (fun acc replace -> replace.F.Replace(acc, replace.T)) startText

    let sb = System.Text.StringBuilder()
    let appendToSB text = sb.AppendLine(text) |> ignore
    let replaceAndAppendToSB text = replace text |> appendToSB
    let appendSection title includeHeaders src =    
        appendToSB title
        replaceAndAppendToSB src.FirstLine
        if includeHeaders then replaceAndAppendToSB src.Headers
        appendToSB ""
        replaceAndAppendToSB src.Body

    appendSection "===REQUEST START===" (options.CmdLineOptions.requestHeaders = IncludeRequestHeaders) req
    appendSection "===RESPONSE START===" (options.CmdLineOptions.responseHeaders = IncludeResponseHeaders) resp

    let opFileName = "session-" + req.Id+".txt"
    let opFile = System.IO.Path.Combine(options.CmdLineOptions.outputFolder, opFileName)
    System.IO.File.WriteAllText(opFile, sb.ToString())

    printf "%s\r\n" opFileName
    ()

let modifyDictAndOutput sessionId options handler =
    let (exists, origState) = dict.TryGetValue sessionId 
    if exists then dict.Remove(sessionId) |> ignore 
    let newState = handler (exists, origState)
    dict.Add(sessionId, newState)  

    match (newState.ReqResult, newState.RespResult) with
    | Some(req), Some(resp) -> combineAndOutput newState options
    | _, _ -> ()
      
let handleRequest (reqResult:ExtractedParts) (exists, origState) =
    if exists then {origState with ReqResult = Some reqResult} 
    else {Id=reqResult.Id; ReqResult = Some reqResult; RespResult = None}
    
let handleResponse (respResult:ExtractedParts) (exists, origState) =    
    if exists then {origState with RespResult = Some respResult} 
    else {Id=respResult.Id; ReqResult = None; RespResult = Some respResult}

let lines (sr:StreamReader) = 
    seq {
        while not sr.EndOfStream do
            yield sr.ReadLine()
    }

let readHeaders sr = 
    lines sr
    |> Seq.takeWhile (fun (x:string) -> x.Length > 0) 
    |> String.concat "\r\n" 

let extract sessionId options handler (entry:ZipArchiveEntry) =
    use stream = entry.Open()
    use sr = new StreamReader(stream, System.Text.Encoding.UTF8)
    let extracted = {Id = sessionId; FirstLine=sr.ReadLine(); Headers=(readHeaders sr); Body=sr.ReadToEnd()}
    modifyDictAndOutput sessionId options (handler extracted)

let processEntry options (entry:ZipArchiveEntry) =
    match entry.FullName with
    | SazMatch (sessionId, fileType) ->
        match fileType with
        | Request -> extract sessionId options handleRequest entry
        | Response -> extract sessionId options handleResponse entry
    | _ -> ()
    
let extractRules (rulesFile:string) = 
    RulesJson.Load(rulesFile)
    |> Array.map (fun rule -> 
        let appliesTo = rule.AppliesTo |> Array.map (fun x -> Regex(x, RegexOptions.Compiled ||| RegexOptions.IgnoreCase))
        let replaces = rule.Replaces |> Array.map (fun x -> 
            {F=Regex(x.F, RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline); T = x.T})
        {AppliesTo=appliesTo; Replaces = replaces})
    
let entry cmdLineOptions = 
    let rules = extractRules cmdLineOptions.rulesFile
    let opts = {Rules = rules; CmdLineOptions=cmdLineOptions}

    let opDir = System.IO.Directory.CreateDirectory(cmdLineOptions.outputFolder)
    System.IO.Directory.GetFiles(cmdLineOptions.outputFolder) |> Seq.iter File.Delete

    use file = File.OpenRead(cmdLineOptions.inputFile)
    use zip = new ZipArchive(file, ZipArchiveMode.Read)

    zip.Entries |> Seq.iter (processEntry opts)



