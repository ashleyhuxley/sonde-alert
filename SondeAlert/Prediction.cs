namespace ElectricFox.SondeAlert
{
    public class Datum
    {
        public int time { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public double alt { get; set; }
    }

    public class Prediction
    {
        public string serial { get; set; }
        public string type { get; set; }
        public object subtype { get; set; }
        public DateTime datetime { get; set; }
        public List<double> position { get; set; }
        public double altitude { get; set; }
        public double ascent_rate { get; set; }
        public double descent_rate { get; set; }
        public double burst_altitude { get; set; }
        public bool descending { get; set; }
        public bool landed { get; set; }
        public List<Datum> data { get; set; }
    }
}
