using System.Text;

namespace CleanArchitecture.CodeGenerator.CodeWriter;

public class MenuGenerator
{
    public string AddMenuItem(string existingCode, string sectionTitle, MenuSectionItemModel newItem)
    {
        var stringBuilder = new StringBuilder(existingCode);

        // Find the section to add the new item
        var sectionStartIndex = stringBuilder.ToString().IndexOf($"Title = \"{sectionTitle}\"");
        if (sectionStartIndex == -1)
        {
            throw new Exception($"Section '{sectionTitle}' not found.");
        }

        // Find where the SectionItems list starts within that section
        var sectionItemsIndex = stringBuilder.ToString().IndexOf("SectionItems =", sectionStartIndex);
        if (sectionItemsIndex == -1)
        {
            throw new Exception("SectionItems not found in the specified section.");
        }

        // Find the closing bracket for the SectionItems list
        var closingBracketIndex = FindClosingBracketIndex(stringBuilder.ToString(), sectionItemsIndex);
        if (closingBracketIndex == -1)
        {
            throw new Exception("Could not find the end of the SectionItems block.");
        }

        // Insert the new menu item
        string newItemCode = GenerateMenuItemCode(newItem);
        stringBuilder.Insert(closingBracketIndex, newItemCode);

        return stringBuilder.ToString();
    }

    public string RemoveMenuItem(string existingCode, string sectionTitle, string itemTitle)
    {
        var stringBuilder = new StringBuilder(existingCode);

        // Find the section to remove the item from
        var sectionStartIndex = stringBuilder.ToString().IndexOf($"Title = \"{sectionTitle}\"");
        if (sectionStartIndex == -1)
        {
            throw new Exception($"Section '{sectionTitle}' not found.");
        }

        // Find where the SectionItems list starts within that section
        var sectionItemsIndex = stringBuilder.ToString().IndexOf("SectionItems =", sectionStartIndex);
        if (sectionItemsIndex == -1)
        {
            throw new Exception("SectionItems not found in the specified section.");
        }

        // Find the menu item by title
        var menuItemIndex = stringBuilder.ToString().IndexOf($"Title = \"{itemTitle}\"", sectionItemsIndex);
        if (menuItemIndex == -1)
        {
            throw new Exception($"Menu item '{itemTitle}' not found in section '{sectionTitle}'.");
        }

        // Find the end of the menu item block (up to the next comma or closing bracket)
        var itemEndIndex = FindItemEndIndex(stringBuilder.ToString(), menuItemIndex);

        // Remove the menu item code
        stringBuilder.Remove(menuItemIndex, itemEndIndex - menuItemIndex);

        return stringBuilder.ToString();
    }

    // Generates the code for a new menu item
    private string GenerateMenuItemCode(MenuSectionItemModel item)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("                new MenuSectionItemModel");
        stringBuilder.AppendLine("                {");
        stringBuilder.AppendLine($"                    Title = \"{item.Title}\",");
        if (!string.IsNullOrEmpty(item.Icon))
        {
            stringBuilder.AppendLine($"                    Icon = \"{item.Icon}\",");
        }
        if (!string.IsNullOrEmpty(item.Href))
        {
            stringBuilder.AppendLine($"                    Href = \"{item.Href}\",");
        }
        stringBuilder.AppendLine($"                    PageStatus = PageStatus.{item.PageStatus},");
        stringBuilder.AppendLine($"                    IsParent = {item.IsParent.ToString().ToLower()},");

        // Add sub-items if any
        if (item.MenuItems != null && item.MenuItems.Count > 0)
        {
            stringBuilder.AppendLine("                    MenuItems = new List<MenuSectionSubItemModel>");
            stringBuilder.AppendLine("                    {");
            foreach (var subItem in item.MenuItems)
            {
                stringBuilder.AppendLine(GenerateSubMenuItemCode(subItem));
            }
            stringBuilder.AppendLine("                    },");
        }
        stringBuilder.AppendLine("                },");

        return stringBuilder.ToString();
    }

    // Generates the code for a submenu item
    private string GenerateSubMenuItemCode(MenuSectionSubItemModel subItem)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("                        new MenuSectionSubItemModel");
        stringBuilder.AppendLine("                        {");
        stringBuilder.AppendLine($"                            Title = \"{subItem.Title}\",");
        if (!string.IsNullOrEmpty(subItem.Href))
        {
            stringBuilder.AppendLine($"                            Href = \"{subItem.Href}\",");
        }
        stringBuilder.AppendLine($"                            PageStatus = PageStatus.{subItem.PageStatus},");
        if (!string.IsNullOrEmpty(subItem.Target))
        {
            stringBuilder.AppendLine($"                            Target = \"{subItem.Target}\"");
        }
        stringBuilder.AppendLine("                        },");

        return stringBuilder.ToString();
    }

    // Helper function to find the closing bracket for a list or block
    private int FindClosingBracketIndex(string code, int startIndex)
    {
        int openBrackets = 0;
        for (int i = startIndex; i < code.Length; i++)
        {
            if (code[i] == '{')
            {
                openBrackets++;
            }
            else if (code[i] == '}')
            {
                openBrackets--;
                if (openBrackets == 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    // Helper function to find the end of a menu item block
    private int FindItemEndIndex(string code, int startIndex)
    {
        int openBrackets = 0;
        for (int i = startIndex; i < code.Length; i++)
        {
            if (code[i] == '{')
            {
                openBrackets++;
            }
            else if (code[i] == '}')
            {
                openBrackets--;
                if (openBrackets == 0 && (code[i + 1] == ',' || code[i + 1] == ']'))
                {
                    return i + 1;
                }
            }
        }
        return code.Length;
    }
}

// Sample Models
public class MenuSectionItemModel
{
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Href { get; set; }
    public string? Target { get; set; }
    public PageStatus PageStatus { get; set; } = PageStatus.Completed;
    public bool IsParent { get; set; }
    public List<MenuSectionSubItemModel>? MenuItems { get; set; }
}

public class MenuSectionSubItemModel
{
    public string Title { get; set; } = string.Empty;
    public string? Href { get; set; }
    public PageStatus PageStatus { get; set; } = PageStatus.Completed;
    public string? Target { get; set; }
}

public enum PageStatus
{
    Completed,
    InProgress,
    Pending
}
