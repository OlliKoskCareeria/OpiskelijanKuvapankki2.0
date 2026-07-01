namespace OpiskelijanKuvapankki2_0.Models
{
    public partial class Organisation
    {
        public int Id { get; set; }

        public string ?Domain { get; set; }

        public bool IsActive { get; set; }
    }
}
