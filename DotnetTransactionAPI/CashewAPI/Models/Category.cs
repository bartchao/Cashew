namespace CashewAPI.Models;

public class Category
{
    public string CategoryPk { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string? IconName { get; set; }
    public string? EmojiIconName { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateTimeModified { get; set; }
    public int Order { get; set; }
    public bool Income { get; set; }
    public int? MethodAdded { get; set; }
    public string? MainCategoryPk { get; set; }
}
