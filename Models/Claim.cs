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
        public string UserId { get; set; } // Link the claim to the user

        // Calculated property.
        public decimal TotalSalary => HoursWorked * HourlyRate;

        // Validation logic (static to keep the model clean)
        public static bool IsOverLimit(decimal hourlyRate, decimal hoursWorked, decimal limit)
        {
            return hourlyRate * hoursWorked > limit;
        }
    }
}