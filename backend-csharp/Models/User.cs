using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Picture2Text.Api.Models;

[Table("User")]
public class User
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("ID_NO")]
    public string IdNo { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("Password")]
    public string Password { get; set; } = string.Empty;
}
