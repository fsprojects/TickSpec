module Hangman

let tally (word:string) guesses =
   guesses |> Seq.filter (fun c ->
      not (String.exists ((=) c) word)
   ) |> Seq.length

let toPartialWord (word:string) (guesses:char seq) =
   word |> String.map (fun c -> 
      if Seq.exists ((=) c) guesses then c else '_'
   )