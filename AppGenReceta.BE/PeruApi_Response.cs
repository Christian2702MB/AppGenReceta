namespace AppGenReceta.BE
{
    public class PeruApi_Response
    {
        public bool Success { get; set; }
        public string RazonSocial { get; set; }
        public string Message { get; set; }
    }

    public class PeruApi_RawData
    {
        public string ruc { get; set; }
        public string razon_social { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }
}
