module LearningThreadModule
open System
open PatternMatchingModule
open ObservationDataModule
open ReferenceModule
open DataCollectionModule
open StatisticsComputationModule

open System.Threading.Tasks
let year = 2005
let process_a_year() = 
    for month in [1..12] do
        let aTask = new Task(fun() -> 
            let AvgComp = AveragesComputation(new DateTime(year,month,1))
            let DevComp = DeviationsComputation(new DateTime(year,month,1))
            let CorrComp = CorrelationsComputation(new DateTime(year,month,1))
            Console.WriteLine("Sample Complete for {0}", new DateTime(year,month,1)) 
        )
        aTask.Start()
        Console.WriteLine("Task {0} created!", month)
    let mutable resp = "N"
    while resp.ToString().ToUpper() <> "Y" do        
        Console.WriteLine("Check to see that all analysis files are created...")
        Console.WriteLine("Are all files created? (Y/N)")
        resp <- Console.ReadLine()

let learn_a_year() = 
    for month in [1..12] do               
        DataCollection(new DateTime(year,month,1)) |> ignore
        Console.WriteLine("Data {0}/{1} collected!", month,year)
    //for month in [1..12] do               
    //    DataCollection(new DateTime(year+1,month,1)) |> ignore
    //    Console.WriteLine("Data {0}/{1} collected!", month,year+1)
    