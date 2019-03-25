// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open LearningThreadModule

open System.IO
open ReferenceModule
open ObservationDataModule
open PatternMatchingModule

type SimpleRecommender(live:bool, date:DateTime) = 
    let parse_date(dte:string) = 
        let mutable date_read = new DateTime(2020,1,1)
        DateTime.TryParse(dte,&date_read) |> ignore
        date_read
    let mutable orderizedOptims = []
    do
        let mutable normalizedOptims = []    
        let mutable data = []
        let mutable forward_data = []
        let mutable prediction_accuracy = []
        if live then 
            data <- ReadAndParseFile(LiveDataSet())
        else
            let file_name = FileName(date)
            let full_data = ReadAndParseFile(new FileInfo(String.Format(@"{0}\{1}",minute_data,file_name)))
            let mutable date_is_less_than = true
            let mutable index = 1
            while date_is_less_than do
                if (parse_date(full_data.[index].[0]) < date) then 
                    index <- index + 1
                else
                    date_is_less_than <- false
            data <- full_data.[0]::[for x in [index..Math.Min(index + BackDataLength, full_data.Length - 1)] do yield full_data.[x]]
            let start_forward_date = parse_date(data.[data.Length - 1].[0])
            let forward_data_full = ReadAndParseFile(new FileInfo(String.Format(@"{0}\Forward\{1}",averages_data,FileName(start_forward_date))))
            index <- 1
            date_is_less_than <- true
            while date_is_less_than do
                if (parse_date(forward_data_full.[index].[0]) < date) then 
                    index <- index + 1
                else
                    date_is_less_than <- false
            forward_data <- [for str in forward_data_full.[index] do yield str]
        let exchs = [for dt in data.[0] do yield dt]
        let latestRow = data.[data.Length - 1]
        let prices = [for x in [1..exchs.Length - 1] do yield (exchs.[x], latestRow.[x])]
        let patternMatch = PatternMatching(DateTime.Now, data)
        for (ex,opti) in patternMatch.Optimizations do 
            if (Double.IsNaN(opti) || Double.IsInfinity(opti)) = false then
                let mutable prc = 0.
                if Double.TryParse((latestRow.[exchs |> List.findIndex(fun (exc) -> ex = exc)]), &prc) then
                    normalizedOptims <- (ex, opti / prc)::normalizedOptims
                    if false = live then 
                        prediction_accuracy <- (ex, abs(float(forward_data |> List.findIndex(fun (exc) -> ex = exc)) - opti) / prc)::prediction_accuracy
            else
                if false = live then 
                    prediction_accuracy <- (ex, 0.)::prediction_accuracy
        orderizedOptims <- (normalizedOptims |> List.sortByDescending(fun (ex,opPr) -> abs(opPr)))
        let prediction_files = [for fle in (new DirectoryInfo(prediction_accuracy_folder)).GetFiles() do yield fle]
        if false = live then 
            if (prediction_files |> List.exists(fun fle -> fle.Name = FileName(date))) then 
                File.AppendAllText(prediction_accuracy_folder + String.Format(@"\{0}",FileName(date)),"\n" + exchs.[0]+","+String.Join(",",prediction_accuracy))
            else
                File.WriteAllText(prediction_accuracy_folder + String.Format(@"\{0}",FileName(date)),String.Join(",",exchs))
                File.AppendAllText(prediction_accuracy_folder + String.Format(@"\{0}",FileName(date)),"\n" + exchs.[0]+","+String.Join(",",prediction_accuracy))
    member this.SimpleSelection = orderizedOptims.[0..Math.Min(3,orderizedOptims.Length - 1)]
[<EntryPoint>]
let main argv = 
    learn_a_year()
    Console.WriteLine("CHECK THAT THE FILES ARE COMPLETED BEFORE Pressing any key to continue");
    let foo = Console.ReadKey()
    0 // return an integer exit code

