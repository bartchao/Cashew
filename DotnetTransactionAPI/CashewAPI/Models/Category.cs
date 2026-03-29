// Category.cs — Domain model for transaction categories in the Cashew budget app.
// Maps directly to the "categories" table in the Cashew SQLite database.

namespace CashewAPI.Models;

/// <summary>
/// Represents a transaction category (e.g., Food, Transport) or subcategory.
/// Subcategories reference their parent via <see cref="MainCategoryPk"/>.
/// </summary>
public class Category
{
    /// <summary>UUID v4 primary key for this category.</summary>
    public string CategoryPk { get; set; } = string.Empty;

    /// <summary>Display name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex colour string used for UI display.</summary>
    public string? Colour { get; set; }

    /// <summary>Material icon name for the category.</summary>
    public string? IconName { get; set; }

    /// <summary>Emoji used as an alternative icon.</summary>
    public string? EmojiIconName { get; set; }

    /// <summary>Date the category was created.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Timestamp of the last modification.</summary>
    public DateTime? DateTimeModified { get; set; }

    /// <summary>Sort order for UI display.</summary>
    public int Order { get; set; }

    /// <summary>Whether this category is for income (<c>true</c>) or expenses (<c>false</c>).</summary>
    public bool Income { get; set; }

    /// <summary>How the category was added (e.g., manual, import).</summary>
    public int? MethodAdded { get; set; }

    /// <summary>Parent category PK if this is a subcategory; <c>null</c> for top-level categories.</summary>
    public string? MainCategoryPk { get; set; }
}
