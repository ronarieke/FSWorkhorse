// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open LearningThreadModule
open ObservationDataModule

[<EntryPoint>]
let main argv = 
    //learn_a_year()
    //let RecSpin = SimpleRecommenderSpinner()
    process_a_year()
    Console.WriteLine("CHECK THAT THE FILES ARE COMPLETED BEFORE Pressing any key to continue");

    let foo = Console.ReadKey()
    0 // return an integer exit code

