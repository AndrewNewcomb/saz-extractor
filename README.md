#Purpose
I wanted to compare web site requests from a development site with those from the corresponding live site to check for any differences. I used Fiddler2 to capture session traces from both sites but found that there was a lot of noise in each file, such as build versions, cache buster values, and http header differences. The code in this project takes a .saz file, extracts each request/response pair from it, applies a series of regular expressions, and saves the resulting files. A file comparison tool such as WinMerge can then be used to compare the files with a reference of files, highlighting any differences.

The code won't deal with compressed output, so before saving the fiddler sessions expand any gzipped responses via the Fiddler menu option `Rules`, `Remove All Encodings`.

The code won't yet deal with an encrypted .saz file.
Consider securing the output files as if the sessions contain sensitive data, such as cookies, or personally identifiable information then the data will appear in the output files (unless removed by a regex). 

#Usage
sazExtractor inputSazFile /o outputFolder /r jsonRulesFile

Where:
* inputSazFile - The saz file to be processed.
* /o outputFolder - The output folder defaults to a child folder in the same folder as the inputSazFile, but with a ".extracted" extension. An input file of "c:\temp\input.saz" would give folder "c:\temp\input.saz.extracted". The output folder will be created if it does not exits. If the output folder already exists any files in it will be deleted.
* /r jsonRulesFile - The json rules file defaults to a file the same name as the inputSazFile, but with a ".json extension". An input file of "c:\temp\input.saz" would give "c:\temp\input.saz.json".
* /reqh - will include the request headers in the output.
* /resph - will include the response headers in the output.
* /h or /? will show this usage information.

The json rules file takes the format
```
[
	{
		"appliesTo":[".*"],
		"replaces":[
		  { "f":"https?://[^/]+/", "t":"REMOVED/"},
		  { "f":"bld=%27[0-9a-z.]+%27", "t":"bld=REMOVED"}
		]
	},
	{
		"appliesTo":["^GET","^POST.*logger"],
		"replaces":[
		  { "f":"^.*(var options.*\\}\\};).*$", "t":"$1"}
		]
	}
]
```
where `appliesTo` is an array of regex patterns that are applied to the first line of the request. If any match the `replaces` are applied to the file. Each element in the `replaces` array has two properties, `f` is a regex pattern to search for, and  `t` contains the replacement to be used if the pattern is found. 
