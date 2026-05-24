open System.IO
open System.IO.Compression
let zip = Path.Combine(Directory.GetCurrentDirectory(), "deploy.zip")
if File.Exists(zip) then File.Delete(zip)
ZipFile.CreateFromDirectory("publish", zip)
printfn "Created: %s (%d bytes)" zip (FileInfo(zip).Length)
