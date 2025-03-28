namespace SparkNET.Session
{
    public class SessionModel
    {
        public string? id { get; set; }
        public string? device { get; set; }
        public string? app { get; set; }
        public string? ip { get; set; }
        public DateTime? created { get; set; }
        public DateTime? updated { get; set; }
        public DateTime? expires { get; set; }
    }
}
