namespace LocalLinker.Models
{
    public class RazorpayViewModel
    {
        public string RazorpayKey { get; set; }
        public string Razorpay_Order_Id { get; set; }
        public int Payment_Id { get; set; }
        public decimal Amount { get; set; }
        public string CustomerEmail { get; set; }
        public int Booking_id { get; set; }
    }
}
