namespace PartTwoProg.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string AdditionalNotes { get; set; }
        public string SupportingDocument { get; set; }
        public string Status { get; set; } = "Pending"; // Initial status
    }
}