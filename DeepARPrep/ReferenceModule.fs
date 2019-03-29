module ReferenceModule
open System
open System.IO
open SimbaForex2
open SimbaForex2.Models.OandaModel

let access_token = "fb298328abcc279ecd6f98131c8abcf3-a308bc81ad8b28d14b6e594db4dda6df"
let account_id = "001-001-895063-001"
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

let measuring_root = @"C:\JonesOnie\CSV\Measuring"
let order_ledger_file = measuring_root + @"\OrderLedger.csv"
let account_info_file = measuring_root + @"\AccountInfo.csv"

let DataIncrement = 5

let BackDataLength = 120
let ForwardDataLength = 60
let epsilon = 0.01
let HourImportance = 9.
let DayImportance = 5.
let MonthImportance = 2.
let YearImportance = 1.
let exchanges = 
    let instruments = OandaConn.GetInstruments(access_tuple).instruments
    //([for instr in instruments do yield instr.name]) //|> List.where(fun ex -> false = (not_interesting |> List.contains(ex))))//.[0..5]
    ["EUR_USD"; "USD_JPY"; "GBP_USD"; "AUD_USD"; "USD_CAD"]
