module ReferenceModule
open System
open System.IO
open SimbaForex2
open SimbaForex2.Models.OandaModel

let access_token = "<YOUUR ACCESS TOKEN>"
let account_id = "<YOUR ACCOUNT ID>"
let url = "https://api-fxtrade.oanda.com"
let access_tuple = Tuple.Create(url,account_id,access_token)
let minute_data = @"C:\JonesOnie\CSV\MinuteData"
let live_data_set = @"C:\JonesOnie\CSV\LiveDataSet.csv"

let statistics_root = @"C:\JonesOnie\CSV\Statistics"
let averages_data = statistics_root + @"\Averages"
let deviations_data = statistics_root + @"\Deviations"
let correlations_data = statistics_root + @"\Correlations"

let testing_root = @"C:\JonesOnie\CSV\Testing"
let prediction_accuracy_folder = testing_root + @"\PredictionAccuracy"

let DataIncrement = 5

let BackDataLength = 120
let ForwardDataLength = 60

let HourImportance = 9.
let DayImportance = 5.
let MonthImportance = 2.
let YearImportance = 1.
let exchanges = 
    let instruments = OandaConn.GetInstruments(access_tuple).instruments
    //([for instr in instruments do yield instr.name]) //|> List.where(fun ex -> false = (not_interesting |> List.contains(ex))))//.[0..5]
    ["AUD_JPY";"AUD_NZD";"AUD_USD";"EUR_AUD";"EUR_CAD";"EUR_GBP";"EUR_JPY";"EUR_USD";"GBP_AUD";"GBP_CAD";"GBP_JPY";"GBP_USD";"NZD_USD";"USD_CAD";"USD_JPY"]
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


        