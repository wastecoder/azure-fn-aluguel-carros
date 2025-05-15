namespace fnPayment.Model
{
    public class PaymentModel
    {
        public Guid id { get; set; }
        public Guid IdPaymant { get; set; }
        public string nome { get; set; }
        public string email { get; set; }
        public Veiculo veiculo { get; set; }
        public DateTime data { get; set; }
        public string status { get; set; }
        public DateTime? dataAprovacao { get; set; }
    }

    public class Veiculo
    {
        public string modelo { get; set; }
        public int ano { get; set; }
        public string tempoAluguel { get; set; }
    }
}
