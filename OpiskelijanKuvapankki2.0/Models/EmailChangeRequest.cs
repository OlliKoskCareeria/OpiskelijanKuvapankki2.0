namespace OpiskelijanKuvapankki2_0.Models
{
    
        public class EmailChangeRequest
        {
            public int Id { get; set; }

            public int LoginId { get; set; }

            public required string NewEmail { get; set; }

            public required string Code { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime TimeValid { get; set; }

            public int Attempts { get; set; }
        }
    
}
