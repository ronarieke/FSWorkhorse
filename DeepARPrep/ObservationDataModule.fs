module ObservationDataModule
open SimbaForex2
open ReferenceModule
open SharedMethodsModule
open System.Threading.Tasks
open System
open System.IO
open PatternMatchingModule
open SimbaForex2
open SimbaForex2.Models.OandaModel
let LiveDataSet() = 
    let candles = [for dte in [DateTime.Now] do yield (dte, [ for exchange in exchanges do yield (exchange, OandaConn.LiveDataCaptureNMany(access_tuple,exchange,BackDataLength).candles)])]
    let mutable st = "Time"
    for exchange in exchanges do
        st <- st+","+exchange
    File.WriteAllText(live_data_set,st)
    for (dte,candleSet) in candles do
        for x in [0..BackDataLength - 1] do
            let mutable ist = "\n" + (snd candleSet.[0]).[x].time
            for exchange in exchanges do
                try
                    ist <- ist + "," + normalized([for chi in (snd (candleSet |> List.find(fun (exch,cndS) -> exch = exchange))) do yield chi] |> List.find(fun cndle -> cndle.time = (snd candleSet.[0]).[x].time)).ToString()
                with
                    | e -> 
                        ist <- ist + ",0.000"   
            File.AppendAllText(live_data_set,ist)
    FileInfo(live_data_set)
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
        let patternMatch = PatternMatching(date, data)
        for (ex,opti) in patternMatch.Optimizations do 
            if (Double.IsNaN(opti) || Double.IsInfinity(opti)) = false then
                let mutable prc = 0.
                if Double.TryParse((latestRow.[exchs |> List.findIndex(fun (exc) -> ex = exc)]), &prc) then
                    normalizedOptims <- (ex, opti)::normalizedOptims
                    if false = live then 
                        prediction_accuracy <- (ex, abs(float(forward_data.[exchs |> List.findIndex(fun exa -> exa = ex)]) - opti))::prediction_accuracy
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
    member this.CompleteSelection = orderizedOptims
type SimpleRecommenderSpinner() = 
    let dateSets = [for fle in (new DirectoryInfo(String.Format("{0}\{1}",averages_data,"Forward"))).GetFiles() do yield (ReadAndParseFile(fle).Tail |> List.collect(fun row -> [DateTime.Parse(row.[0])])) ]
    do
        let thePastX = DateTime.Now
        for dateSet in dateSets do
            let tsk = new Task(fun () -> 
                for date in dateSet do
                    SimpleRecommender(false, date) |> ignore                    
            )
            tsk.Start()
        Console.WriteLine("Recommendation Process Complete! Press any key to continue...")

type ComplexRecommender() =
    member this.ImageAccuracy() =
        let date = DateTime.Now
        let dir = new DirectoryInfo(prediction_accuracy_folder)
        let files = ([for fle in dir.GetFiles() do yield fle] |> List.sortBy(fun fle -> (((fle.Name |> fun f -> (DateTime(int(f.Split('.').[0].Split('_').[0]),int(f.Split('.').[0].Split('_').[1]),1), DateTime(date.Year,date.Month,1)) |> fun (ta,tb) ->  MonthImportance / float(1. + float(abs(AbsoluteDiff("Month", ta.Month - tb.Month)))) + YearImportance / float(1. + float(abs(ta.Year - tb.Year))))))))
        let trained = [ for x in RandGen(float(files.Length - 1)) do yield (ApplySort(date, ReadAndParseFile(files.[x]), true) |> fun fle -> fle.[0]::[for y in RandGen(float(int(fle.Length) - 1)).Tail do yield fle.[y] ]) ]
        let single_exchanges = "Exchanges"::exchanges
        let Exchange_Accuracies = [for x in [1..single_exchanges.Length - 1] do yield (single_exchanges.[x], ([for trainer in trained do yield [for tRow in trainer do yield float(tRow.[x])] |> List.average] |> List.average))]
        let recommended = SimpleRecommender(true, date)
        let recommendation_clearance = [for (exch,optii) in recommended.CompleteSelection do yield (exch, optii, (optii + (if optii > 0. then -1. else 1.) * (snd (Exchange_Accuracies |> List.find(fun (excc,accuracy) -> excc = exch)))))]
        let orders_to_execute = (recommendation_clearance |> List.where(fun (ex,op,cl) -> (op > 0. && cl > 0.) || (op < 0. && cl < 0.)) |> fun res -> (if res.Length > 0 then (res |> List.sortBy(fun (ex,op,cl) -> abs(cl))).[0..Math.Min(res.Length - 1, 2)] else [("none available", 0., 0.)]))
        let closures_to_execute = (recommendation_clearance |> List.where(fun (ex,op,cl) -> (op < 0. && cl > 0.) || (op > 0. && cl < 0.)) |> fun res -> (if res.Length > 0 then res else [("none available", 0.,0.)]))
        (closures_to_execute, orders_to_execute)
type OrderExecutor() = 
    let PlaceLimitOrderFor(account:AccountSummary, exchange:string, optii:float, openOrClose:bool) = 
        let available_margin = float(account.marginAvailable)
        let leverage = MarginRatio(exchange)
        let fivePercentOfRemaining = int(0.025 * available_margin / leverage)
        let latestPrice = [for cndle in OandaConn.CandlesData(access_tuple, exchange, 60, "S5").candles do yield normalized(cndle) ]
        let latest = latestPrice.[latestPrice.Length - 1]
        let lastAverage = latestPrice |> List.average
        let price = 
            if false = openOrClose then 
                latest
            else
                if latest > lastAverage then 
                    if optii > 0. then 
                        latest + 2. * (lastAverage - latest)
                    else
                        lastAverage + (latest - lastAverage)
                else
                    if optii > 0. then 
                        lastAverage + (latest - lastAverage)
                    else
                        latest + 2. * (lastAverage - latest)
        let spinner = ((if optii > 0. then 1. else -1.) * (if openOrClose then 1. else -1.) = 1.)
        OandaConn.LimitOrder(access_tuple, exchange, spinner, fivePercentOfRemaining, decimal(Math.Round(price,(if exchange.Contains("JPY") then 2 else 5))))
        String.Join(",",[DateTime.Now.ToString(); exchange; optii.ToString(); spinner.ToString(); decimal(Math.Round(price,(if exchange.Contains("JPY") then 2 else 5))).ToString(); fivePercentOfRemaining.ToString()])
    //let (closures, recommendations) = ComplexRecommender().ImageAccuracy()
    let recommendation = SimpleRecommender(true,DateTime.Now).SimpleSelection
    do
        let account = OandaConn.AccountDetails(access_tuple).account
        let recommend = (recommendation |> List.sortBy(fun (e,o) -> -abs(o))).[0]
        Console.WriteLine("{0}: {1}... {2}", fst recommend, (if (snd recommend) > 0. then "BUY" else "SELL"), DateTime.Now)
        File.AppendAllText(order_ledger_file, PlaceLimitOrderFor(account, fst recommend, snd recommend, true))
        //if (recommendations.[0] |> fun (ex,op,cl) -> op = 0. && cl = 0.) then 
        //    Console.WriteLine("No Orders to initiate... checking to see if anything needs to close.")
        //else
        //    for (ex,op,cl)  in recommendations do
        //        File.WriteAllText(order_ledger_file, PlaceLimitOrderFor(account, ex,op,(op > 0.) = (cl > 0.)))
        //let positions = [for pos in account.positions do yield pos]
        //for (ex,op,cl) in closures do
        //    if (positions |> List.exists(fun (pos) -> pos.instrument = ex)) then 
        //        let units = (positions |> List.find(fun pos -> pos.instrument = ex)) |> fun pos -> float(int(pos.long.units) + int(pos.short.units))
        //        if (op > 0. && cl < 0. && units > 0.) || (op < 0. && cl > 0. && units < 0.) then 
        //            File.WriteAllText(order_ledger_file, PlaceLimitOrderFor(account, ex, op, (op > 0.) = (cl > 0.)))