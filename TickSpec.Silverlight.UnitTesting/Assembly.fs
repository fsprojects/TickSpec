namespace TickSpec

open TickSpec
open System.Reflection

module Assembly =
    let FindFeatures (assembly:Assembly) =
        let definitions = StepDefinitions(assembly)
        let sources =
            assembly.GetManifestResourceNames()
            |> Seq.filter (fun name -> 
                name.EndsWith(".txt") || 
                name.EndsWith(".story") ||
                name.EndsWith(".feature")
            )
        let namespaces  =
            assembly.GetTypes()
            |> Seq.map (fun t -> t.Namespace)
            |> Seq.distinct
            |> Seq.filter (fun t -> t <> null)
            |> Seq.sortBy(fun s -> - s.Length)
        seq {
            for source in sources do
                let stream = assembly.GetManifestResourceStream source
                let ns = namespaces |> Seq.tryFind(fun s -> s.StartsWith(s + "."))
                let filename = match ns with Some ns -> source.Substring(ns.Length+1) | None -> source
                yield definitions.GenerateFeature(filename,stream)
        }