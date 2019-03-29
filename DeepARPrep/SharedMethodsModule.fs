module SharedMethodsModule
open System
open SimbaForex2.Models.OandaModel
open System.IO
open ReferenceModule

let MarginRatio(exchange) = 
    if ["EUR_USD";"USD_CAD"] |> List.contains(exchange) then 
        0.02
    else if exchange = "AUD_USD" then 
        0.03
    else if exchange = "USD_JPY" then 
        0.04
    else if exchange = "GBP_USD" then 
        0.05
    else
        0.07
let normalized(cndle:Candlestick) = 
    (float(cndle.mid.h + cndle.mid.l) + 2. * float(cndle.mid.o) + 2. * float(cndle.mid.c)) / 6.
let normalizedDataSet(candles:System.Collections.Generic.List<Candlestick>) = 
    [for cndle in candles do yield normalized(cndle)]
let FileName(date:DateTime) = 
    String.Format("{0}_{1}.csv", date.Year,date.Month)
let ReadAndParseFile(file:FileInfo)= 
    use strm = file.OpenText()
    let header = strm.ReadLine().Split(',')
    let mutable row = strm.ReadLine().Split(',')
    let mutable resultant = [row]
    let mutable cancellation = false
    while cancellation <> true do
        let str = strm.ReadLine()
        if str <> null then 
            resultant<-str.Split(',')::resultant
        else
           cancellation <- true
    resultant <- header::(resultant |> List.sortBy(fun row -> Convert.ToDateTime(row.[0])))
    resultant
let ContinueIteration(ctr,whole_file_length) = 
    ctr + BackDataLength + DataIncrement + ForwardDataLength < whole_file_length
let AbsoluteDiff(diff_type: string, num:int) = 
    if diff_type = "Month" then 
        if abs(num) > 6 then 
            12 - num
        else
            num
    else
        if abs(num) > 15 then 
            31 - num
        else
            num
let ApplySort(date:DateTime,lstlst:List<string[]>,inner) = 
    if inner then 
        lstlst.[0]::(lstlst.[1..lstlst.Length - 1] |> List.sortByDescending(fun (lst:string[]) -> DateTime.Parse(lst.[0]) |> fun (chi) -> HourImportance / float(abs((chi - date).Hours)) + DayImportance / float(abs((chi - date).Days))))
    else
        lstlst.[0]::(lstlst.[1..lstlst.Length - 1] |> List.sortByDescending(fun (lst:string[]) -> DateTime.Parse(lst.[0]) |> fun (chi) -> MonthImportance / float(abs(AbsoluteDiff("Month", chi.Month - date.Month))) + YearImportance / float(abs(chi.Year - date.Year))))

let RandGen(size) = 
        let rand = System.Random()
        let mutable ctr = 1.
        let mutable index_set = [0]
        while ctr < size do
            let prev = int(ctr)
            index_set <- prev::index_set
            let fu = rand.NextDouble()
            ctr <- (4./3. + float(fu)) * ctr + 1.
            if int(ctr) <= prev then 
                ctr <- float(prev + 1)
        [0..index_set.Length]
