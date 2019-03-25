module ObservationDataModule
open SimbaForex2
open ReferenceModule
open System.Text
open System
open System.IO

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