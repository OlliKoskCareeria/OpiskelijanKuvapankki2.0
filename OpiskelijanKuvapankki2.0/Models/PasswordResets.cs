namespace OpiskelijanKuvapankki2_0.Models
{
    
        public class PasswordReset
        {
            public Guid Id { get; set; }          

            public int UserId { get; set; }       

            public string CodeHash { get; set; } = null!;  

            public DateTime ExpiresAt { get; set; }        

            public DateTime? UsedAt { get; set; }          

            public int FailedAttempts { get; set; }        

            public DateTime CreatedAt { get; set; }        

            public string? CreatedByIp { get; set; }       

        }

    
}
