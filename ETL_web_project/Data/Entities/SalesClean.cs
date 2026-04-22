using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETL_web_project.Data.Entities
{
    [Table("SalesClean", Schema = "silver")]
    public class SalesClean
    {
        [Key]
        public long Id { get; set; }

        public int SourceId { get; set; }

        public DateTime SalesTime { get; set; }

        [Required, MaxLength(20)]
        public string StoreCode { get; set; }

        [Required, MaxLength(50)]
        public string ProductCode { get; set; }

        [MaxLength(10)]
        public string? CustomerCode { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CleanedAt { get; set; } = DateTime.Now;

        public bool IsProcessedToGold { get; set; } = false;
    }
}
