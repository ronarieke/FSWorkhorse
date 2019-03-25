module PatternMatchingModule
open System
open System.IO
open ReferenceModule
open StatisticsComputationModule

type PatternMatching(date:DateTime, info:List<string[]>) = 
    let mutable InterestingSets : List<DateTime*List<string[]>*string[]*string[]*string[]> = []
    let mutable InterestDecay : List<DateTime*float> = []
    let RandGen(size) = 
        let rand = System.Random()
        let mutable ctr = 1.
        let mutable index_set = [0]
        while ctr < size do
            let prev = int(ctr)
            index_set <- prev::index_set
            ctr <- (1. + 4./3. * float(rand.Next())) * ctr
            if int(ctr) <= prev then 
                ctr <- float(prev + 1)
        index_set |> List.sort
    let CorrelateCompare(corrRow1:List<string>, corrRow2:List<string>) = 
        let mutable foo1 = 0.
        let mutable foo2 = 0.
        1. / ([for x in [1..corrRow1.Length - 1] do yield (corrRow1.[x], corrRow2.[x]) ] |> List.where(fun (c1,c2) -> Double.TryParse(c1,&foo1) && Double.TryParse(c2,&foo2) && float(c1) <> 0. && float(c2) <> 0. && false = Double.IsNaN(float(c1)) && false = Double.IsNaN(float(c2))) |> List.collect(fun (c1,c2) -> [abs(float(c1)-float(c2))]) |> List.sum)
    let ApplySort(date:DateTime,lstlst:List<string[]>,inner) = 
        if inner then 
            lstlst.[0]::(lstlst.[1..lstlst.Length - 1] |> List.sortByDescending(fun (lst:string[]) -> DateTime.Parse(lst.[0]) |> fun (chi) -> HourImportance / float(abs((chi - date).Hours)) + DayImportance / float(abs((chi - date).Days))))
        else
            lstlst.[0]::(lstlst.[1..lstlst.Length - 1] |> List.sortByDescending(fun (lst:string[]) -> DateTime.Parse(lst.[0]) |> fun (chi) -> MonthImportance / float(abs(chi.Month - date.Month)) + YearImportance / float(abs(chi.Year - date.Year))))

    let mutable optimizations = []
    do
        let dir = new DirectoryInfo(minute_data)
        let avgs_dir = new DirectoryInfo(averages_data + @"\Backward")
        let for_avgs_dir = new DirectoryInfo(deviations_data + @"\Forward")
        let corrs_dir = new DirectoryInfo(correlations_data + @"\Backward")

        let hour = date.Hour
        let month = date.Month
        let year = date.Year
        let dayOfWeek = int(date.DayOfWeek)
        
        let files = [for fle in dir.GetFiles() do yield fle] |> List.sortBy(fun fle -> (((fle.Name |> fun f -> (DateTime(int(f.Split('.').[0].Split('_').[0]),int(f.Split('.').[0].Split('_').[1]),1), DateTime(date.Year,date.Month,1)) |> fun (ta,tb) ->  MonthImportance / float(1. + float(abs(ta.Month - tb.Month))) + YearImportance / float(1. + float(abs(ta.Year - tb.Year)))))))
        let avgs = [for fle in avgs_dir.GetFiles() do yield fle] |> List.sortBy(fun fle -> (((fle.Name |> fun f -> (DateTime(int(f.Split('.').[0].Split('_').[0]),int(f.Split('.').[0].Split('_').[1]),1), DateTime(date.Year,date.Month,1)) |> fun (ta,tb) ->  MonthImportance / float(1. + float(abs(ta.Month - tb.Month))) + YearImportance / float(1. + float(abs(ta.Year - tb.Year)))))))
        let for_avgs = [for fle in for_avgs_dir.GetFiles() do yield fle] |> List.sortBy(fun fle -> (((fle.Name |> fun f -> (DateTime(int(f.Split('.').[0].Split('_').[0]),int(f.Split('.').[0].Split('_').[1]),1), DateTime(date.Year,date.Month,1)) |> fun (ta,tb) ->  MonthImportance / float(1. + float(abs(ta.Month - tb.Month))) + YearImportance / float(1. + float(abs(ta.Year - tb.Year)))))))
        let corrs = [for fle in corrs_dir.GetFiles() do yield fle] |> List.sortBy(fun fle -> (((fle.Name |> fun f -> (DateTime(int(f.Split('.').[0].Split('_').[0]),int(f.Split('.').[0].Split('_').[1]),1), DateTime(date.Year,date.Month,1)) |> fun (ta,tb) ->  MonthImportance / float(1. + float(abs(ta.Month - tb.Month))) + YearImportance / float(1. + float(abs(ta.Year - tb.Year)))))))
        let trained = [ for x in RandGen(float(files.Length - 1)) do yield [for y in RandGen(float(int(avgs.[x].Length) - 1)).Tail do yield ApplySort(date, ReadAndParseFile(avgs.[x]), true) |> fun avg -> (avg.[y], ApplySort(date,ReadAndParseFile(for_avgs.[x]), true).[y], ApplySort(date, ReadAndParseFile(corrs.[x]), true).[y],(ReadAndParseFile(files.[x]) |> fun (kay:List<string[]>) -> kay.[1..kay.Length - 1] |> List.findIndex(fun key -> key.[0] = avg.[y].[0]) |> fun ind -> kay.[ind - BackDataLength..ind]))]]
        let single_exchanges = "Exchanges"::exchanges
        let header = "CorrelationPairs"::([for ex in single_exchanges.Tail do yield [ for ex2 in single_exchanges.Tail do yield ex+"&"+ex2]] |> List.concat)
        
        let observed = [ for e in [1..info.[0].Length - 1] do yield (info.[0].[e],[ for x in [1..info.Length - 1] do yield (Convert.ToDateTime(info.[x].[0]), float(info.[x].[e]))])]
        
        let avg_data = [for (instr,dat) in observed do yield (instr, dat, average_delta(dat))]
        let dev_data = [for (instr,dat,avg) in avg_data do yield (instr, dat, avg, deviation_delta(dat, avg))]
        let complete_data = [for (instr,dat,avg,dev) in dev_data do yield (instr,dat,avg,dev,[ for (instr2,dat2,avg2,dev2) in dev_data do yield (instr2,correlation_delta(dat,dat2,avg.ToString(),avg2.ToString(),dev.ToString(),dev2.ToString()))])]
        
        let CorrelationCompare(corrLst,corrLst2) = 
            let corrSum = ([for (instrI,corrI) in corrLst do yield Math.Abs(corrI - (corrLst2 |> List.find(fun (instrII,corrII) -> instrII = instrI) |> fun (i:string,c:float) -> c))] |> List.sum)
            corrSum
        // info is the observation that we are predicting for
        // trained is a tuple of string lists that we want to use for our optimization
        // trained = (random(averages), random(deviations), random(correlations)) list
        // to get this list, first we randomly generate some indices, based on the length of the 
            // 1.  File Count
            // 2. File's string row count
        // Then we use the Read and parse method to construct rows of strings, the first row contains the exchange names.

        // OBJECTIVE:  pass in to the optimization function the average rate of return and the forward rate of return for past data, 
            // along with the current reading's average rate of return, and finally, the index in the sorted array.
        // The index is sorted by the CorrelationCompare function, so I also need to compute the correlation array
        let CorrelationArray(instrument:string, strLst:string[]) : List<string*float> = 
            ([for x in [1..header.Length - 1] do yield (x,header.[x].Split('&')) ] |> List.where(fun (ind,splt) -> splt.[0]=instrument) |> List.collect(fun (ind,splt) -> [splt.[1],float(strLst.[ind])])) 
        //  Provided an array of training_data: List<string[]*string[]*string[]*string[] list>, I am interested in the last string array:
        let Ordered_Complete_Data = 
            // grab the correct correlation from the observation set
            let GrabCorrelationFor(instrument:string) = 
                (complete_data |> List.find(fun (instr,dat,avg,dev,corr) -> instr = instrument) |> fun (instr,dat,avg,dev,corr) -> corr)
            // sort training info on correlation comparison
            let stat_data(exch:string, stats:(string[]*string[]*string[]*List<string[]>)) = 
                let correlations = [ for ca in (stats |> fun (a,b,c,d) -> c) do yield ca]
                let avgFoo = ((stats |> fun (a:string[],b:string[],c:string[],d:List<string[]>) -> a).[single_exchanges |> List.findIndex(fun ex -> ex = exch)])
                let avg = float(avgFoo) 
                let dev = float((stats |> fun (a:string[],b:string[],c:string[],d:List<string[]>) -> b).[single_exchanges |> List.findIndex(fun ex -> ex = exch)])
                let corrs = ((stats |> fun (a:string[],b:string[],c:string[],d:List<string[]>) -> [for x in c do yield x]) |> List.where(fun c -> header.[(correlations |> List.findIndex(fun cx -> cx = c))].Split('&').[0] = exch ) |> List.collect(fun c -> [(header.[(correlations |> List.findIndex(fun cx -> cx = c))].Split('&').[1],float(c))]))
                (exch,avg,dev,corrs)
            let unindexed = [for x in [0..trained.Length - 1] do yield (x, ([ for y in [0..trained.[x].Length - 1] do yield (y,([ for exch in single_exchanges.Tail do yield (exch,stat_data(exch, trained.[x].[y]), CorrelationArray(exch,trained.[x].[y] |> fun(av,de,co,da) -> co))] |> List.sortByDescending(fun (exch,stats, value) -> CorrelationCompare(GrabCorrelationFor(exch),value)  )))] |> List.sortByDescending(fun (ind,arrAB) -> ([for (exch, value, corrs) in arrAB  do yield (corrs |> List.collect(fun (st,va) -> [va]) |> List.sum)] |> List.sum))))]
            let indexed = [for x in [0..unindexed.Length - 1] do yield [ for y in [0..(snd unindexed.[x]).Length - 1] do yield ((x + 1) * (y + 1), (snd (snd unindexed.[x]).[y]))]]
            indexed

        // with the sorting in place, we may compute the optimization and logistically regress the values based on significance of match on correlation values
        let optimization(f0:float,p0:float,p1:float,n:int) = 
            (Math.Log(float(n) + 2.) * Math.Sqrt(Math.Pow(f0-p0,2.)), p1 / (Math.Log(float(n) + 2.) * Math.Sqrt(Math.Pow(f0-p0,2.))))
        let InstrumentFactor(exch:string) = 
            (complete_data |> List.find(fun (exc,dat,avg,dev,corrLst) -> exc = exch) |> fun (e,da,av,de,co) -> av)
        let unStackedOptimizations = [for innerArr in Ordered_Complete_Data do yield [ for (logInd, iiLst) in innerArr do yield ([for (exch,(exch2,avg,forward,corr),corrInd) in iiLst do yield (exch,(optimization(InstrumentFactor(exch), avg,forward,logInd)))])]] 
        let denominatorFor(exchange) = 
            (unStackedOptimizations |> List.collect(fun innerArr -> [for slice in innerArr do yield [(snd (snd (slice |> List.find(fun (exh,va) -> exh = exchange))))] |> List.sum]) |> List.sum) 
        let numeratorFor(exchange) = 
            (unStackedOptimizations |> List.collect(fun innerArr -> [for slice in innerArr do yield [(fst (snd (slice |> List.find(fun (exh,va) -> exh = exchange))))] |> List.sum]) |> List.sum) 
        let stackedOptimizations = [for exch in single_exchanges.[1..single_exchanges.Length - 1] do yield exch, numeratorFor(exch)/denominatorFor(exch)]
        for (exchange, optii) in stackedOptimizations do
            Console.WriteLine("{0} Has optimization {1}", exchange, optii)
        optimizations <- stackedOptimizations
    member this.Optimizations = optimizations
